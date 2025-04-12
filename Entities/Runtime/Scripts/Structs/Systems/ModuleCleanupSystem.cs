#if ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
    [BurstCompile]
    public partial struct ModuleCleanupSystem : ISystem
    {
        private EntityQuery m_ModuleValidateQuery;
        private EntityQuery m_ModuleCleanupQuery;
        private EntityQuery m_ModuleDestroyQuery;

        private BufferLookup<Modules> m_ModulesLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModuleValidateQuery = SystemAPI.QueryBuilder().WithAll<Modules>().WithAbsent<StatusEffects>().Build();
            m_ModuleCleanupQuery = SystemAPI.QueryBuilder().WithAll<ModuleCleanupTag>().Build();
            m_ModuleDestroyQuery = SystemAPI.QueryBuilder().WithAll<Module, ModuleDestroyTag>().Build();

            m_ModulesLookup = SystemAPI.GetBufferLookup<Modules>();

            state.RequireForUpdate<Module>();
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public void OnUpdate(ref SystemState state)
        {
            m_ModulesLookup.Update(ref state);

            var commandBufferParallel = SystemAPI.GetSingletonRW<BeginStatusEffectEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Validate status managers on entities to see if parent entity is destroyed.
            if (!m_ModuleValidateQuery.IsEmpty)
            {
                var moduleValidateJob = new ModuleValidateJob
                {
                    CommandBuffer = commandBufferParallel,
                    ModuleLookup = SystemAPI.GetComponentLookup<Module>(true)
                };
                state.Dependency = moduleValidateJob.ScheduleParallelByRef(m_ModuleValidateQuery, state.Dependency);
            }
            // Cleanup occurs the frame after the parent entity is destroyed.
            if (!m_ModuleCleanupQuery.IsEmpty)
            {
                var moduleCleanupJob = new ModuleCleanupJob
                {
                    CommandBuffer = commandBufferParallel
                };
                state.Dependency = moduleCleanupJob.ScheduleParallelByRef(m_ModuleCleanupQuery, state.Dependency);
            }

            if (m_ModuleDestroyQuery.IsEmpty)
                return;

            NativeParallelMultiHashMap<Entity, int> modulesEntitiesIndexes = new NativeParallelMultiHashMap<Entity, int>(m_ModuleDestroyQuery.CalculateEntityCount(), Allocator.TempJob);
            NativeParallelMultiHashMap<Entity, int>.ParallelWriter modulesIndexesParallel = modulesEntitiesIndexes.AsParallelWriter();

            // Destroy occurs the frame after the status effect is removed.
            var moduleDestroyJob = new ModuleDestroyJob
            {
                CommandBuffer = commandBufferParallel,
                ModulesIndexes = modulesIndexesParallel,
                ModulesLookup = m_ModulesLookup
            };
            state.Dependency = moduleDestroyJob.ScheduleParallelByRef(m_ModuleDestroyQuery, state.Dependency);
            
            state.CompleteDependency();

            // Destroy modules.
            (NativeArray<Entity> Entities, int Index) uniqueModulesEntitiesIndexes = modulesEntitiesIndexes.GetUniqueKeyArray(Allocator.Temp);

            for (int i = 0; i < uniqueModulesEntitiesIndexes.Item2; i++)
            {
                Entity key = uniqueModulesEntitiesIndexes.Entities[i];
                
                if (!m_ModulesLookup.TryGetBuffer(key, out var buffer))
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
            
            uniqueModulesEntitiesIndexes.Entities.Dispose();
            modulesEntitiesIndexes.Dispose();
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct ModuleValidateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly] public ComponentLookup<Module> ModuleLookup;

            void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<Modules> modules)
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
        [WithAll(typeof(ModuleCleanupTag))]
        public partial struct ModuleCleanupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            void Execute([ChunkIndexInQuery] int sortKey, Entity entity)
            {
                CommandBuffer.DestroyEntity(sortKey, entity);
            }
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        [WithAll(typeof(ModuleDestroyTag))]
        public partial struct ModuleDestroyJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public NativeParallelMultiHashMap<Entity, int>.ParallelWriter ModulesIndexes;
            [ReadOnly]
            public BufferLookup<Modules> ModulesLookup;

            void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in Module module)
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