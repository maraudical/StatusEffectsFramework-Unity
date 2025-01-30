#if ENTITIES && ADDRESSABLES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
    [UpdateBefore(typeof(StatusManagerSystem))]
    [BurstCompile]
    public partial struct ModuleUpdateSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<ModuleUpdateTag, Module>();
            m_EntityQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (m_EntityQuery.IsEmpty)
                return;

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var commandBufferParallel = commandBuffer.AsParallelWriter();

            new ModuleUpdateJob
            {
                CommandBuffer = commandBufferParallel,
            }.ScheduleParallel(m_EntityQuery);

            state.Dependency.Complete();

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct ModuleUpdateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            void Execute([EntityIndexInQuery] int sortKey, Entity entity, ref Module module, in ModuleUpdateTag tag)
            {
                module.IsBeingUpdated = false;
                CommandBuffer.SetComponentEnabled<ModuleUpdateTag>(sortKey, entity, false);
            }
        }
    }
}
#endif