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
    /// Applies <see cref="StatusEffectRequests"/> and expires <see cref="StatusEffects"/>. Runs in the
    /// <see cref="StatusEffectSystemGroup"/>, or the <c>PredictedStatusEffectSystemGroup</c> with Netcode.
    /// </summary>
    /// <remarks>
    /// Custom systems that decrement <see cref="StatusEffectTiming.Event"/> and
    /// <see cref="StatusEffectTiming.Predicate"/> <see cref="StatusEffects"/> should update in the same
    /// group as this system, so expiry is checked against up to date durations.
    /// <para>
    /// Systems that act on modules should update after that group, once the module buffers have been
    /// added or removed. For gameplay that means the <see cref="SimulationSystemGroup"/>, or the
    /// <c>PredictedSimulationSystemGroup</c> with Netcode so modules are predicted along with the
    /// effects. Both status effect groups update first in their parent group, so the default placement
    /// is already after them. Presentation only modules, such as visual effects, can stay in the
    /// <see cref="SimulationSystemGroup"/> on the client.
    /// </para>
    /// </remarks>
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
#if NETCODE
        private EntityQuery m_ClearEventsQuery;
#endif

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_RequestsQuery = SystemAPI.QueryBuilder().WithAllRW<StatusEffects>().WithPresentRW<StatusEffectEvents, StatusVariablePreEvaluateUpdate>().WithAll<Simulate>().WithAllRW<StatusManagerComponent, StatusEffectRequests>().Build();
            m_RequestsQuery.AddChangedVersionFilter(ComponentType.ReadWrite<StatusEffectRequests>());
            m_StatusEffectsQuery = SystemAPI.QueryBuilder().WithAllRW<StatusEffects>().WithPresentRW<StatusEffectEvents, StatusVariablePreEvaluateUpdate>().WithAll<Simulate>().Build();
#if NETCODE
            // Only matches entities with enabled events, so ones with nothing to clear are skipped.
            m_ClearEventsQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents, Simulate>().Build();
#endif

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
#else
            var elapsedTime = SystemAPI.Time.ElapsedTime;
#endif

            // Update and check durations.
            var statusEffectsJob = new StatusEffectsJob
            {
#if NETCODE
                NetworkTime = networkTime,
                TickRate = tickRate,
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
                TickRate = tickRate,
#else
                ElapsedTime = elapsedTime,
#endif
                Registry = registry,
            };
            state.Dependency = statusEffectRequestsJob.ScheduleParallelByRef(m_RequestsQuery, state.Dependency);

#if NETCODE
            // Clear predicted events once prediction ends so systems after the prediction group don't see
            // events from ticks that get re-simulated every frame. Scheduled after the requests job so
            // events added this tick are included.
            if (!state.WorldUnmanaged.IsServer() && networkTime.IsFinalPredictionTick)
            {
                var clearEventsJob = new ClearEventsAfterPredictionJob
                {
                    CommandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                };
                state.Dependency = clearEventsJob.ScheduleParallelByRef(m_ClearEventsQuery, state.Dependency);
            }
#endif
        }

#if NETCODE
        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        internal partial struct ClearEventsAfterPredictionJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
            {
                // SetBuffer already replaces the buffer with an empty one, so no Clear() is needed.
                CommandBuffer.SetBuffer<StatusEffectEvents>(sortKey, entity);
                CommandBuffer.SetComponentEnabled<StatusEffectEvents>(sortKey, entity, false);
            }
        }
