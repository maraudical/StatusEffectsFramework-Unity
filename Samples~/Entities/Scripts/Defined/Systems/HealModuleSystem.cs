using StatusEffectFramework.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[assembly: RegisterGenericComponentType(typeof(Modules<StatusEffectFramework.Entities.Samples.HealModuleStruct>))]

namespace StatusEffectFramework.Entities.Samples
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
        private EntityQuery m_EventQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EventQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusEffectEvents, Modules<HealModuleStruct>, ExamplePlayerComponent, StatusFloats>().WithAll<Simulate>().Build();
            m_EventQuery.AddChangedVersionFilter(ComponentType.ReadWrite<Modules<HealModuleStruct>>());
            
            state.RequireForUpdate(m_EventQuery);
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

            var eventJob = new HealEventJob
            {
                References = statusReferences,
                CommandBuffer = commandBuffer,
            };
            state.Dependency = eventJob.ScheduleParallelByRef(m_EventQuery, state.Dependency);
        }

        [BurstCompile]
        partial struct HealEventJob : IJobEntity
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
                UnityEngine.Debug.Log("heal changed");
                // AsNativeArray does not create a copy of the data so any changes will effect the source buffer.
                var healModulesArray = healModules.AsNativeArray();
                healModulesArray.Sort();
                
                if (!player.MaxHealth.TryGetValue(player.ComponentId, statusFloats, out var maxHealth))
                    return;

                foreach (var statusEffectEvent in statusEffectEvents)
                {
                    if (!References.TryGetReference(statusEffectEvent.StatusEffectDataId, out var reference))
                        continue;

                    ref var data = ref reference.Value;

                    switch (statusEffectEvent.Event)
                    {
                        case StatusEffectEvent.Added:
                            if (!StatusEffectsECSUtility.TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out var statusEffect))
                                break;
                            UnityEngine.Debug.Log("added");
                            ref var modules = ref data.Modules;
                            for (int i = 0; i < modules.Length; i++)
                            {
                                var moduleInfo = modules[i];

                                player.Health += data.BaseValue * math.max(0, statusEffect.Stacks);
                            }

                            break;
                        case StatusEffectEvent.Updated:
                            int index = healModulesArray.BinarySearchFirst(statusEffectEvent.Id);
                            
                            if (index < 0 || !StatusEffectsECSUtility.TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out statusEffect))
                                break;
                            UnityEngine.Debug.Log("updated");
                            for (int i = index; i < healModulesArray.Length; i++)
                                if (healModulesArray[i].Id != statusEffectEvent.Id)
                                    player.Health += data.BaseValue * math.max(0, statusEffect.Stacks - statusEffectEvent.PreviousStacks);
                            break;
                    }
                }

                player.Health = math.min(player.Health, maxHealth);
            }
        }
    }
}