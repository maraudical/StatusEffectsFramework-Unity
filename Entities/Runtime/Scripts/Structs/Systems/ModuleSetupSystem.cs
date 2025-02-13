#if ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(ModuleCleanupSystem))]
    [BurstCompile]
    // Cannot add cleanup components during baking process so it is done here instead.
    public partial struct ModuleSetupSystem : ISystem
    {
        private EntityQuery m_StatusSetupQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<StatusEffects>().WithNone<Modules>();
            m_StatusSetupQuery = state.GetEntityQuery(builder);

            state.RequireForUpdate(m_StatusSetupQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var commandBufferParallel = commandBuffer.AsParallelWriter();

            new StatusSetupJob
            {
                CommandBuffer = commandBufferParallel,
            }.ScheduleParallel(m_StatusSetupQuery);

            state.Dependency.Complete();

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct StatusSetupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            void Execute([EntityIndexInQuery] int sortKey, Entity entity)
            {
                CommandBuffer.AddBuffer<Modules>(sortKey, entity);
            }
        }
    }
}
#endif