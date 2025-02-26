#if ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct ModuleUpdateSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAllRW<Module>().WithAll<ModuleUpdateTag>().Build();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var commandBufferParallel = commandBuffer.AsParallelWriter();

            new ModuleUpdateJob
            {
                CommandBuffer = commandBufferParallel,
            }.ScheduleParallel(m_EntityQuery);

            state.CompleteDependency();

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