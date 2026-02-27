#if ENTITIES && NETCODE
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
    [BurstCompile]
    public partial struct InterpolatedStatusEffectEventsSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<ActiveStatusEffects>().WithAllRW<InterpolatedStatusEffects>().WithPresentRW<StatusEffectEvents>().Build();
            m_EntityQuery.SetChangedVersionFilter(ComponentType.ReadOnly<ActiveStatusEffects>());

            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new StatusEffectsInterpolatedEventsJob().ScheduleParallel(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusEffectsInterpolatedEventsJob : IJobEntity
        {
            public unsafe void Execute([ChunkIndexInQuery] int sortKey, 
                Entity entity, 
                EnabledRefRW<StatusEffectEvents> statusEffectEventsEnabledRW,
                in DynamicBuffer<ActiveStatusEffects> statusEffects,
                ref DynamicBuffer<StatusEffectEvents> statusEffectEvents, 
                ref DynamicBuffer<InterpolatedStatusEffects> interpolatedStatusEffects)
            {
                statusEffectEvents.Clear();

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

                bool hasStatusEffects;
                bool hasInterpolatedStatusEffects;
                ActiveStatusEffects statusEffect;
                InterpolatedStatusEffects interpolatedStatusEffect;

                bool statusEffectEventsEnabled = statusEffectEventsEnabledRW.ValueRO;;

                for (; ; )
                {
                    hasStatusEffects = enumerator.MoveNext();
                    hasInterpolatedStatusEffects = interpolatedEnumerator.MoveNext();
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
                                UnityEngine.Debug.Log("added");
                                statusEffect = enumerator.Current;
                                statusEffectEvents.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId));
                                if (!statusEffectEventsEnabled)
                                {
                                    statusEffectEventsEnabled = true;
                                    statusEffectEventsEnabledRW.ValueRW = true;
                                }
                            }
                            while (enumerator.MoveNext());
                        }
                        else if (statusEffect.Id < interpolatedStatusEffect.Id)
                        {
                            UnityEngine.Debug.Log("added");
                            // Status effect was added, trigger added event.
                            statusEffectEvents.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId));
                            if (!statusEffectEventsEnabled)
                            {
                                statusEffectEventsEnabled = true;
                                statusEffectEventsEnabledRW.ValueRW = true;
                            }
                        }
                        else if (statusEffect.Id > interpolatedStatusEffect.Id)
                        {
                            UnityEngine.Debug.Log("removed");
                            // Status effect was removed, trigger removed event.
                            statusEffectEvents.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId, interpolatedStatusEffect.Stacks, StatusEffectEvent.Removed));
                            if (!statusEffectEventsEnabled)
                            {
                                statusEffectEventsEnabled = true;
                                statusEffectEventsEnabledRW.ValueRW = true;
                            }
                        }
                        else if (statusEffect.Stacks != interpolatedStatusEffect.Stacks)
                        {
                            UnityEngine.Debug.Log("updated");
                            // Status effect was updated, trigger updated event.
                            statusEffectEvents.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId, interpolatedStatusEffect.Stacks, StatusEffectEvent.Updated));
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
                            UnityEngine.Debug.Log("removed");
                            interpolatedStatusEffect = interpolatedEnumerator.Current;
                            statusEffectEvents.Add(new StatusEffectEvents(interpolatedStatusEffect.Id, interpolatedStatusEffect.StatusEffectDataId, interpolatedStatusEffect.Stacks, StatusEffectEvent.Removed));
                            if (!statusEffectEventsEnabled)
                            {
                                statusEffectEventsEnabled = true;
                                statusEffectEventsEnabledRW.ValueRW = true;
                            }
                        }
                        while (interpolatedEnumerator.MoveNext());
                    }
                    else
                        break;
                }

                if (statusEffectEventsEnabled && statusEffectEvents.Length == 0)
                    statusEffectEventsEnabledRW.ValueRW = false;
            }
        }
    }
}
#endif