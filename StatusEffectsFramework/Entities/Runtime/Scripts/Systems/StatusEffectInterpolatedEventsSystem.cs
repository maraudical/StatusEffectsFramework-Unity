#if ENTITIES && NETCODE
using Unity.Burst;
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

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusEffectsInterpolatedEventsJob : IJobEntity
        {
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;

            public void Execute([ChunkIndexInQuery] int sortKey, 
                Entity entity, 
                EnabledRefRW<StatusEffectEvents> statusEffectEventsEnabledRW,
                in DynamicBuffer<StatusEffects> statusEffects,
                ref DynamicBuffer<StatusEffectEvents> statusEffectEvents, 
                ref DynamicBuffer<InterpolatedStatusEffects> interpolatedStatusEffects)
            {
                int length = statusEffects.Length;
                int interpolatedLength = interpolatedStatusEffects.Length;
                // We copy to a new array here so that we don't sort the underlying
                // buffer and cause another changed version to trigger when the status
                // effects are synced back to the server.
                using var statusEffectsArray = statusEffects.ToNativeArray(Allocator.Temp);
                using var interpolatedStatusEffectsArray = interpolatedStatusEffects.ToNativeArray(Allocator.Temp);

                interpolatedStatusEffects.Clear();
                foreach (var copy in statusEffects)
                    interpolatedStatusEffects.Add(new InterpolatedStatusEffects { Id = copy.Id, StatusEffectDataId = copy.StatusEffectDataId, Stacks = copy.Stacks });

                statusEffectsArray.Sort();
                interpolatedStatusEffectsArray.Sort();
                
                var enumerator = statusEffectsArray.GetEnumerator();
                var interpolatedEnumerator = interpolatedStatusEffectsArray.GetEnumerator();

                bool hasStatusEffects = enumerator.MoveNext();
                bool hasInterpolatedStatusEffects = interpolatedEnumerator.MoveNext();
                StatusEffects statusEffect;
                InterpolatedStatusEffects interpolatedStatusEffect;

                bool statusEffectEventsEnabled = statusEffectEventsEnabledRW.ValueRO;

                for (; ; )
                {
                    statusEffect = enumerator.Current;
                    interpolatedStatusEffect = interpolatedEnumerator.Current;
                    // Iterate on status effects until we reach similar ids.
                    if (hasStatusEffects)
                    {
                        if (!hasInterpolatedStatusEffects)
                        {
                            // Loop through remaining status effects, trigger added events.
                            do
                            {
                                statusEffect = enumerator.Current;
                                bool isOld = NetworkTime.InterpolationTick.TimeSince(statusEffect.TickAdded, TickRate) > StatusEffectEvents.SecondsTillOldThreshold;
                                statusEffectEvents.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId, isOld));
                                if (!statusEffectEventsEnabled)
                                {
                                    statusEffectEventsEnabled = true;
                                    statusEffectEventsEnabledRW.ValueRW = true;
                                }
                            }
                            while (enumerator.MoveNext());
                            break;
                        }
                        else if (statusEffect.Id < interpolatedStatusEffect.Id)
                        {
                            hasStatusEffects = enumerator.MoveNext();
                            // Status effect was added, trigger added event.
                            bool isOld = NetworkTime.InterpolationTick.TimeSince(statusEffect.TickAdded, TickRate) > StatusEffectEvents.SecondsTillOldThreshold;
                            statusEffectEvents.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId, isOld));
                            if (!statusEffectEventsEnabled)
                            {
                                statusEffectEventsEnabled = true;
                                statusEffectEventsEnabledRW.ValueRW = true;
                            }
                        }
                        else if (statusEffect.Id > interpolatedStatusEffect.Id)
                        {
                            hasInterpolatedStatusEffects = interpolatedEnumerator.MoveNext();
                            // Status effect was removed, trigger removed event.
                            statusEffectEvents.Add(new StatusEffectEvents(interpolatedStatusEffect.Id, interpolatedStatusEffect.StatusEffectDataId, interpolatedStatusEffect.Stacks, StatusEffectEvent.Removed));
                            if (!statusEffectEventsEnabled)
                            {
                                statusEffectEventsEnabled = true;
                                statusEffectEventsEnabledRW.ValueRW = true;
                            }
                        }
                        else if (statusEffect.StatusEffectDataId == interpolatedStatusEffect.StatusEffectDataId)
                        {
                            hasStatusEffects = enumerator.MoveNext();
                            hasInterpolatedStatusEffects = interpolatedEnumerator.MoveNext();

                            if (statusEffect.Stacks == interpolatedStatusEffect.Stacks)
                                continue;
                            // Status effect was updated, trigger updated event.
                            bool isOld = NetworkTime.InterpolationTick.TimeSince(statusEffect.TickUpdated, TickRate) > StatusEffectEvents.SecondsTillOldThreshold;
                            statusEffectEvents.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId, interpolatedStatusEffect.Stacks, StatusEffectEvent.Updated, isOld));
                            if (!statusEffectEventsEnabled)
                            {
                                statusEffectEventsEnabled = true;
                                statusEffectEventsEnabledRW.ValueRW = true;
                            }
                        }
                        else
                        {
                            hasStatusEffects = enumerator.MoveNext();
                            hasInterpolatedStatusEffects = interpolatedEnumerator.MoveNext();
                            // Status effect was updated with a different status effect data, trigger removed and added events.
                            statusEffectEvents.Add(new StatusEffectEvents(interpolatedStatusEffect.Id, interpolatedStatusEffect.StatusEffectDataId, interpolatedStatusEffect.Stacks, StatusEffectEvent.Removed));
                            bool isOld = NetworkTime.InterpolationTick.TimeSince(statusEffect.TickAdded, TickRate) > StatusEffectEvents.SecondsTillOldThreshold;
                            statusEffectEvents.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId, isOld));
                            if (!statusEffectEventsEnabled)
                            {
                                statusEffectEventsEnabled = true;
                                statusEffectEventsEnabledRW.ValueRW = true;
                            }
                        }
                    }
                    else if (hasInterpolatedStatusEffects)
                    {
                        // Loop through remaining interpolated status effects, trigger removed events.
                        do
                        {
                            interpolatedStatusEffect = interpolatedEnumerator.Current;
                            statusEffectEvents.Add(new StatusEffectEvents(interpolatedStatusEffect.Id, interpolatedStatusEffect.StatusEffectDataId, interpolatedStatusEffect.Stacks, StatusEffectEvent.Removed));
                            if (!statusEffectEventsEnabled)
                            {
                                statusEffectEventsEnabled = true;
                                statusEffectEventsEnabledRW.ValueRW = true;
                            }
                        }
                        while (interpolatedEnumerator.MoveNext());
                        break;
                    }
                    else
                        break;
                }
            }
        }
    }
}
#endif