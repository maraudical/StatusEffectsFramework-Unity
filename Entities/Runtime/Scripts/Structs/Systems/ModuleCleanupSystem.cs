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
        private EntityQuery m_ModulesCleanupQuery;
        private EntityQuery m_ModuleDestroyTagQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModulesCleanupQuery = SystemAPI.QueryBuilder().WithAll<Modules>().WithAbsent<StatusEffects>().Build();
            m_ModuleDestroyTagQuery = SystemAPI.QueryBuilder().WithAll<Module, ModuleDestroyTag>().Build();

            state.RequireForUpdate<Module>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var modulesLookup = SystemAPI.GetBufferLookup<Modules>();
            var moduleLookup = SystemAPI.GetComponentLookup<Module>();

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var commandBufferParallel = commandBuffer.AsParallelWriter();

            NativeParallelMultiHashMap<Entity, int> modulesEntitiesIndexes = new NativeParallelMultiHashMap<Entity, int>(m_ModuleDestroyTagQuery.CalculateEntityCount(), Allocator.TempJob);
            NativeParallelMultiHashMap<Entity, int>.ParallelWriter modulesIndexesParallel = modulesEntitiesIndexes.AsParallelWriter();
            
            // Validate status managers on entities to see if parent entity is destroyed.
            new ModuleValidateJob
            {
                CommandBuffer = commandBufferParallel,
                ModuleLookup = moduleLookup
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
                ModulesLookup = modulesLookup
            }.ScheduleParallel(m_ModuleDestroyTagQuery);
            
            state.CompleteDependency();

            // Cleanup modules.
            var uniqueModulesEntitiesIndexes = modulesEntitiesIndexes.GetUniqueKeyArray(Allocator.Temp);

            for (int i = 0; i < uniqueModulesEntitiesIndexes.Item2; i++)
            {
                Entity key = uniqueModulesEntitiesIndexes.Item1[i];
                
                if (!modulesLookup.TryGetBuffer(key, out var buffer))
                    continue;
                
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
                if (!ModulesLookup.TryGetBuffer(module.Parent, out var modulesBuffer))
                    return;

                var buffer = modulesBuffer.Reinterpret<Entity>();
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