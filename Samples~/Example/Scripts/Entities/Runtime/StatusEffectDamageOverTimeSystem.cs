#if ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using static StatusEffects.Modules.DamageOverTimeModule;

namespace StatusEffects.Entities.Example
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct StatusEffectDamageOverTimeSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<DamageOverTimeEntityModule, Module>().Build();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBufferParallel = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var damageOverTimeJob = new DamageOverTimeJob
            {
                CommandBuffer = commandBufferParallel,
                PlayerLookup = SystemAPI.GetComponentLookup<ExamplePlayer>(),
                TimeDelta = SystemAPI.Time.DeltaTime
            };
            state.Dependency = damageOverTimeJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct DamageOverTimeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly]
            public ComponentLookup<ExamplePlayer> PlayerLookup;
            public float TimeDelta;

            public void Execute([ChunkIndexInQuery] int sortKey, ref DamageOverTimeEntityModule damageOverTime, in Module module)
            {
                Entity entity = module.Parent;
                
                if (PlayerLookup.TryGetComponent(entity, out ExamplePlayer player))
                {
                    damageOverTime.CurrentSeconds -= TimeDelta;
                    while (damageOverTime.CurrentSeconds <= 0)
                    {
                        damageOverTime.CurrentSeconds += damageOverTime.InvervalSeconds;
                        player.Health -= module.BaseValue * module.Stacks;
                        CommandBuffer.SetComponent(sortKey, entity, player);
                    }
                }
            }
        }
    }
}
#endif