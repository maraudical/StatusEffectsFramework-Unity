#if ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static StatusEffects.Modules.HealModule;

namespace StatusEffects.Entities.Example
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    // Special case where on the same frame we are dealing damage over time
    // and healing. In this example the heal comes after the damage over time.
    [UpdateAfter(typeof(StatusEffectDamageOverTimeSystem))]
    public partial struct StatusEffectHealSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<HealEntityModule, Module, ModuleUpdateTag>().Build();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBufferParallel = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var healJob = new HealJob
            {
                CommandBuffer = commandBufferParallel,
                PlayerLookup = SystemAPI.GetComponentLookup<ExamplePlayer>(),
                StatusFloatsLookup = SystemAPI.GetBufferLookup<StatusFloats>(),
                LastSystemVersion = state.LastSystemVersion
            };
            state.Dependency = healJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct HealJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly]
            public ComponentLookup<ExamplePlayer> PlayerLookup;
            [ReadOnly]
            public BufferLookup<StatusFloats> StatusFloatsLookup;

            public uint LastSystemVersion;

            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in HealEntityModule healModule, in Module module, in ModuleUpdateTag update)
            {
                Entity parentEntity = module.Parent;
                
                if (PlayerLookup.TryGetComponent(parentEntity, out ExamplePlayer player))
                {
                    var buffer = StatusFloatsLookup[parentEntity];
                    player.MaxHealth.GetValue(player.ComponentId, buffer, out var maxHealth, StatusFloatsLookup.DidChange(parentEntity, LastSystemVersion));

                    if (module.IsBeingDestroyed)
                    {
                        player.Health = math.min(player.Health, maxHealth);
                    }
                    else
                    {
                        player.Health += module.BaseValue * math.max(0, module.Stacks - module.PreviousStacks);
                        player.Health = math.min(player.Health, maxHealth);
                    }
                    CommandBuffer.SetComponent(sortKey, parentEntity, player);
                }
            }
        }
    }
}
#endif