#if ENTITIES
using Unity.Burst;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginStatusEffectEntityCommandBufferSystem))]
    [UpdateBefore(typeof(ModuleCleanupSystem))]
    [BurstCompile]
    // Cannot add cleanup components during baking process so it is done here instead.
    public partial struct ModuleSetupSystem : ISystem
    {
        private EntityQuery m_StatusSetupQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_StatusSetupQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects>().WithNone<Modules>().Build();
            state.RequireForUpdate(m_StatusSetupQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBufferParallel = SystemAPI.GetSingletonRW<EndStatusEffectEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var statusSetupJob = new StatusSetupJob
            {
                CommandBuffer = commandBufferParallel,
            };
            state.Dependency = statusSetupJob.ScheduleParallelByRef(m_StatusSetupQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct StatusSetupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
            {
                CommandBuffer.AddBuffer<Modules>(sortKey, entity);
            }
        }
    }
}
#endif