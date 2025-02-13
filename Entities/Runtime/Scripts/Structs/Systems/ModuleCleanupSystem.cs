#if ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(ModuleUpdateSystem))]
    [BurstCompile]
    public partial struct ModuleCleanupSystem : ISystem
    {
        private BufferLookup<Modules> m_ModulesLookup;
        private ComponentLookup<Module> m_ModuleLookup;

        private EntityQuery m_ModulesCleanupQuery;
        private EntityQuery m_ModuleDestroyTagQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModulesLookup = state.GetBufferLookup<Modules>();
            m_ModuleLookup = state.GetComponentLookup<Module>();

            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<Modules>().WithAbsent<StatusEffects>();
            m_ModulesCleanupQuery = state.GetEntityQuery(builder);

            builder = new EntityQueryBuilder(Allocator.Temp).WithAll<Module, ModuleDestroyTag>();
            m_ModuleDestroyTagQuery = state.GetEntityQuery(builder);

            state.RequireForUpdate<Module>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_ModulesLookup.Update(ref state);
            m_ModuleLookup.Update(ref state);

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var commandBufferParallel = commandBuffer.AsParallelWriter();

            NativeParallelMultiHashMap<Entity, int> modulesEntitiesIndexes = new NativeParallelMultiHashMap<Entity, int>(m_ModuleDestroyTagQuery.CalculateEntityCount(), Allocator.TempJob);
            NativeParallelMultiHashMap<Entity, int>.ParallelWriter modulesIndexesParallel = modulesEntitiesIndexes.AsParallelWriter();
            
            // Validate status managers on entities to see if parent entity is destroyed.
            new ModuleValidateJob
            {
                CommandBuffer = commandBufferParallel,
                ModuleLookup = m_ModuleLookup
            }.ScheduleParallel(m_ModulesCleanupQuery);
            // Cleanup occurs the frame after the parent entity is destroyed.
            new ModuleCleanupJob
            {
                CommandBuffer = commandBufferParallel
            }.ScheduleParallel();
            // Destroy occurs the frame after the status effect is removed.
            new ModuleDestroyJob
            {
                CommandBuffer = commandBufferParallel,
                ModulesIndexes = modulesIndexesParallel,
                ModulesLookup = m_ModulesLookup
            }.ScheduleParallel();
            
            state.Dependency.Complete();

            // Cleanup modules.
            var uniqueModulesEntitiesIndexes = modulesEntitiesIndexes.GetUniqueKeyArray(Allocator.Temp);

            for (int i = 0; i < uniqueModulesEntitiesIndexes.Item2; i++)
            {
                Entity key = uniqueModulesEntitiesIndexes.Item1[i];
                
                if (!m_ModulesLookup.HasBuffer(key))
                    continue;

                var buffer = m_ModulesLookup[key];
                var enumerator = modulesEntitiesIndexes.GetValuesForKey(key);
                var sortedIndexes = new NativeList<int>(buffer.Length, Allocator.Temp);

                foreach (var value in enumerator)
                    sortedIndexes.Add(value);

                sortedIndexes.Sort();

                for (var v = sortedIndexes.Length - 1; v >= 0; v--)
                    buffer.RemoveAt(sortedIndexes[v]);

                sortedIndexes.Dispose();
            }
            
            uniqueModulesEntitiesIndexes.Item1.Dispose();
            modulesEntitiesIndexes.Dispose();

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct ModuleValidateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly]
            public ComponentLookup<Module> ModuleLookup;

            void Execute([EntityIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<Modules> modules)
            {
                foreach (var moduleEntity in modules.Reinterpret<Entity>())
                {
                    var module = ModuleLookup[moduleEntity];
                    module.PreviousStacks = module.Stacks;
                    module.IsBeingUpdated = true;
                    module.IsBeingDestroyed = true;
                    CommandBuffer.SetComponent(sortKey, moduleEntity, module);
                    CommandBuffer.SetComponentEnabled<ModuleUpdateTag>(sortKey, moduleEntity, true);
                    CommandBuffer.SetComponentEnabled<ModuleCleanupTag>(sortKey, moduleEntity, true);
                }
                CommandBuffer.RemoveComponent<Modules>(sortKey, entity);
            }
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct ModuleCleanupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            void Execute([EntityIndexInQuery] int sortKey, Entity entity, in ModuleCleanupTag tag)
            {
                CommandBuffer.DestroyEntity(sortKey, entity);
            }
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct ModuleDestroyJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public NativeParallelMultiHashMap<Entity, int>.ParallelWriter ModulesIndexes;
            [ReadOnly]
            public BufferLookup<Modules> ModulesLookup;

            void Execute([EntityIndexInQuery] int sortKey, Entity entity, in Module module, in ModuleDestroyTag tag)
            {
                var buffer = ModulesLookup[module.Parent].Reinterpret<Entity>();
                for (var i = 0; i < buffer.Length; i++)
                    if (buffer[i] == entity)
                    {
                        ModulesIndexes.Add(module.Parent, i);
                        break;
                    }
                CommandBuffer.DestroyEntity(sortKey, entity);
            }
        }
    }
}
#endif