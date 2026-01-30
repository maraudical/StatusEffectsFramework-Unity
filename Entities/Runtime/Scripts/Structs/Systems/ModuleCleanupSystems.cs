#if ENTITIES
using Unity.Burst;
using Unity.Entities;

namespace StatusEffects.Entities
{
#if NETCODE
    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [BurstCompile]
    // Cleanup modules when the target was destroyed.
    public partial struct ModulesPredictedDestroySystem : ISystem
    {
        private EntityQuery m_ModulesPredictedDestroyQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModulesPredictedDestroyQuery = SystemAPI.QueryBuilder().WithPresent<PredictedDestroy, Modules>().Build();
            m_ModulesPredictedDestroyQuery.AddChangedVersionFilter(ComponentType.ReadOnly<PredictedDestroy>());

            state.RequireForUpdate(m_ModulesPredictedDestroyQuery);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public void OnUpdate(ref SystemState state)
        {
            var modulesPredictedDestroyJob = new ModulesPredictedDestroyJob
            {
                CommandBuffer = SystemAPI.GetSingletonRW<EndStatusEffectEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };
            state.Dependency = modulesPredictedDestroyJob.ScheduleParallelByRef(m_ModulesPredictedDestroyQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct ModulesPredictedDestroyJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            void Execute([ChunkIndexInQuery] int sortKey, EnabledRefRO<PredictedDestroy> predictedDestroy, in DynamicBuffer<Modules> modules)
            {
                var isPredictedDestroy = predictedDestroy.ValueRO;

                foreach (var module in modules.Reinterpret<Entity>())
                    CommandBuffer.SetComponentEnabled<PredictedDestroy>(sortKey, module, isPredictedDestroy);
            }
        }
    }

#endif
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [BurstCompile]
    // Cleanup modules when the target was destroyed.
    public partial struct ModuleTargetCleanupSystem : ISystem
    {
        private EntityQuery m_ModuleTargetCleanupQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModuleTargetCleanupQuery = SystemAPI.QueryBuilder().WithAll<Modules>().WithNone<StatusEffects>().Build();

            state.RequireForUpdate(m_ModuleTargetCleanupQuery);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public void OnUpdate(ref SystemState state)
        {
            var moduleTargetCleanupJob = new ModuleTargetCleanupJob
            {   
                CommandBuffer = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };
            state.Dependency = moduleTargetCleanupJob.ScheduleParallelByRef(m_ModuleTargetCleanupQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct ModuleTargetCleanupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<Modules> modules)
            {
                foreach (var module in modules.Reinterpret<Entity>())
                    CommandBuffer.DestroyEntity(sortKey, module);
                
                CommandBuffer.RemoveComponent<Modules>(sortKey, entity);
            }
        }
    }
    
    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
    [BurstCompile]
    public partial struct ModuleCleanupSystem : ISystem
    {
        private EntityQuery m_ModuleCleanupQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModuleCleanupQuery = SystemAPI.QueryBuilder().WithAll<ModuleCleanupComponent>().WithNone<Module>().Build();

            state.RequireForUpdate(m_ModuleCleanupQuery);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public void OnUpdate(ref SystemState state)
        {
            if (!m_ModuleCleanupQuery.IsEmpty)
            {
                var moduleCleanupJob = new ModuleCleanupJob
                {
                    CommandBuffer = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
                };
                state.Dependency = moduleCleanupJob.ScheduleParallelByRef(m_ModuleCleanupQuery, state.Dependency);
            }
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct ModuleCleanupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
            {
                CommandBuffer.DestroyEntity(sortKey, entity);
            }
        }
    }
}
#endif