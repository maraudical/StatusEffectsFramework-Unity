using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[assembly: RegisterGenericComponentType(typeof(StatusEffectsFramework.Entities.Modules<StatusEffectsFramework.Entities.Samples.HealModuleStruct>))]

namespace StatusEffectsFramework.Entities.Samples
{
    public struct HealModuleStruct { }
    
#if NETCODE
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
#else
    [UpdateInGroup(typeof(SimulationSystemGroup))]
#endif
    [BurstCompile]
    public partial struct HealModuleSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusEffectEvents, Modules<HealModuleStruct>, ExamplePlayerComponent, StatusFloats>().WithAll<Simulate>().Build();
            m_EntityQuery.AddChangedVersionFilter(ComponentType.ReadWrite<Modules<HealModuleStruct>>());

            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<StatusReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusReferences = SystemAPI.GetSingleton<StatusReferences>();
            var commandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var lookup = SystemAPI.GetBufferLookup<Modules<HealModuleStruct>>();
            var playerLookup = SystemAPI.GetComponentLookup<ExamplePlayerComponent>();
            var statusFloatsLookup = SystemAPI.GetBufferLookup<StatusFloats>();

            var job = new HealModuleJob
            {
                References = statusReferences,
                CommandBuffer = commandBuffer,
            };
            state.Dependency = job.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile]
        partial struct HealModuleJob : IJobEntity
        {
            public TypeIndex TypeIndex;
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            public void Execute([ChunkIndexInQuery] int sortKey,
                Entity entity,
                in DynamicBuffer<StatusEffects> statusEffects,
                in DynamicBuffer<StatusEffectEvents> statusEffectEvents,
                ref DynamicBuffer<Modules<HealModuleStruct>> healModules,
                ref ExamplePlayerComponent player,
                in DynamicBuffer<StatusFloats> statusFloats)
            {
                // AsNativeArray does not create a copy of the data so any changes will effect the source buffer.
                var healModulesArray = healModules.AsNativeArray();
                healModulesArray.Sort();

                if (!player.MaxHealth.TryGetValue(player.ComponentId, statusFloats, out var maxHealth))
                    return;

                StatusEffects statusEffect;
                int index;

                foreach (var statusEffectEvent in statusEffectEvents)
                {
                    if (!References.TryGetReference(statusEffectEvent.StatusEffectDataId, out var reference))
                        continue;

                    ref var data = ref reference.Value;

                    switch (statusEffectEvent.Event)
                    {
                        case StatusEffectEvent.Added:
                            AddHealth(0, statusEffectEvent.Id, ref player, in data, in statusEffects);
                            break;
                        case StatusEffectEvent.Updated:
                            AddHealth(statusEffectEvent.PreviousStacks, statusEffectEvent.Id, ref player, in data, in statusEffects);
                            break;
                    }

                    void AddHealth(float subtract, uint id, ref ExamplePlayerComponent player, in UnmanagedStatusEffectData data, in DynamicBuffer<StatusEffects> statusEffects)
                    {
                        index = healModulesArray.BinarySearchFirst(id);

                        if (index < 0 || !StatusEffects.TryGetStatusEffect(statusEffects, id, out statusEffect))
                            return;

                        for (int i = index; i < healModulesArray.Length && healModulesArray[i].Id == id; i++)
                            player.Health += data.BaseValue * math.max(0, statusEffect.Stacks - subtract);
                    }
                }

                player.Health = math.min(player.Health, maxHealth);
            }
        }
    }
}