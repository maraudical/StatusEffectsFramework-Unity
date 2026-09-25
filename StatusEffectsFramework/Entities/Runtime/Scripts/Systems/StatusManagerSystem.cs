#if ENTITIES
using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Custom systems that decrement <see cref="StatusEffectTiming.Event"/>
    /// and <see cref="StatusEffectTiming.Predicate"/> <see cref="StatusEffects"/>
    /// buffers should update in the <see cref="StatusEffectSystemGroup"/>.
    /// Any module related systems should most likely run in the
    /// <see cref="SimulationSystemGroup"/>.
    /// </summary>
#if NETCODE
    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup))]
#else
    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
#endif
    [BurstCompile]
    public partial struct StatusManagerSystem : ISystem
    {
        private EntityQuery m_RequestsQuery;
        private EntityQuery m_StatusEffectsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_RequestsQuery = SystemAPI.QueryBuilder().WithAllRW<StatusEffects>().WithPresentRW<StatusEffectEvents, StatusVariablePreEvaluateUpdate>().WithAll<Simulate>().WithAllRW<StatusManagerComponent, StatusEffectRequests>().Build();
            m_RequestsQuery.AddChangedVersionFilter(ComponentType.ReadWrite<StatusEffectRequests>());
            m_StatusEffectsQuery = SystemAPI.QueryBuilder().WithAllRW<StatusEffects>().WithPresentRW<StatusEffectEvents, StatusVariablePreEvaluateUpdate>().WithAll<Simulate>().Build();

            state.RequireForUpdate(m_StatusEffectsQuery);
            state.RequireForUpdate<UnmanagedStatusRegistry>();
#if NETCODE
            state.RequireForUpdate<NetworkTime>();
#endif
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>();

#if NETCODE
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();
            var endPredictedSimulationEntityCommandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
#else
            var elapsedTime = SystemAPI.Time.ElapsedTime;
#endif

            // Update and check durations.
            var statusEffectsJob = new StatusEffectsJob
            {
#if NETCODE
                IsServer = state.WorldUnmanaged.IsServer(),
                NetworkTime = networkTime,
                TickRate = tickRate,
                EndPredictedSimulationEntityCommandBuffer = endPredictedSimulationEntityCommandBuffer,
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
#else
                ElapsedTime = elapsedTime,
#endif
                StatusEffectsHandle = SystemAPI.GetBufferTypeHandle<StatusEffects>(),
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(),
                PreEvaluateUpdateHandle = SystemAPI.GetComponentTypeHandle<StatusVariablePreEvaluateUpdate>(),
            };
            state.Dependency = statusEffectsJob.ScheduleParallelByRef(m_StatusEffectsQuery, state.Dependency);

            // Handle any add/remove requests.
            var statusEffectRequestsJob = new StatusEffectRequestsJob
            {
#if NETCODE
                NetworkTime = networkTime,
#else
                ElapsedTime = elapsedTime,
#endif
                Registry = registry,
            };
            state.Dependency = statusEffectRequestsJob.ScheduleParallelByRef(m_RequestsQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        internal partial struct StatusEffectsJob : IJobChunk
        {
#if NETCODE
            public bool IsServer;
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;
            public EntityCommandBuffer.ParallelWriter EndPredictedSimulationEntityCommandBuffer;
            [ReadOnly]
            public EntityTypeHandle EntityTypeHandle;
#else
            public double ElapsedTime;
#endif
            /// <summary>
            /// Read-write so expired effects can be removed in place, but it is only accessed as
            /// read-write for chunks where something expires so the change version isn't bumped
            /// when nothing changed.
            /// </summary>
            public BufferTypeHandle<StatusEffects> StatusEffectsHandle;
            public BufferTypeHandle<StatusEffectEvents> StatusEffectEventsHandle;
            public ComponentTypeHandle<StatusVariablePreEvaluateUpdate> PreEvaluateUpdateHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
#if NETCODE
                var entities = chunk.GetNativeArray(EntityTypeHandle);
                bool clearEventsAfterPrediction = !IsServer && NetworkTime.IsFinalPredictionTick;
#endif
                var statusEffectsAccessorRO = chunk.GetBufferAccessorRO(ref StatusEffectsHandle);
                var statusEffectEventsAccessor = chunk.GetBufferAccessorRW(ref StatusEffectEventsHandle);
                BufferAccessor<StatusEffects> statusEffectsAccessorRW = default;
                bool hasWriteAccess = false;

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    var statusEffectEvents = statusEffectEventsAccessor[i];
                    statusEffectEvents.Clear();

#if NETCODE
                    if (clearEventsAfterPrediction)
                    {
                        EndPredictedSimulationEntityCommandBuffer.SetBuffer<StatusEffectEvents>(unfilteredChunkIndex, entities[i]).Clear();
                        EndPredictedSimulationEntityCommandBuffer.SetComponentEnabled<StatusEffectEvents>(unfilteredChunkIndex, entities[i], false);
                    }

#endif
                    if (!HasExpiredStatusEffect(statusEffectsAccessorRO[i]))
                    {
                        if (chunk.IsComponentEnabled(ref StatusEffectEventsHandle, i))
                            chunk.SetComponentEnabled(ref StatusEffectEventsHandle, i, false);
                        continue;
                    }

                    // Only bump the change version of chunks that actually have expired effects.
                    if (!hasWriteAccess)
                    {
                        statusEffectsAccessorRW = chunk.GetBufferAccessorRW(ref StatusEffectsHandle);
                        hasWriteAccess = true;
                    }

                    var statusEffects = statusEffectsAccessorRW[i];

                    // Iterate in reverse to not skip any that get removed. RemoveAt keeps the
                    // order of the remaining effects, matching StatusEffectRequestsJob.
                    for (int e = statusEffects.Length - 1; e >= 0; e--)
                    {
                        var statusEffect = statusEffects[e];
                        if (!IsExpired(statusEffect))
                            continue;

                        statusEffectEvents.Add(new StatusEffectEvents(statusEffect.InstanceId, statusEffect.Id, statusEffect.Stacks, StatusEffectEvent.Removed));
                        statusEffects.RemoveAt(e);
                    }

                    chunk.SetComponentEnabled(ref StatusEffectEventsHandle, i, true);
                    chunk.SetComponentEnabled(ref PreEvaluateUpdateHandle, i, true);
                }
            }

            private bool HasExpiredStatusEffect(in DynamicBuffer<StatusEffects> statusEffects)
            {
                for (int e = 0; e < statusEffects.Length; e++)
                    if (IsExpired(statusEffects[e]))
                        return true;

                return false;
            }

            private bool IsExpired(in StatusEffects statusEffect)
            {
                // Event and Predicate timings only check if duration has
                // run out because user created systems should handle
                // decrementing those StatusEffects.
                return statusEffect.Timing is not StatusEffectTiming.Infinite
                    && statusEffect.TimeRemaining(
#if NETCODE
                        NetworkTime.ServerTick, NetworkTime.ServerTickFraction, TickRate
#else
                        ElapsedTime
#endif
                        ) <= 0f;
            }
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        internal partial struct StatusEffectRequestsJob : IJobEntity
        {
#if NETCODE
            public NetworkTime NetworkTime;
#else
            public double ElapsedTime;
#endif
            public UnmanagedStatusRegistry Registry;

            void Execute([ChunkIndexInQuery] int sortKey,
                Entity entity,
                EnabledRefRW<StatusEffectEvents> statusEffectEventsEnabledRW,
                ref StatusManagerComponent statusManager,
                ref DynamicBuffer<StatusEffectRequests> statusEffectRequests,
                ref DynamicBuffer<StatusEffects> statusEffects,
                ref DynamicBuffer<StatusEffectEvents> statusEffectEvents,
                EnabledRefRW<StatusVariablePreEvaluateUpdate> preEvaluateUpdateRW)
            {
                // If nothing to change then continue.
                if (statusEffectRequests.Length <= 0)
                    return;

                // Local functions in a struct can't access instance fields, so copy them out.
#if NETCODE
                var currentTick = NetworkTime.ServerTick;
#else
                var elapsedTime = ElapsedTime;
#endif
                // Requests are applied directly to the buffer so every request sees the result of
                // the ones before it. Removed effects are only marked with 0 stacks so buffer indices
                // stay valid until everything is compacted at the end. Events are then built by
                // comparing the buffer against this snapshot.
                using var originalStatusEffects = new NativeArray<StatusEffects>(statusEffects.AsNativeArray(), Allocator.Temp);
                using var removalCandidates = new NativeList<IndexedStatusEffects>(statusEffects.Length, Allocator.Temp);

                for (int i = 0; i < statusEffectRequests.Length; i++)
                    EvaluateRequest(ref statusEffects, ref statusManager, statusEffectRequests[i], Registry);

                statusEffectRequests.Clear();

                bool statusEffectsChanged = false;

                // Effects that existed before keep their index until compaction, so compare in place.
                for (int i = 0; i < originalStatusEffects.Length; i++)
                {
                    var originalStatusEffect = originalStatusEffects[i];
                    ref var statusEffect = ref statusEffects.ElementAt(i);

                    if (statusEffect.Stacks <= 0)
                        statusEffectEvents.Add(new StatusEffectEvents(originalStatusEffect.InstanceId, originalStatusEffect.Id, originalStatusEffect.Stacks, StatusEffectEvent.Removed));
                    else if (statusEffect.Stacks != originalStatusEffect.Stacks)
                    {
                        statusEffectEvents.Add(new StatusEffectEvents(originalStatusEffect.InstanceId, originalStatusEffect.Id, originalStatusEffect.Stacks, StatusEffectEvent.Updated));
#if NETCODE
                        statusEffect.TickUpdated = currentTick;
#endif
                    }
                    else
                        continue;

                    statusEffectsChanged = true;
                }
                // Anything past the original length was added this update.
                for (int i = originalStatusEffects.Length; i < statusEffects.Length; i++)
                {
                    var statusEffect = statusEffects[i];
                    // Added and removed within the same update.
                    if (statusEffect.Stacks <= 0)
                        continue;

                    statusEffectEvents.Add(new StatusEffectEvents(statusEffect.InstanceId, statusEffect.Id));
                    statusEffectsChanged = true;
                }

                // Compact out removed effects, keeping the order of the rest.
                int length = 0;
                for (int i = 0; i < statusEffects.Length; i++)
                    if (statusEffects[i].Stacks > 0)
                        statusEffects[length++] = statusEffects[i];
                statusEffects.ResizeUninitialized(length);

                if (statusEffectsChanged)
                {
                    statusEffectEventsEnabledRW.ValueRW = true;
                    preEvaluateUpdateRW.ValueRW = true;
                }

                void EvaluateRequest(ref DynamicBuffer<StatusEffects> buffer,
                                     ref StatusManagerComponent manager,
                                     StatusEffectRequests request,
                                     in UnmanagedStatusRegistry registry)
                {
                    if (request.Type is StatusEffectRequestType.Add)
                        AddRequest(ref buffer, ref manager, request, registry);
                    else
                        RemoveRequest(ref buffer, request, registry);
                }

                // Copied from regular private StatusManager.AddStatusEffect() with burstable types and math.
                void AddRequest(ref DynamicBuffer<StatusEffects> buffer,
                                ref StatusManagerComponent manager,
                                StatusEffectRequests request,
                                in UnmanagedStatusRegistry registry)
                {
                    if (request.Stacks <= 0)
                        return;

                    // If the duration given is less than zero it won't be applied.
                    if (request.Timing is not StatusEffectTiming.Infinite && request.Duration < 0)
                        return;

                    ref var data = ref registry.GetStatusEffectData(request.Id);
                    // Buffer indices of an effect being replaced and an infinite effect to merge into.
                    int flagForRemoval = -1;
                    int mergeIndex = -1;

                    // If stacking is allowed then check for the max stacks and possible stack merging.
                    if (data.AllowEffectStacking)
                    {
                        int stackCount = 0;
                        // Go through all similar effects and count the stacks and if there is an
                        // infinite effect.
                        for (int x = 0; x < buffer.Length; x++)
                        {
                            var existingEffect = buffer[x];
                            if (existingEffect.Stacks <= 0 || existingEffect.Id != data.Id)
                                continue;

                            if (existingEffect.Timing is StatusEffectTiming.Infinite)
                                mergeIndex = x;

                            stackCount += existingEffect.Stacks;
                        }
                        // Check if the current stacked amount is already max.
                        if (data.MaxStacks >= 0)
                        {
                            if (stackCount >= data.MaxStacks)
                                return;
                            // Otherwise cut the given stack if it would've been over the max.
                            if (stackCount + request.Stacks > data.MaxStacks)
                                request.Stacks = data.MaxStacks - stackCount;
                        }
                        // We can only safely merge infinite stacks because merging things with
                        // duration or predicates we either can't or its too difficult to determine
                        // their similarity.
                        if (request.Timing is not StatusEffectTiming.Infinite)
                            mergeIndex = -1;
                    }
                    // Non-stackable: Check to delete the effect if it already exists to prevent duplicates.
                    else
                    {
                        request.Stacks = 1;
                        int oldStatusEffectIndex = -1;

                        for (int x = 0; x < buffer.Length; x++)
                        {
                            var existingEffect = buffer[x];
                            if (existingEffect.Stacks <= 0)
                                continue;

                            bool isSameEffect = data.ComparableName != default
                                ? registry.GetStatusEffectData(existingEffect.Id).ComparableName == data.ComparableName
                                : existingEffect.Id == data.Id;

                            if (isSameEffect)
                            {
                                oldStatusEffectIndex = x;
                                break;
                            }
                        }

                        if (oldStatusEffectIndex >= 0)
                        {
                            var oldStatusEffect = buffer[oldStatusEffectIndex];
                            ref var oldData = ref registry.GetStatusEffectData(oldStatusEffect.Id);

                            switch (data.NonStackingBehaviour)
                            {
                                case NonStackingBehaviour.MatchHighestValue:
                                    if (data.BaseValue == oldData.BaseValue)
                                        goto case NonStackingBehaviour.TakeHighestDuration;

                                    float baseValue = math.abs(data.BaseValue);
                                    float oldBaseValue = math.abs(oldData.BaseValue);
                                    // WARNING: There is an extremely special case here where
                                    // a player may either have or try to apply an effect which
                                    // has an infinite duration (-1). In this situation, attempt
                                    // to take the higest value, and if they are the same take
                                    // the infinite duration effect.
                                    if (request.Timing is StatusEffectTiming.Infinite || oldStatusEffect.Timing is StatusEffectTiming.Infinite)
                                    {
                                        if (baseValue < oldBaseValue)
                                            return;
                                        else if (baseValue > oldBaseValue || oldStatusEffect.Timing is not StatusEffectTiming.Infinite)
                                        {
                                            flagForRemoval = oldStatusEffectIndex;
                                            break;
                                        }
                                        else
                                            return;
                                    }
                                    // Find which effect is highest value.
                                    ref var highestValueData = ref baseValue < oldBaseValue ? ref oldData : ref data;
                                    float highestValueDuration = baseValue < oldBaseValue ? oldStatusEffect.Duration : request.Duration;
                                    ref var lowestValueData = ref baseValue < oldBaseValue ? ref data : ref oldData;
                                    float lowestValueDuration = baseValue < oldBaseValue ? request.Duration : oldStatusEffect.Duration;
                                    // Calculate the new duration = d1 + d2 / (v1 / v2). Note this assumes neither base value will ever be 0.
                                    if (highestValueData.BaseValue == 0 || lowestValueData.BaseValue == 0)
                                        throw new ArgumentException("A StatusEffectData has a base value of 0! This will cause an error!");

                                    request.Duration = highestValueDuration + lowestValueDuration / (math.abs(highestValueData.BaseValue) / math.abs(lowestValueData.BaseValue));
                                    data = ref highestValueData;
                                    flagForRemoval = oldStatusEffectIndex;
                                    break;
                                case NonStackingBehaviour.TakeHighestDuration:
                                    if (oldStatusEffect.Timing is StatusEffectTiming.Infinite || (request.Duration < oldStatusEffect.Duration && request.Timing is not StatusEffectTiming.Infinite))
                                        return;
                                    else
                                        flagForRemoval = oldStatusEffectIndex;
                                    break;
                                case NonStackingBehaviour.TakeHighestValue:
                                    float oldValue = math.abs(oldData.BaseValue);
                                    float newValue = math.abs(data.BaseValue);
                                    if (newValue == oldValue)
                                        goto case NonStackingBehaviour.TakeHighestDuration;
                                    if (newValue < oldValue)
                                        return;
                                    else
                                        flagForRemoval = oldStatusEffectIndex;
                                    break;
                                case NonStackingBehaviour.TakeNewest:
                                    flagForRemoval = oldStatusEffectIndex;
                                    break;
                                case NonStackingBehaviour.TakeOldest:
                                    return;
                            }
                        }
                    }

                    // Check for conditions.
                    bool preventStatusEffect = false;

                    for (int c = 0; c < data.Conditions.Length; c++)
                    {
                        UnmanagedCondition condition = data.Conditions[c];
                        // If the condition is checking for existence and it doesn't exist or if
                        // its checking non-existence and does exist then skip this condition.
                        if (condition.Exists != ConditionTargetExists(buffer, condition, registry))
                            continue;

                        int conditionalStacks = condition.Add || condition.UseStacks ? condition.Stacks * (condition.Scaled ? request.Stacks : 1) : -1;

                        if (condition.Add)
                        {
                            StatusEffectRequests conditionalRequest = condition.Timing switch
                            {
                                ConditionalTiming.Duration => StatusEffectRequests.AddWithDuration(condition.ActionData, condition.Duration, conditionalStacks),
                                ConditionalTiming.Inherited => new StatusEffectRequests
                                {
                                    Type = StatusEffectRequestType.Add,
                                    Id = condition.ActionData,
                                    Timing = request.Timing,
                                    Duration = request.Duration,
                                    Interval = request.Interval,
                                    Stacks = conditionalStacks,
                                    EventId = request.EventId
                                },
                                _ => StatusEffectRequests.Add(condition.ActionData, conditionalStacks),
                            };
                            AddRequest(ref buffer, ref manager, conditionalRequest, registry);
                        }
                        // Special case where the configurable which is the
                        // current data to be added is tagged for removal.
                        else if (condition.ActionData == data.Id)
                        {
                            if (!condition.UseStacks || conditionalStacks >= request.Stacks)
                            {
                                preventStatusEffect = true;
                                if (condition.UseStacks)
                                    conditionalStacks -= request.Stacks;
                                request.Stacks = 0;
                                // In the case where other status effects of this Id exist we need to request to remove them.
                                RemoveRequest(ref buffer, ConditionalRemoval(condition, conditionalStacks), registry);
                            }
                            else
                                request.Stacks -= conditionalStacks;
                        }
                        // Otherwise we remove effects.
                        else
                            RemoveRequest(ref buffer, ConditionalRemoval(condition, conditionalStacks), registry);
                    }

                    if (preventStatusEffect)
                        return;

                    if (flagForRemoval >= 0)
                        buffer.ElementAt(flagForRemoval).Stacks = 0;

                    // Merge into the existing infinite effect unless a condition removed it.
                    if (mergeIndex >= 0 && buffer[mergeIndex].Stacks > 0)
                    {
                        buffer.ElementAt(mergeIndex).Stacks += request.Stacks;
                        return;
                    }

                    // Add the status effect
                    buffer.Add(new StatusEffects()
                    {
#if NETCODE
                        TickAdded = currentTick,
                        TickUpdated = currentTick,
#else
                        TimeAdded = elapsedTime,
#endif
                        InstanceId = manager.AvailableId++,
                        Id = data.Id,
                        Timing = request.Timing,
                        Duration = request.Duration,
                        Interval = request.Interval,
                        Stacks = request.Stacks,
                        EventId = request.EventId,
                    });
                }

                void RemoveRequest(ref DynamicBuffer<StatusEffects> buffer,
                                   StatusEffectRequests request,
                                   in UnmanagedStatusRegistry registry)
                {
                    removalCandidates.Clear();

                    for (int x = 0; x < buffer.Length; x++)
                    {
                        var statusEffect = buffer[x];
                        if (statusEffect.Stacks > 0 && MatchesRemoval(statusEffect, request, registry))
                            removalCandidates.Add(new IndexedStatusEffects(x, statusEffect));
                    }
                    // Sort by value and duration.
                    removalCandidates.Sort(new IndexedStatusEffectComparer(registry, false));
                    // Remove until the requested stack count is reached.
                    int removedCount = 0;
                    for (int x = 0; x < removalCandidates.Length; x++)
                    {
                        if (request.Stacks >= 0 && removedCount >= request.Stacks)
                            break;

                        ref var statusEffect = ref buffer.ElementAt(removalCandidates[x].Index);
                        int removing = request.Stacks >= 0 ? math.min(statusEffect.Stacks, request.Stacks - removedCount) : statusEffect.Stacks;
                        statusEffect.Stacks -= removing;
                        removedCount += removing;
                    }
                }

                static bool ConditionTargetExists(in DynamicBuffer<StatusEffects> buffer,
                                                  in UnmanagedCondition condition,
                                                  in UnmanagedStatusRegistry registry)
                {
                    for (int x = 0; x < buffer.Length; x++)
                    {
                        var statusEffect = buffer[x];
                        if (statusEffect.Stacks <= 0)
                            continue;

                        switch (condition.SearchableConfigurable)
                        {
                            case ConditionalConfigurable.AllGroups:
                                if ((registry.GetStatusEffectData(statusEffect.Id).Group & condition.SearchableGroup) != 0)
                                    return true;
                                break;
                            case ConditionalConfigurable.Name:
                                if (registry.GetStatusEffectData(statusEffect.Id).ComparableName == condition.SearchableComparableName)
                                    return true;
                                break;
                            case ConditionalConfigurable.Data:
                                if (statusEffect.Id == condition.SearchableData)
                                    return true;
                                break;
                        }
                    }

                    return false;
                }

                static bool MatchesRemoval(in StatusEffects statusEffect,
                                           in StatusEffectRequests request,
                                           in UnmanagedStatusRegistry registry)
                {
                    return request.RemovalType switch
                    {
                        StatusEffectRemovalType.InstanceId => statusEffect.InstanceId == request.InstanceId,
                        StatusEffectRemovalType.Id => statusEffect.Id == request.Id,
                        StatusEffectRemovalType.ComparableName => registry.GetStatusEffectData(statusEffect.Id).ComparableName == request.Id,
                        StatusEffectRemovalType.AnyGroups => (registry.GetStatusEffectData(statusEffect.Id).Group & request.Group) != 0,
                        StatusEffectRemovalType.AllGroups => (registry.GetStatusEffectData(statusEffect.Id).Group & request.Group) == request.Group,
                        StatusEffectRemovalType.Any => true,
                        _ => false,
                    };
                }

                static StatusEffectRequests ConditionalRemoval(in UnmanagedCondition condition, int stacks)
                {
                    return condition.ActionConfigurable switch
                    {
                        ConditionalConfigurable.Data => StatusEffectRequests.RemoveWithId(condition.ActionData, stacks),
                        ConditionalConfigurable.Name => StatusEffectRequests.RemoveWithComparableName(condition.ActionComparableName, stacks),
                        ConditionalConfigurable.AllGroups => StatusEffectRequests.RemoveWithGroup(condition.ActionGroup, stacks, true),
                        ConditionalConfigurable.AnyGroups => StatusEffectRequests.RemoveWithGroup(condition.ActionGroup, stacks, false),
                        _ => throw new InvalidOperationException($"Invalid {nameof(ConditionalConfigurable)} enum value of {condition.ActionConfigurable}.")
                    };
                }
            }
        }
    }
}
#endif
