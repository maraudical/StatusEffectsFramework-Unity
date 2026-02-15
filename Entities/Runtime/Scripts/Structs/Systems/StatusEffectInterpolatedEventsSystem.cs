#if ENTITIES && NETCODE
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

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
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, InterpolatedStatusEffects>().Build();
            m_EntityQuery.SetChangedVersionFilter(ComponentType.ReadOnly<StatusEffects>());

            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusEffectsInterpolatedEventsJob = new StatusEffectsInterpolatedEventsJob
            {
                EndStatusEffectEntityCommandBuffer = SystemAPI.GetSingleton<EndStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                BeginSimulationEntityCommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };
            state.Dependency = statusEffectsInterpolatedEventsJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusEffectsInterpolatedEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter EndStatusEffectEntityCommandBuffer;
            public EntityCommandBuffer.ParallelWriter BeginSimulationEntityCommandBuffer;

            public unsafe void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<StatusEffects> statusEffects, ref DynamicBuffer<InterpolatedStatusEffects> interpolatedStatusEffects)
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

                bool hasStatusEffects;
                bool hasInterpolatedStatusEffects;
                StatusEffects statusEffect;
                InterpolatedStatusEffects interpolatedStatusEffect;

                var events = EndStatusEffectEntityCommandBuffer.AddBuffer<StatusEffectEvents>(sortKey, entity);
                // Preemptively remove the buffer so that it only lasts for one frame.
                BeginSimulationEntityCommandBuffer.RemoveComponent<StatusEffectEvents>(sortKey, entity);

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
                                statusEffect = enumerator.Current;
                                events.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId));
                            }
                            while (enumerator.MoveNext());
                        }
                        else if (statusEffect.Id < interpolatedStatusEffect.Id)
                        {
                            // Status effect was added, trigger added event.
                            events.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId));
                        }
                        else if (statusEffect.Id > interpolatedStatusEffect.Id)
                        {
                            // Status effect was removed, trigger removed event.
                            events.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId, interpolatedStatusEffect.Stacks, StatusEffectEvent.Removed));
                        }
                        else if (statusEffect.Stacks != interpolatedStatusEffect.Stacks)
                        {
                            // Status effect was updated, trigger updated event.
                            events.Add(new StatusEffectEvents(statusEffect.Id, statusEffect.StatusEffectDataId, interpolatedStatusEffect.Stacks, StatusEffectEvent.Updated));
                        }
                    }
                    else if (hasInterpolatedStatusEffects)
                    {
                        // Loop through remaining interpolated status effects, trigger removed events.
                        do
                        {
                            interpolatedStatusEffect = interpolatedEnumerator.Current;
                            events.Add(new StatusEffectEvents(interpolatedStatusEffect.Id, interpolatedStatusEffect.StatusEffectDataId, interpolatedStatusEffect.Stacks, StatusEffectEvent.Removed));
                        }
                        while (interpolatedEnumerator.MoveNext());
                    }
                    else
                        break;
                }
            }
        }
    }
}
#endif