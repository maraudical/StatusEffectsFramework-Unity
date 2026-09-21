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
        private ulong m_StableTypeHash;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusEffectEvents, Modules<HealModuleStruct>, ExamplePlayerComponent, StatusFloats>().WithAll<Simulate>().Build();
            m_EntityQuery.AddChangedVersionFilter(ComponentType.ReadWrite<Modules<HealModuleStruct>>());
            m_StableTypeHash = TypeManager.GetTypeInfo<ExamplePlayerComponent>().StableTypeHash;

            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<UnmanagedStatusRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new HealModuleJob
            {
                StableTypeHash = m_StableTypeHash,
                Registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>(),
                CommandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            };
            state.Dependency = job.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile]
        partial struct HealModuleJob : IJobEntity
        {
            public ulong StableTypeHash;
            public UnmanagedStatusRegistry Registry;
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

                if (!player.MaxHealth.TryGetValue(StableTypeHash, Registry, statusFloats, out var maxHealth))
                    return;

                StatusEffects statusEffect;
                int index;

                foreach (var statusEffectEvent in statusEffectEvents)
                {
                    ref var data = ref Registry.GetStatusEffectData(statusEffectEvent.Id);

                    switch (statusEffectEvent.Event)
                    {
                        case StatusEffectEvent.Added:
                            AddHealth(0, statusEffectEvent.InstanceId, ref player, in data, in statusEffects);
                            break;
                        case StatusEffectEvent.Updated:
                            AddHealth(statusEffectEvent.PreviousStacks, statusEffectEvent.InstanceId, ref player, in data, in statusEffects);
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