#if ENTITIES
using System;
using System.Globalization;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
#if NETCODE
using Unity.NetCode;
using static Unity.Entities.EntitiesJournaling;
#endif

namespace StatusEffectFramework.Entities
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
            m_RequestsQuery = SystemAPI.QueryBuilder().WithAllRW<StatusEffects>().WithPresentRW<StatusEffectEvents, StatusVariablePreEvaluateUpdate>().WithAllRW<DynamicFloats, DynamicInts>().WithAllRW<DynamicBools>().WithAll<Simulate>().WithAllRW<StatusManagerComponent, StatusEffectRequests>().Build();
            m_RequestsQuery.AddChangedVersionFilter(ComponentType.ReadWrite<StatusEffectRequests>());
            m_StatusEffectsQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects>().WithPresentRW<StatusEffectEvents, StatusVariablePreEvaluateUpdate>().WithAllRW<DynamicFloats, DynamicInts>().WithAllRW<DynamicBools>().WithAll<Simulate>().Build();

            state.RequireForUpdate(m_StatusEffectsQuery);
            state.RequireForUpdate<StatusReferences>();
#if NETCODE
            state.RequireForUpdate<NetworkTime>();
#endif
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var references = ref SystemAPI.GetSingletonRW<StatusReferences>().ValueRW;

#if NETCODE
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();
            var endPredictedSimulationEntityCommandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var endStatusEffectEntityCommandBuffer = SystemAPI.GetSingleton<EndPredictedStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            if (networkTime.IsFirstPredictionTick)
            {
                var checkIfRebuildModulesNeededJob = new CheckIfRebuildModulesNeededJob
                {
                    References = references,
                    EndPredictedSimulationEntityCommandBuffer = endPredictedSimulationEntityCommandBuffer,
                    EndStatusEffectEntityCommandBuffer = endStatusEffectEntityCommandBuffer,
                };
                state.Dependency = checkIfRebuildModulesNeededJob.ScheduleParallelByRef(state.Dependency);
            }