#endif

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        internal partial struct StatusEffectsJob : IJobChunk
        {
#if NETCODE
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;
#else
            public double ElapsedTime;
#endif
            public BufferTypeHandle<StatusEffects> StatusEffectsHandle;
            public BufferTypeHandle<StatusEffectEvents> StatusEffectEventsHandle;
            public ComponentTypeHandle<StatusVariablePreEvaluateUpdate> PreEvaluateUpdateHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var statusEffectsAccessorRO = chunk.GetBufferAccessorRO(ref StatusEffectsHandle);
                var statusEffectEventsAccessorRO = chunk.GetBufferAccessorRO(ref StatusEffectEventsHandle);
                BufferAccessor<StatusEffects> statusEffectsAccessorRW = default;
                BufferAccessor<StatusEffectEvents> statusEffectEventsAccessorRW = default;
                bool hasStatusEffectsWriteAccess = false;
                bool hasStatusEffectEventsWriteAccess = false;

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    bool hasExpiredStatusEffect = HasExpiredStatusEffect(statusEffectsAccessorRO[i]);

                    // Only bump the change version of events in chunks with old events to clear or new ones to add.
                    if (hasExpiredStatusEffect || statusEffectEventsAccessorRO[i].Length > 0)
                    {
                        if (!hasStatusEffectEventsWriteAccess)
                        {
                            statusEffectEventsAccessorRW = chunk.GetBufferAccessorRW(ref StatusEffectEventsHandle);
                            hasStatusEffectEventsWriteAccess = true;
                        }

                        statusEffectEventsAccessorRW[i].Clear();
                    }

                    if (!hasExpiredStatusEffect)
                    {
                        if (chunk.IsComponentEnabled(ref StatusEffectEventsHandle, i))
                            chunk.SetComponentEnabled(ref StatusEffectEventsHandle, i, false);
                        continue;
                    }

                    // Only bump the change version of chunks that actually have expired effects.
                    if (!hasStatusEffectsWriteAccess)
                    {
                        statusEffectsAccessorRW = chunk.GetBufferAccessorRW(ref StatusEffectsHandle);
                        hasStatusEffectsWriteAccess = true;
                    }

                    var statusEffects = statusEffectsAccessorRW[i];
                    var statusEffectEvents = statusEffectEventsAccessorRW[i];

                    // Iterate in reverse to not skip any that get removed. RemoveAt keeps the
                    // order of the remaining effects, matching StatusEffectRequestsJob. The buffer
                    // must stay sorted by InstanceId for InterpolatedStatusEffectEventsSystem.
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
            public ClientServerTickRate TickRate;
#else
            public double ElapsedTime;
#endif
            public UnmanagedStatusRegistry Registry;

            void Execute(EnabledRefRW<StatusEffectEvents> statusEffectEventsEnabledRW,
                ref StatusManagerComponent statusManager,
                ref DynamicBuffer<StatusEffectRequests> statusEffectRequests,
                ref DynamicBuffer<StatusEffects> statusEffects,
                ref DynamicBuffer<StatusEffectEvents> statusEffectEvents,
                EnabledRefRW<StatusVariablePreEvaluateUpdate> preEvaluateUpdateRW)
            {
                // If nothing to change then continue.
                if (statusEffectRequests.Length <= 0)
                    return;

                // Requests are applied directly to the buffer so every request sees the result of
                // the ones before it. Removed effects are only marked with 0 stacks so buffer indices
                // stay valid until everything is compacted at the end. Events are then built by
                // comparing the buffer against this snapshot.
                using var originalStatusEffects = new NativeArray<StatusEffects>(statusEffects.AsNativeArray(), Allocator.Temp);
                using var removalCandidates = new NativeList<IndexedStatusEffects>(statusEffects.Length, Allocator.Temp);

                var processor = new StatusEffectRequestProcessor
                {
                    Buffer = statusEffects,
                    Manager = statusManager,
                    Registry = Registry,
                    RemovalCandidates = removalCandidates,
#if NETCODE
                    CurrentTick = NetworkTime.ServerTick,
                    CurrentTickFraction = NetworkTime.ServerTickFraction,
                    TickRate = TickRate,
#else
                    ElapsedTime = ElapsedTime,
#endif
                };

                for (int i = 0; i < statusEffectRequests.Length; i++)
                    processor.Process(statusEffectRequests[i]);

                statusManager = processor.Manager;
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
                        statusEffect.TickUpdated = NetworkTime.ServerTick;
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

                // Compact out removed effects, keeping the order of the rest. The buffer must
                // stay sorted by InstanceId for InterpolatedStatusEffectEventsSystem.
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
            }
        }
    }
}
