#if ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginStatusEffectEntityCommandBufferSystem))]
    [BurstCompile]
    public partial struct ModuleUpdateSystem : ISystem
    {
        private EntityQuery m_ModuleUpdateQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModuleUpdateQuery = SystemAPI.QueryBuilder().WithAll<Module, ModuleUpdateTag>().Build();
            state.RequireForUpdate(m_ModuleUpdateQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBufferParallel = SystemAPI.GetSingletonRW<BeginStatusEffectEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var moduleUpdateJob = new ModuleUpdateJob
            {
                CommandBuffer = commandBufferParallel,
                ModuleLookup = SystemAPI.GetComponentLookup<Module>(true)
            };
            state.Dependency = moduleUpdateJob.ScheduleParallelByRef(m_ModuleUpdateQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        [WithAll(typeof(Module), typeof(ModuleUpdateTag))]
        public partial struct ModuleUpdateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly] public ComponentLookup<Module> ModuleLookup;

            void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
            {
                var module = ModuleLookup.GetRefRO(entity).ValueRO;
                module.IsBeingUpdated = false;
                CommandBuffer.SetComponent(sortKey, entity, module);
                CommandBuffer.SetComponentEnabled<ModuleUpdateTag>(sortKey, entity, false);
            }
        }
    }
}
#endif