#else
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            var endStatusEffectEntityCommandBuffer = SystemAPI.GetSingleton<EndStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
#endif
            
            // Update and check durations.
            var statusEffectsJob = new StatusEffectsJob
            {
#if NETCODE
                IsServer = state.WorldUnmanaged.IsServer(),
                NetworkTime = networkTime,
                TickRate = tickRate,
                EndPredictedSimulationEntityCommandBuffer = endPredictedSimulationEntityCommandBuffer,
#else
                ElapsedTime = elapsedTime,
#endif
                EndStatusEffectEntityCommandBuffer = endStatusEffectEntityCommandBuffer,
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
                References = references,
                EndStatusEffectEntityCommandBuffer = endStatusEffectEntityCommandBuffer,
            };
            state.Dependency = statusEffectRequestsJob.ScheduleParallelByRef(m_RequestsQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        internal static void AddDynamicValue(int sortKey, 
            in Entity entity, 
            uint id, 
            ref UnmanagedStatusEffectData statusEffectData, 
            ref EntityCommandBuffer.ParallelWriter commandBuffer, 
            ref DynamicBuffer<DynamicFloats> dynamicFloats, 
            ref DynamicBuffer<DynamicInts> dynamicInts, 
            ref DynamicBuffer<DynamicBools> dynamicBools)
        {
            for (int i = 0; i < statusEffectData.Effects.Length; i++)
            {
                ref var effect = ref statusEffectData.Effects[i];
                if (effect.ValueSource is not ValueSource.DynamicValue)
                    continue;

                commandBuffer.AddComponent(sortKey, entity, ComponentType.ReadOnly(effect.TypeIndex));

                switch (effect.ValueType)
                {
                    case ValueType.Float:
                        dynamicFloats.Add(new DynamicFloats()
                        {
                            Id = id,
                            TypeIndex = effect.TypeIndex,
                            StatusName = effect.StatusName,
                            ValueModifier = effect.ValueModifier,
                            PostEvaluate = effect.PostEvaluate,
                            Priority = effect.Priority,
                            Value = effect.FloatValue,
                            DynamicEffectInfo = effect.DynamicEffectInfo
                        });
                        break;
                    case ValueType.Int:
                        dynamicInts.Add(new DynamicInts()
                        {
                            Id = id,
                            TypeIndex = effect.TypeIndex,
                            StatusName = effect.StatusName,
                            ValueModifier = effect.ValueModifier,
                            PostEvaluate = effect.PostEvaluate,
                            Priority = effect.Priority,
                            Value = effect.IntValue,
                            DynamicEffectInfo = effect.DynamicEffectInfo
                        });
                        break;
                    case ValueType.Bool:
                        dynamicBools.Add(new DynamicBools()
                        {
                            Id = id,
                            TypeIndex = effect.TypeIndex,
                            StatusName = effect.StatusName,
                            PostEvaluate = effect.PostEvaluate,
                            Priority = effect.Priority,
                            Value = effect.BoolValue,
                            DynamicEffectInfo = effect.DynamicEffectInfo
                        });
                        break;
                }
            }
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        internal static void RemoveIdAndBuildTypeList(uint id, ref DynamicBuffer<DynamicFloats> dynamicFloats, ref DynamicBuffer<DynamicInts> dynamicInts , ref DynamicBuffer<DynamicBools> dynamicBools, ref UnsafeHashSet<TypeIndex> existingTypes, ref UnsafeHashSet<TypeIndex> removingTypes)
        {
            for (int v = dynamicFloats.Length - 1; v >= 0; v--)
            {
                var dynamicFloat = dynamicFloats[v];

                if (dynamicFloat.Id == id)
                {
                    dynamicFloats.RemoveAtSwapBack(v);
                    removingTypes.Add(dynamicFloat.TypeIndex);
                }
                else
                    existingTypes.Add(dynamicFloat.TypeIndex);
            }
            for (int v = dynamicInts.Length - 1; v >= 0; v--)
            {
                var dynamicInt = dynamicInts[v];

                if (dynamicInt.Id == id)
                {
                    dynamicInts.RemoveAtSwapBack(v);
                    removingTypes.Add(dynamicInt.TypeIndex);
                }
                else
                    existingTypes.Add(dynamicInt.TypeIndex);
            }
            for (int v = dynamicBools.Length - 1; v >= 0; v--)
            {
                var dynamicBool = dynamicBools[v];

                if (dynamicBool.Id == id)
                {
                    dynamicBools.RemoveAtSwapBack(v);
                    removingTypes.Add(dynamicBool.TypeIndex);
                }
                else
                    existingTypes.Add(dynamicBool.TypeIndex);
            }
        }
#if NETCODE

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        [WithAll(typeof(Simulate))]
        internal partial struct CheckIfRebuildModulesNeededJob : IJobEntity
        {
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter EndPredictedSimulationEntityCommandBuffer;
            public EntityCommandBuffer.ParallelWriter EndStatusEffectEntityCommandBuffer;

            void Execute([ChunkIndexInQuery] int sortKey,
                Entity entity,
                in DynamicBuffer<StatusEffects> statusEffects,
                in DynamicBuffer<InterpolatedStatusEffects> interpolatedStatusEffects,
                ref DynamicBuffer<DynamicFloats> dynamicFloats,
                ref DynamicBuffer<DynamicInts> dynamicInts,
                ref DynamicBuffer<DynamicBools> dynamicBools)
            {
                if (statusEffects.Length != interpolatedStatusEffects.Length)
                    goto AddTag;

                for (int i = 0; i < statusEffects.Length; i++)
                    if (statusEffects[i].Id != interpolatedStatusEffects[i].Id)
                        goto AddTag;

                return;

                AddTag:

                EndStatusEffectEntityCommandBuffer.AddComponent<RebuildModulesTag>(sortKey, entity);
                EndPredictedSimulationEntityCommandBuffer.RemoveComponent<RebuildModulesTag>(sortKey, entity);

                foreach (var dynamicFloat in dynamicFloats)
                    EndStatusEffectEntityCommandBuffer.RemoveComponent(sortKey, entity, ComponentType.ReadOnly(dynamicFloat.TypeIndex));
                foreach (var dynamicInt in dynamicInts)
                    EndStatusEffectEntityCommandBuffer.RemoveComponent(sortKey, entity, ComponentType.ReadOnly(dynamicInt.TypeIndex));
                foreach (var dynamicBool in dynamicBools)
                    EndStatusEffectEntityCommandBuffer.RemoveComponent(sortKey, entity, ComponentType.ReadOnly(dynamicBool.TypeIndex));

                dynamicFloats.Clear();
                dynamicInts.Clear();
                dynamicBools.Clear();

                foreach (var statusEffect in statusEffects)
                    if (References.IdToStatusEffectDataMap.Value.TryGetValue(statusEffect.StatusEffectDataId, out var reference))
                    {
                        ref UnmanagedStatusEffectData statusEffectData = ref reference.Value;
                        AddDynamicValue(sortKey,
                            entity,
                            statusEffect.Id,
                            ref statusEffectData,
                            ref EndStatusEffectEntityCommandBuffer,
                            ref dynamicFloats,
                            ref dynamicInts,
                            ref dynamicBools);
                    }
            } 
        }
#endif

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        internal partial struct StatusEffectsJob : IJobEntity
        {
#if NETCODE
            public bool IsServer;
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;
            public EntityCommandBuffer.ParallelWriter EndPredictedSimulationEntityCommandBuffer;
#else
            public double ElapsedTime;
#endif
            public EntityCommandBuffer.ParallelWriter EndStatusEffectEntityCommandBuffer;

            void Execute([ChunkIndexInQuery] int sortKey,
                Entity entity,
                EnabledRefRW<StatusEffectEvents> statusEffectEventsEnabledRW,
                in DynamicBuffer<StatusEffects> statusEffects,
                ref DynamicBuffer<StatusEffectEvents> statusEffectEvents,
                ref DynamicBuffer<DynamicFloats> dynamicFloats,
                ref DynamicBuffer<DynamicInts> dynamicInts,
                ref DynamicBuffer<DynamicBools> dynamicBools,
                EnabledRefRW<StatusVariablePreEvaluateUpdate> preEvaluateUpdateRW)
            {
                statusEffectEvents.Clear();

#if NETCODE
                if (!IsServer && NetworkTime.IsFinalPredictionTick)
                {
                    EndPredictedSimulationEntityCommandBuffer.SetBuffer<StatusEffectEvents>(sortKey, entity).Clear();
                    EndPredictedSimulationEntityCommandBuffer.SetComponentEnabled<StatusEffectEvents>(sortKey, entity, false);
                }

#endif
                UnsafeHashSet<TypeIndex> existingTypes = new UnsafeHashSet<TypeIndex>(0, Allocator.Temp);
                UnsafeHashSet<TypeIndex> removingTypes = new UnsafeHashSet<TypeIndex>(0, Allocator.Temp);
                DynamicBuffer<StatusEffects> statusEffectsCopy = default;
                bool statusEffectEventsEnabled = statusEffectEventsEnabledRW.ValueRO;

                // Iterate in reverse to not skip any that get removed.
                for (int i = statusEffects.Length - 1; i >= 0; i--)
                {
                    var statusEffect = statusEffects[i];

                    switch (statusEffect.Timing)
                    {
                        case StatusEffectTiming.Infinite:
                            continue;
                        default:
                            // Event and Predicate timings only check if duration has
                            // run out because user created systems should handle
                            // decrementing those StatusEffects.
                            if (statusEffect.TimeRemaining(
#if NETCODE
                                NetworkTime.ServerTick, NetworkTime.ServerTickFraction, TickRate
#else
                                ElapsedTime
#endif
                                ) <= 0f)
                            {
                                if (statusEffectEvents.Length == 0)
                                {
                                    statusEffectsCopy = EndStatusEffectEntityCommandBuffer.SetBuffer<StatusEffects>(sortKey, entity);
                                    statusEffectsCopy.CopyFrom(statusEffects);
                                    if (!statusEffectEventsEnabled)
                                    {
                                        statusEffectEventsEnabledRW.ValueRW = true;
                                        preEvaluateUpdateRW.ValueRW = true;
                                    }
                                }
                                statusEffectEvents.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId, statusEffect.Stacks, StatusEffectEvent.Removed));
                                
                                statusEffectsCopy.RemoveAtSwapBack(i);
                                // Remove dynamic effects from the buffers.
                                existingTypes.Clear();
                                removingTypes.Clear();

                                RemoveIdAndBuildTypeList(statusEffect.Id, ref dynamicFloats, ref dynamicInts, ref dynamicBools, ref existingTypes, ref removingTypes);

                                foreach (var type in removingTypes)
                                    if (!existingTypes.Contains(type))
                                        EndStatusEffectEntityCommandBuffer.RemoveComponent(sortKey, entity, ComponentType.ReadOnly(type));
                            }
                            break;
                    }
                }

                existingTypes.Dispose();
                removingTypes.Dispose();

                if (statusEffectEventsEnabled && statusEffectEvents.Length == 0)
                    statusEffectEventsEnabledRW.ValueRW = false;
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
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter EndStatusEffectEntityCommandBuffer;
            public UnsafeParallelHashSet<TypeIndex>.ParallelWriter TypeIndices;

            unsafe void Execute([ChunkIndexInQuery] int sortKey, 
                Entity entity,
                EnabledRefRW<StatusEffectEvents> statusEffectEventsEnabledRW, 
                ref StatusManagerComponent statusManager, 
                ref DynamicBuffer<StatusEffectRequests> statusEffectRequests, 
                ref DynamicBuffer<StatusEffects> statusEffects, 
                ref DynamicBuffer<StatusEffectEvents> statusEffectEvents,
                ref DynamicBuffer<DynamicFloats> dynamicFloats,
                ref DynamicBuffer<DynamicInts> dynamicInts,
                ref DynamicBuffer<DynamicBools> dynamicBools,
                EnabledRefRW<StatusVariablePreEvaluateUpdate> preEvaluateUpdateRW)
            {
                // If nothing to change then continue.
                if (statusEffectRequests.Length <= 0)
                    return;
                
                NativeArray<StatusEffects>.ReadOnly unsortedStatusEffects = statusEffects.AsNativeArray().AsReadOnly();
                NativeList<IndexedStatusEffects> statusEffectStackUpdates = new NativeList<IndexedStatusEffects>(unsortedStatusEffects.Length, Allocator.Temp);

                for (int i = statusEffectRequests.Length - 1; i >= 0; i--)
                {
                    EvaluateRequest(ref statusEffects, statusEffectRequests.ElementAt(i), References);

                    void EvaluateRequest(ref DynamicBuffer<StatusEffects> statusEffectBuffer,
                                         StatusEffectRequests request, 
                                         in StatusReferences references)
                    {
                        // Copied from regular private StatusManager.AddStatusEffect() with burstable types and math.
                        if (request.Type is StatusEffectRequestType.Add)
                        {
                            if (request.Stacks <= 0)
                                return;
                            
                            if (!references.IdToStatusEffectDataMap.Value.TryGetValue(request.Hash, out var reference))
                                return;
                            
                            // If the duration given is less than zero it won't be applied.
                            if (request.Timing is not StatusEffectTiming.Infinite && request.Duration < 0)
                                return;
                            // Declare here to use later.
                            ref UnmanagedStatusEffectData statusEffectData = ref reference.Value;
                            IndexedStatusEffects flagForRemoval = new IndexedStatusEffects(-1, default);
                            IndexedStatusEffects indexedStatusEffect = new IndexedStatusEffects()
                            {
                                Index = -1,
                                StatusEffectDataId = statusEffectData.Id,
                                Timing = request.Timing,
                                Interval = request.Interval,
                                EventId = request.EventId
                            };
                            // If stacking is allowed then check for the max stacks and possible stack merging.
                            if (statusEffectData.AllowEffectStacking)
                            {
                                if (request.Timing is not StatusEffectTiming.Infinite)
                                    goto CheckConditionals;

                                NativeList<IndexedStatusEffects>  sortedStatusEffects = new NativeList<IndexedStatusEffects>(unsortedStatusEffects.Length, Allocator.Temp);
                                int stackCount = 0;

                                for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    if (unsortedStatusEffects[x].StatusEffectDataId == request.Hash)
                                        sortedStatusEffects.Add(new IndexedStatusEffects(x, unsortedStatusEffects[x]));

                                if (sortedStatusEffects.Length <= 0)
                                {
                                    sortedStatusEffects.Dispose();
                                    if (!TryToLimitStacks(ref request.Stacks, statusEffectData.MaxStacks))
                                        return;
                                    goto CheckConditionals;
                                }

                                int infiniteEffectIndex = -1;
                                // Go through all similar effects and count the stacks and if there is an
                                // infinite effect.
                                foreach (var existentEffect in sortedStatusEffects)
                                {
                                    if (existentEffect.Timing is StatusEffectTiming.Infinite)
                                        infiniteEffectIndex = existentEffect.Index;

                                    stackCount += existentEffect.Stacks;
                                }
                                // Iterate again for currently updating ones.
                                foreach (var updatingEffect in statusEffectStackUpdates)
                                {
                                    if (updatingEffect.StatusEffectDataId != statusEffectData.Id)
                                        continue;

                                    stackCount += updatingEffect.Stacks;
                                }

                                if (!TryToLimitStacks(ref request.Stacks, statusEffectData.MaxStacks))
                                {
                                    sortedStatusEffects.Dispose();
                                    return;
                                }
                                
                                // We can only safely merge infinite stacks because merging things with
                                // duration or predicates we either can't or its too difficult to determine
                                // their similarity.
                                if (request.Timing is StatusEffectTiming.Infinite && infiniteEffectIndex >= 0)
                                {
                                    // Setup dummy update so that we can add normally later.
                                    indexedStatusEffect.Index = infiniteEffectIndex;
                                    sortedStatusEffects.Dispose();
                                    goto CheckConditionals;
                                }
                                // False when stack count is already max, otherwise true.
                                bool TryToLimitStacks(ref int stacks, int maxStacks)
                                {
                                    // Check if the current stacked amount is already max.
                                    if (maxStacks >= 0)
                                        if (stackCount >= maxStacks)
                                            return false;
                                        // Otherwise cut the given stack if it would've been over the max.
                                        else if (stackCount + stacks > maxStacks)
                                            stacks = maxStacks - stackCount;

                                    return true;
                                }

                                sortedStatusEffects.Dispose();
                            }
                            // Non-stackable: Check to delete the effect if it already exists to prevent duplicates.
                            else
                            {
                                request.Stacks = 1;
                                int oldStatusEffectIndex = -1;

                                if (statusEffectData.ComparableName != default)
                                {
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    {
                                        if (!references.IdToStatusEffectDataMap.Value.TryGetValue(unsortedStatusEffects[x].StatusEffectDataId, out var unsortedReferences))
                                            continue;
                                        ref UnmanagedStatusEffectData unsortedData = ref unsortedReferences.Value;
                                        if (unsortedData.ComparableName == statusEffectData.ComparableName)
                                        {
                                            oldStatusEffectIndex = x;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    for (int y = 0; y < unsortedStatusEffects.Length; y++)
                                    {
                                        if (unsortedStatusEffects[y].StatusEffectDataId == statusEffectData.Id)
                                        {
                                            oldStatusEffectIndex = y;
                                            break;
                                        }
                                    }
                                }

                                if (oldStatusEffectIndex < 0)
                                    goto CheckConditionals;

                                IndexedStatusEffects oldStatusEffect = new IndexedStatusEffects(oldStatusEffectIndex, statusEffectBuffer.ElementAt(oldStatusEffectIndex));
                                var oldReference = references.IdToStatusEffectDataMap.Value[oldStatusEffect.StatusEffectDataId];

                                switch (statusEffectData.NonStackingBehaviour)
                                {
                                    case NonStackingBehaviour.MatchHighestValue:
                                        if (statusEffectData.BaseValue == oldReference.Value.BaseValue)
                                            goto case NonStackingBehaviour.TakeHighestDuration;

                                        float baseValue = math.abs(statusEffectData.BaseValue);
                                        float oldBaseValue = math.abs(statusEffectData.BaseValue);
                                        // WARNING: There is an extremely special case here where
                                        // a player may either have or try to apply an effect which
                                        // has an infinite duration (-1). In this situation, attempt
                                        // to take the higest value, and if they are the same take
                                        // the infinite duration effect.
                                        if (request.Timing is StatusEffectTiming.Infinite || oldStatusEffect.Timing is StatusEffectTiming.Infinite)
                                        {
                                            if (baseValue < oldReference.Value.BaseValue)
                                                return;
                                            else if (baseValue > oldReference.Value.BaseValue || oldStatusEffect.Timing is not StatusEffectTiming.Infinite)
                                            {
                                                flagForRemoval = oldStatusEffect;
                                                break;
                                            }
                                            else
                                                return;
                                        }
                                        // Find which effect is highest value.
                                        var highestValueReference = baseValue < oldBaseValue ? oldReference : reference;
                                        ref var highestValueData = ref highestValueReference.Value;
                                        float highestValueDuration = baseValue < oldBaseValue ? oldStatusEffect.Duration : request.Duration;
                                        var lowestValueReference = baseValue < oldBaseValue ? reference : oldReference;
                                        ref var lowestValueData = ref lowestValueReference.Value;
                                        float lowestValueDuration = baseValue < oldBaseValue ? request.Duration : oldStatusEffect.Duration;
                                        // Calculate the new duration = d1 + d2 / (v1 / v2). Note this assumes neither base value will ever be 0.
                                        if (highestValueData.BaseValue == 0 || lowestValueData.BaseValue == 0)
                                            throw new ArgumentException("A StatusEffectData has a base value of 0! This will cause an error!");

                                        request.Duration = highestValueDuration + lowestValueDuration / (math.abs(highestValueData.BaseValue) / math.abs(lowestValueData.BaseValue));
                                        reference = highestValueReference;
                                        statusEffectData = highestValueData;
                                        flagForRemoval = oldStatusEffect;
                                        break;
                                    case NonStackingBehaviour.TakeHighestDuration:
                                        if (oldStatusEffect.Timing is StatusEffectTiming.Infinite || (request.Duration < oldStatusEffect.Duration && request.Timing is not StatusEffectTiming.Infinite))
                                            return;
                                        else
                                            flagForRemoval = oldStatusEffect;
                                        break;
                                    case NonStackingBehaviour.TakeHighestValue:
                                        float oldValue = math.abs(oldReference.Value.BaseValue);
                                        float newValue = math.abs(statusEffectData.BaseValue);
                                        if (newValue == oldValue)
                                            goto case NonStackingBehaviour.TakeHighestDuration;
                                        if (newValue < oldValue)
                                            return;
                                        else
                                            flagForRemoval = oldStatusEffect;
                                        break;
                                    case NonStackingBehaviour.TakeNewest:
                                        flagForRemoval = oldStatusEffect;
                                        break;
                                    case NonStackingBehaviour.TakeOldest:
                                        return;
                                }
                            }

                            CheckConditionals:
                            // Check for conditions.
                            bool preventStatusEffect = false;

                            for (int c = 0; c < statusEffectData.Conditions.Length; c++)
                            {
                                UnmanagedCondition condition = statusEffectData.Conditions[c];
                                bool exists = false;

                                switch (condition.SearchableConfigurable)
                                {
                                    case ConditionalConfigurable.AllGroups:
                                        for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                        {
                                            if (!references.IdToStatusEffectDataMap.Value.TryGetValue(unsortedStatusEffects[x].StatusEffectDataId, out var unsortedReference))
                                                continue;
                                            ref UnmanagedStatusEffectData unsortedData = ref unsortedReference.Value;
                                            if ((unsortedData.Group & condition.SearchableGroup) != 0)
                                            {
                                                int alreadyUpdatingIndex = statusEffectStackUpdates.IndexOf(x);
                                                if (alreadyUpdatingIndex >= 0)
                                                {
                                                    if (unsortedStatusEffects[x].Stacks + statusEffectStackUpdates[alreadyUpdatingIndex].Stacks > 0)
                                                        exists = true;
                                                }
                                                else
                                                    exists = true;
                                                break;
                                            }
                                        }
                                        if (!exists)
                                            foreach (var indexedUpdate in statusEffectStackUpdates)
                                            {
                                                if (indexedUpdate.Stacks <= 0)
                                                    continue;

                                                if (references.IdToStatusEffectDataMap.Value.TryGetValue(indexedUpdate.StatusEffectDataId, out var indexedReference) 
                                                && (indexedReference.Value.Group & condition.SearchableGroup) != 0)
                                                    exists = true;
                                            }
                                        break;
                                    case ConditionalConfigurable.Name:
                                        for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                        {
                                            if (!references.IdToStatusEffectDataMap.Value.TryGetValue(unsortedStatusEffects[x].StatusEffectDataId, out var unsortedReference))
                                                continue;
                                            ref UnmanagedStatusEffectData unsortedData = ref unsortedReference.Value;
                                            if (unsortedData.ComparableName == condition.SearchableComparableName)
                                            {
                                                int alreadyUpdatingIndex = statusEffectStackUpdates.IndexOf(x);
                                                if (alreadyUpdatingIndex >= 0)
                                                {
                                                    if (unsortedStatusEffects[x].Stacks + statusEffectStackUpdates[alreadyUpdatingIndex].Stacks > 0)
                                                        exists = true;
                                                }
                                                else
                                                    exists = true;
                                                break;
                                            }
                                        }
                                        if (!exists)
                                            foreach (var indexedUpdate in statusEffectStackUpdates)
                                            {
                                                if (indexedUpdate.Stacks <= 0)
                                                    continue;

                                                if (references.IdToStatusEffectDataMap.Value.TryGetValue(indexedUpdate.StatusEffectDataId, out var indexedReference)
                                                && (indexedReference.Value.ComparableName == condition.SearchableComparableName))
                                                    exists = true;
                                            }
                                        break;
                                    case ConditionalConfigurable.Data:
                                        for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                        {
                                            if (unsortedStatusEffects[x].StatusEffectDataId == condition.SearchableData)
                                            {
                                                int alreadyUpdatingIndex = statusEffectStackUpdates.IndexOf(x);
                                                if (alreadyUpdatingIndex >= 0)
                                                {
                                                    if (unsortedStatusEffects[x].Stacks + statusEffectStackUpdates[alreadyUpdatingIndex].Stacks > 0)
                                                        exists = true;
                                                }
                                                else
                                                    exists = true;
                                                break;
                                            }
                                        }
                                        if (!exists)
                                            foreach (var indexedUpdate in statusEffectStackUpdates)
                                            {
                                                if (indexedUpdate.Stacks <= 0)
                                                    continue;

                                                if (indexedUpdate.StatusEffectDataId == condition.SearchableData)
                                                    exists = true;
                                            }
                                        break;
                                }
                                // If the condition is checking for existence and it doesn't exist or if
                                // its checking non-existence and does exist then skip this condition.
                                if ((condition.Exists && !exists)
                                || (!condition.Exists && exists))
                                    continue;

                                StatusEffectRequests conditionalRequest;
                                int conditionalStacks = condition.Add || condition.UseStacks ? condition.Stacks * (condition.Scaled ? request.Stacks : 1) : -1;

                                if (condition.Add)
                                {
                                    switch (condition.Timing)
                                    {
                                        case ConditionalTiming.Duration:
                                            conditionalRequest = StatusEffectRequests.AddWithDuration(condition.ActionData, condition.Duration, condition.Stacks * (condition.Scaled ? request.Stacks : 1));
                                            break;
                                        case ConditionalTiming.Inherited:
                                            conditionalRequest = new StatusEffectRequests
                                            {
                                                Type = StatusEffectRequestType.Add,
                                                Hash = condition.ActionData,
                                                Timing = request.Timing,
                                                Duration = request.Duration,
                                                Interval = request.Interval,
                                                Stacks = condition.Stacks * (condition.Scaled ? request.Stacks : 1),
                                                EventId = request.EventId
                                            };
                                            break;
                                        default:
                                            conditionalRequest = StatusEffectRequests.Add(condition.ActionData, condition.Stacks * (condition.Scaled ? request.Stacks : 1));
                                            break;
                                    }
                                    EvaluateRequest(ref statusEffectBuffer, conditionalRequest, references);
                                }
                                // Special case where the configurable which is the
                                // current data to be added is tagged for removal.
                                else if (condition.ActionData == statusEffectData.Id)
                                {
                                    if (!condition.UseStacks || conditionalStacks >= request.Stacks)
                                    {
                                        preventStatusEffect = true;
                                        if (condition.UseStacks)
                                            conditionalStacks -= request.Stacks;
                                        request.Stacks = 0;
                                        // In the case where other status effects of this Id exist we need to request to remove them.
                                        conditionalRequest = condition.ActionConfigurable switch
                                        {
                                            ConditionalConfigurable.Data => StatusEffectRequests.RemoveWithStatusEffectDataId(condition.ActionData, conditionalStacks),
                                            ConditionalConfigurable.Name => StatusEffectRequests.RemoveWithComparableName(condition.ActionComparableName, conditionalStacks),
                                            ConditionalConfigurable.AllGroups => StatusEffectRequests.RemoveWithGroup(condition.ActionGroup, conditionalStacks, true),
                                            ConditionalConfigurable.AnyGroups => StatusEffectRequests.RemoveWithGroup(condition.ActionGroup, conditionalStacks, false),
                                            _  => throw new InvalidOperationException($"Invalid {nameof(ConditionalConfigurable)} enum value of {condition.ActionConfigurable}.")
                                        };
                                        EvaluateRequest(ref statusEffectBuffer, conditionalRequest, references);
                                    }
                                    else
                                        request.Stacks -= conditionalStacks;
                                }
                                // Otherwise we remove effects.
                                else
                                {
                                    conditionalRequest = condition.ActionConfigurable switch
                                    {
                                        ConditionalConfigurable.Data => StatusEffectRequests.RemoveWithStatusEffectDataId(condition.ActionData, conditionalStacks),
                                        ConditionalConfigurable.Name => StatusEffectRequests.RemoveWithComparableName(condition.ActionComparableName, conditionalStacks),
                                        ConditionalConfigurable.AllGroups => StatusEffectRequests.RemoveWithGroup(condition.ActionGroup, conditionalStacks, true),
                                        ConditionalConfigurable.AnyGroups => StatusEffectRequests.RemoveWithGroup(condition.ActionGroup, conditionalStacks, false),
                                        _ => throw new InvalidOperationException($"Invalid {nameof(ConditionalConfigurable)} enum value of {condition.ActionConfigurable}.")
                                    };
                                    EvaluateRequest(ref statusEffectBuffer, conditionalRequest, references);
                                }
                            }

                            if (preventStatusEffect)
                                return;

                            if (flagForRemoval.Index >= 0)
                                RemoveStatusEffect(flagForRemoval, -1);

                            // Add the status effect
                            indexedStatusEffect.Duration = request.Duration;
                            AddStatusEffect(indexedStatusEffect, request.Stacks);
                        }
                        // If we aren't adding we are removing.
                        else
                        {
                            NativeList<IndexedStatusEffects> sortedStatusEffects = new NativeList<IndexedStatusEffects>(unsortedStatusEffects.Length, Allocator.Temp);

                            switch (request.RemovalType)
                            {
                                case StatusEffectRemovalType.Id:
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    {
                                        StatusEffects statusEffect = unsortedStatusEffects[x];
                                        if (statusEffect.Id == request.Id)
                                            sortedStatusEffects.Add(new IndexedStatusEffects(x, statusEffect));
                                    }
                                    break;
                                case StatusEffectRemovalType.StatusEffectDataId:
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    {
                                        StatusEffects statusEffect = unsortedStatusEffects[x];
                                        if (statusEffect.StatusEffectDataId == request.Hash)
                                            sortedStatusEffects.Add(new IndexedStatusEffects(x, statusEffect));
                                    }
                                    break;
                                case StatusEffectRemovalType.ComparableName:
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    {
                                        StatusEffects statusEffect = unsortedStatusEffects[x];
                                        ref UnmanagedStatusEffectData statusEffectData = ref references.IdToStatusEffectDataMap.Value[statusEffect.StatusEffectDataId].Value;
                                        if (statusEffectData.ComparableName == request.Hash)
                                            sortedStatusEffects.Add(new IndexedStatusEffects(x, statusEffect));
                                    }
                                    break;
                                case StatusEffectRemovalType.AnyGroups:
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    {
                                        StatusEffects statusEffect = unsortedStatusEffects[x];
                                        ref UnmanagedStatusEffectData statusEffectData = ref references.IdToStatusEffectDataMap.Value[statusEffect.StatusEffectDataId].Value;
                                        if ((statusEffectData.Group & request.Group) != 0)
                                            sortedStatusEffects.Add(new IndexedStatusEffects(x, statusEffect));
                                    }
                                    break;
                                case StatusEffectRemovalType.AllGroups:
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    {
                                        StatusEffects statusEffect = unsortedStatusEffects[x];
                                        ref UnmanagedStatusEffectData statusEffectData = ref references.IdToStatusEffectDataMap.Value[statusEffect.StatusEffectDataId].Value;
                                        if ((statusEffectData.Group & request.Group) == request.Group)
                                            sortedStatusEffects.Add(new IndexedStatusEffects(x, statusEffect));
                                    }
                                    break;
                                case StatusEffectRemovalType.Any:
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                        sortedStatusEffects.Add(new IndexedStatusEffects(x, unsortedStatusEffects[x]));
                                    break;
                            }
                            // Sort by value and duration.
                            sortedStatusEffects.Sort(new IndexedStatusEffectComparer(references, false));
                            // Remove until max stack limit reached.
                            int removedCount = 0;
                            for (int x = 0; x < sortedStatusEffects.Length; x++)
                            {
                                if (request.Stacks >= 0 && removedCount >= request.Stacks)
                                    break;

                                IndexedStatusEffects indexedStatusEffect = sortedStatusEffects.ElementAt(x);
                                var count = RemoveStatusEffect(indexedStatusEffect, request.Stacks - removedCount);
                                removedCount += count;
                            }
                            sortedStatusEffects.Dispose();
                        }
                    }
                    
                    void AddStatusEffect(IndexedStatusEffects indexedStatusEffect, int stacks = 1)
                    {
                        // Check to see if we are updating the index already.
                        int alreadyUpdatingIndex = statusEffectStackUpdates.IndexOf(indexedStatusEffect.Index);
                        if (alreadyUpdatingIndex >= 0)
                        {
                            ref IndexedStatusEffects indexedUpdate = ref statusEffectStackUpdates.ElementAt(alreadyUpdatingIndex);
                            indexedUpdate.Stacks += stacks;
                            return;
                        }
                        // Check to see if we are adding an infinite effect already.
                        if (indexedStatusEffect.Timing is StatusEffectTiming.Infinite)
                        {
                            for (int x = 0; x < statusEffectStackUpdates.Length; x++)
                            {
                                ref IndexedStatusEffects indexedUpdate = ref statusEffectStackUpdates.ElementAt(x);
                                if (indexedUpdate.Timing is StatusEffectTiming.Infinite && indexedUpdate.StatusEffectDataId == indexedStatusEffect.StatusEffectDataId)
                                {
                                    indexedUpdate.Stacks += stacks;
                                    return;
                                }
                            }
                        }
                        // Otherwise create a new stack.
                        indexedStatusEffect.Stacks = stacks;
                        statusEffectStackUpdates.Add(indexedStatusEffect);
                    }

                    // Returns the amount removed
                    int RemoveStatusEffect(IndexedStatusEffects indexedStatusEffect, int stacks = 1)
                    {
                        int alreadyUpdatingIndex = statusEffectStackUpdates.IndexOf(indexedStatusEffect.Index);
                        if (alreadyUpdatingIndex >= 0)
                        {
                            ref IndexedStatusEffects indexedUpdate = ref statusEffectStackUpdates.ElementAt(alreadyUpdatingIndex);
                            if (stacks >= 0)
                            {
                                int currentStackCount = indexedStatusEffect.Stacks + indexedUpdate.Stacks;

                                if (currentStackCount > stacks)
                                {
                                    indexedUpdate.Stacks -= stacks;
                                    return stacks;
                                }
                            }
                            // If it got to this point we can remove the effect. Either
                            // we removed all its stacks or there was no stack count.
                            var removedCount = indexedStatusEffect.Stacks + indexedUpdate.Stacks;
                            indexedUpdate.Stacks = -indexedStatusEffect.Stacks;
                            return removedCount;
                        }
                        else
                        {
                            if (stacks >= 0)
                            {
                                if (indexedStatusEffect.Stacks > stacks)
                                {
                                    indexedStatusEffect.Stacks = -stacks;
                                    statusEffectStackUpdates.Add(indexedStatusEffect);
                                    return stacks;
                                }
                            }
                            // If it got to this point we can remove the effect. Either
                            // we removed all its stacks or there was no stack count.
                            var removedCount = indexedStatusEffect.Stacks;
                            indexedStatusEffect.Stacks = -removedCount;
                            statusEffectStackUpdates.Add(indexedStatusEffect);
                            return removedCount;
                        }
                    }
                }

                statusEffectRequests.Clear();

                // Remove and add status effects from buffer.
                statusEffectStackUpdates.Sort(new IndexedStatusEffectComparer(References, true));
                UnsafeHashSet<TypeIndex> existingTypes = new UnsafeHashSet<TypeIndex>(1, Allocator.Temp);
                UnsafeHashSet<TypeIndex> removingTypes = new UnsafeHashSet<TypeIndex>(1, Allocator.Temp);
                bool statusEffectEventsEnabled = statusEffectEventsEnabledRW.ValueRO;
                
                // Iterate in reverse to not skip any that get removed.
                for (int v = statusEffectStackUpdates.Length - 1; v >= 0; v--)
                {
                    IndexedStatusEffects indexedUpdate = statusEffectStackUpdates[v];

                    if (indexedUpdate.Stacks == 0)
                        continue;

                    // Check if we add a new status effect.
                    if (indexedUpdate.Index < 0)
                    {
                        uint id = statusManager.AvailableId++;
                        statusEffectEvents.Add(new StatusEffectEvents(id, indexedUpdate.StatusEffectDataId));
                        if (!statusEffectEventsEnabled)
                        {
                            statusEffectEventsEnabled = true;
                            statusEffectEventsEnabledRW.ValueRW = true;
                            preEvaluateUpdateRW.ValueRW = true;
                        }
                        statusEffects.Add(new StatusEffects()
                        {
#if NETCODE
                            TickAdded = NetworkTime.ServerTick,
                            TickUpdated = NetworkTime.ServerTick,
#else
                            TimeAdded = ElapsedTime,
#endif
                            Id = id,
                            StatusEffectDataId = indexedUpdate.StatusEffectDataId,
                            Timing = indexedUpdate.Timing,
                            Duration = indexedUpdate.Duration,
                            Interval = indexedUpdate.Interval,
                            Stacks = indexedUpdate.Stacks,
                            EventId = indexedUpdate.EventId,
                        });

                        if (Hint.Unlikely(!References.IdToStatusEffectDataMap.Value.TryGetValue(indexedUpdate.StatusEffectDataId, out var reference)))
                        {
                            UnityEngine.Debug.LogError($"Failed to find status effect data for hash ID: {indexedUpdate.StatusEffectDataId}");
                            continue;
                        }

                        ref UnmanagedStatusEffectData statusEffectData = ref reference.Value;
                        // Add dynamic effects to the buffers.
                        AddDynamicValue(sortKey,
                            entity,
                            id,
                            ref statusEffectData,
                            ref EndStatusEffectEntityCommandBuffer,
                            ref dynamicFloats,
                            ref dynamicInts,
                            ref dynamicBools);

                        continue;
                    }

                    ref var updatingStatusEffectRef = ref statusEffects.ElementAt(indexedUpdate.Index);
                    // If all the stacks are removed we remove the effect.
                    if (updatingStatusEffectRef.Stacks + indexedUpdate.Stacks <= 0)
                    {
                        statusEffectEvents.Add(new StatusEffectEvents(updatingStatusEffectRef.Id, updatingStatusEffectRef.StatusEffectDataId, updatingStatusEffectRef.Stacks, StatusEffectEvent.Removed));
                        if (!statusEffectEventsEnabled)
                        {
                            statusEffectEventsEnabled = true;
                            statusEffectEventsEnabledRW.ValueRW = true;
                            preEvaluateUpdateRW.ValueRW = true;
                        }
                        statusEffects.RemoveAtSwapBack(indexedUpdate.Index);
                        // Remove dynamic effects from the buffers.
                        existingTypes.Clear();
                        removingTypes.Clear();

                        RemoveIdAndBuildTypeList(updatingStatusEffectRef.Id, ref dynamicFloats, ref dynamicInts, ref dynamicBools, ref existingTypes, ref removingTypes);

                        foreach (var type in removingTypes)
                            if (!existingTypes.Contains(type))
                                EndStatusEffectEntityCommandBuffer.RemoveComponent(sortKey, entity, ComponentType.ReadOnly(type));
                    }
                    // Otherwise just update the stack count.
                    else
                    {
                        statusEffectEvents.Add(new StatusEffectEvents(updatingStatusEffectRef.Id, updatingStatusEffectRef.StatusEffectDataId, updatingStatusEffectRef.Stacks, StatusEffectEvent.Updated));
                        if (!statusEffectEventsEnabled)
                        {
                            statusEffectEventsEnabled = true;
                            statusEffectEventsEnabledRW.ValueRW = true;
                            preEvaluateUpdateRW.ValueRW = true;
                        }
                        updatingStatusEffectRef.Stacks += indexedUpdate.Stacks;
                        updatingStatusEffectRef.TickUpdated = NetworkTime.ServerTick;
                    }
                }

                existingTypes.Dispose();
                removingTypes.Dispose();

                statusEffectStackUpdates.Dispose();
            }
        }
    }
}
#endif