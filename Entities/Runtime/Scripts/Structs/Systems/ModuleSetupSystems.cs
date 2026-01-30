#if ENTITIES
using Unity.Burst;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
    [BurstCompile]
    // Each entity that can contain status effects will have a
    // cleanup component of Modules which will be used to destroy
    // loose modules if that entity is destroyed.
    public partial struct ModuleTargetSetupSystem : ISystem
    {
        private EntityQuery m_ModuleTargetSetupQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModuleTargetSetupQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects>().WithNone<Modules>().Build();

            state.RequireForUpdate(m_ModuleTargetSetupQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusSetupJob = new ModuleParentSetupJob
            {
                CommandBuffer = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            };
            state.Dependency = statusSetupJob.ScheduleParallelByRef(m_ModuleTargetSetupQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct ModuleParentSetupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
            {
                CommandBuffer.AddBuffer<Modules>(sortKey, entity);
            }
        }
    }

    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
    [BurstCompile]
    public partial struct ModuleSetupSystem : ISystem
    {
        private EntityQuery m_ModuleSetupQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModuleSetupQuery = SystemAPI.QueryBuilder().WithAll<Module>().WithNone<ModuleCleanupComponent>().Build();

            state.RequireForUpdate(m_ModuleSetupQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusSetupJob = new ModuleSetupJob
            {
                CommandBuffer = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            };
            state.Dependency = statusSetupJob.ScheduleParallelByRef(m_ModuleSetupQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct ModuleSetupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in Module module)
            {
                CommandBuffer.AddComponent(sortKey, entity, new ModuleCleanupComponent { Target =  module.Target});
            }
        }
    }
}
#endif