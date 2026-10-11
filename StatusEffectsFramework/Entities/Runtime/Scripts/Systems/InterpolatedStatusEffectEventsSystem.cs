#if NETCODE
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffectsFramework.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
    [BurstCompile]
    public partial struct InterpolatedStatusEffectEventsSystem : ISystem
    {
        private EntityQuery m_StatusEffectEventsQuery;
        private EntityQuery m_StatusEffectsInterpolatedEventsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_StatusEffectEventsQuery = SystemAPI.QueryBuilder().WithAllRW<StatusEffectEvents>().WithAll<InterpolatedStatusEffects>().Build();
            m_StatusEffectsInterpolatedEventsQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects>().WithAllRW<InterpolatedStatusEffects>().WithPresentRW<StatusEffectEvents>().Build();
            m_StatusEffectsInterpolatedEventsQuery.SetChangedVersionFilter(ComponentType.ReadOnly<StatusEffects>());

            state.RequireForUpdate(m_StatusEffectsInterpolatedEventsQuery);
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();

            state.Dependency = new ClearStatusEffectEventsJob().ScheduleParallel(m_StatusEffectEventsQuery, state.Dependency);

            var statusEffectsInterpolatedEventsJob = new StatusEffectsInterpolatedEventsJob
            {
                NetworkTime = SystemAPI.GetSingleton<NetworkTime>(),
                TickRate = tickRate,
                PredictedGhostHandle = SystemAPI.GetComponentTypeHandle<PredictedGhost>(true),
                StatusEffectsHandle = SystemAPI.GetBufferTypeHandle<StatusEffects>(true),
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(),
                InterpolatedStatusEffectsHandle = SystemAPI.GetBufferTypeHandle<InterpolatedStatusEffects>(),
            };
            state.Dependency = statusEffectsInterpolatedEventsJob.ScheduleParallelByRef(m_StatusEffectsInterpolatedEventsQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct ClearStatusEffectEventsJob : IJobEntity
        {
            public void Execute(EnabledRefRW<StatusEffectEvents> statusEffectEventsEnabledRW,
                ref DynamicBuffer<StatusEffectEvents> statusEffectEvents)
            {
                statusEffectEventsEnabledRW.ValueRW = false;
                statusEffectEvents.Clear();
            } 
        }

        // An IJobChunk rather than an IJobEntity so the events, the snapshot and the events enabled bits
        // are only taken read-write, and their change versions bumped, for chunks that actually differ.
        // The change filter on StatusEffects lets through every chunk that received a snapshot.
        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        struct StatusEffectsInterpolatedEventsJob : IJobChunk
        {
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;
            [ReadOnly] public ComponentTypeHandle<PredictedGhost> PredictedGhostHandle;
            [ReadOnly] public BufferTypeHandle<StatusEffects> StatusEffectsHandle;
            public BufferTypeHandle<StatusEffectEvents> StatusEffectEventsHandle;
            public BufferTypeHandle<InterpolatedStatusEffects> InterpolatedStatusEffectsHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Predicted ghosts run on the server tick, interpolated ghosts on the interpolation tick.
                var currentTick = chunk.Has(ref PredictedGhostHandle) ? NetworkTime.ServerTick : NetworkTime.InterpolationTick;

                var statusEffectsAccessor = chunk.GetBufferAccessorRO(ref StatusEffectsHandle);
                var statusEffectEventsAccessor = new LazyBufferAccessor<StatusEffectEvents>(chunk, ref StatusEffectEventsHandle);
                var interpolatedStatusEffectsAccessor = new LazyBufferAccessor<InterpolatedStatusEffects>(chunk, ref InterpolatedStatusEffectsHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var e))
                {
                    var statusEffects = statusEffectsAccessor[e];

                    // Both buffers are ordered by InstanceId: status effects are only appended with an
                    // increasing AvailableId and removed in order, and the interpolated buffer is rebuilt
                    // below in the same order. This lets us diff them with a single two-index merge.
                    CheckSortedByInstanceId(statusEffects);

                    // Every difference in InstanceId, Id or Stacks emits an event, so a matching snapshot
                    // means there are no events to add and nothing to write.
                    if (MatchesSnapshot(statusEffects, interpolatedStatusEffectsAccessor.GetReadOnly(e)))
                        continue;

                    var statusEffectEvents = statusEffectEventsAccessor.GetReadWrite(e);
                    var interpolatedStatusEffects = interpolatedStatusEffectsAccessor.GetReadWrite(e);

                    int length = statusEffects.Length;
                    int interpolatedLength = interpolatedStatusEffects.Length;
                    int i = 0, j = 0;

                    while (i < length || j < interpolatedLength)
                    {
                        if (j == interpolatedLength || (i < length && statusEffects[i].InstanceId < interpolatedStatusEffects[j].InstanceId))
                        {
                            // Status effect was added, trigger added event.
                            var statusEffect = statusEffects[i++];
                            statusEffectEvents.Add(new StatusEffectEvents(statusEffect.InstanceId, statusEffect.Id, IsOld(currentTick, statusEffect.TickAdded)));
                        }
                        else if (i == length || statusEffects[i].InstanceId > interpolatedStatusEffects[j].InstanceId)
                        {
                            // Status effect was removed, trigger removed event.
                            var interpolatedStatusEffect = interpolatedStatusEffects[j++];
                            statusEffectEvents.Add(new StatusEffectEvents(interpolatedStatusEffect.InstanceId, interpolatedStatusEffect.Id, interpolatedStatusEffect.Stacks, StatusEffectEvent.Removed));
                        }
                        else
                        {
                            var statusEffect = statusEffects[i++];
                            var interpolatedStatusEffect = interpolatedStatusEffects[j++];
                            if (statusEffect.Id != interpolatedStatusEffect.Id)
                            {
                                // Status effect was updated with a different status effect data, trigger removed and added events.
                                // Instance ids aren't currently reused for different data, this is kept defensively.
                                statusEffectEvents.Add(new StatusEffectEvents(interpolatedStatusEffect.InstanceId, interpolatedStatusEffect.Id, interpolatedStatusEffect.Stacks, StatusEffectEvent.Removed));
                                statusEffectEvents.Add(new StatusEffectEvents(statusEffect.InstanceId, statusEffect.Id, IsOld(currentTick, statusEffect.TickAdded)));
                            }
                            else if (statusEffect.Stacks != interpolatedStatusEffect.Stacks)
                            {
                                // Status effect was updated, trigger updated event.
                                statusEffectEvents.Add(new StatusEffectEvents(statusEffect.InstanceId, statusEffect.Id, interpolatedStatusEffect.Stacks, StatusEffectEvent.Updated, IsOld(currentTick, statusEffect.TickUpdated)));
                            }
                        }
                    }

                    chunk.SetComponentEnabled(ref StatusEffectEventsHandle, e, true);

                    // Snapshot the status effects for the next diff. Done after the merge so the
                    // previous snapshot doesn't need to be copied first.
                    interpolatedStatusEffects.ResizeUninitialized(length);
                    for (int k = 0; k < length; k++)
                    {
                        var statusEffect = statusEffects[k];
                        interpolatedStatusEffects[k] = new InterpolatedStatusEffects { InstanceId = statusEffect.InstanceId, Id = statusEffect.Id, Stacks = statusEffect.Stacks };
                    }
                }
            }

            private static bool MatchesSnapshot(in DynamicBuffer<StatusEffects> statusEffects, in DynamicBuffer<InterpolatedStatusEffects> interpolatedStatusEffects)
            {
                if (statusEffects.Length != interpolatedStatusEffects.Length)
                    return false;

                for (int k = 0; k < statusEffects.Length; k++)
                {
                    var statusEffect = statusEffects[k];
                    var interpolatedStatusEffect = interpolatedStatusEffects[k];
                    if (statusEffect.InstanceId != interpolatedStatusEffect.InstanceId
                        || statusEffect.Id != interpolatedStatusEffect.Id
                        || statusEffect.Stacks != interpolatedStatusEffect.Stacks)
                        return false;
                }

                return true;
            }

            private bool IsOld(NetworkTick currentTick, NetworkTick tick) => currentTick.TimeSince(tick, TickRate) > StatusEffectEvents.SecondsTillOldThreshold;

            [System.Diagnostics.Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), System.Diagnostics.Conditional("UNITY_DOTS_DEBUG")]
            private static void CheckSortedByInstanceId(in DynamicBuffer<StatusEffects> statusEffects)
            {
                for (int i = 1; i < statusEffects.Length; i++)
                    if (statusEffects[i - 1].InstanceId >= statusEffects[i].InstanceId)
                        throw new System.InvalidOperationException("StatusEffects buffer must be sorted by strictly increasing InstanceId. Only append new status effects and remove them in order (no swap back or sorting).");
            }
        }
    }
}
#endif