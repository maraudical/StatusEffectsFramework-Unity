#if ENTITIES
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;

namespace StatusEffectFramework.Entities
{
    [UpdateAfter(typeof(StatusManagerSystem))]
#if NETCODE
    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup))]
    [UpdateBefore(typeof(EndPredictedStatusEffectEntityCommandBufferSystem))]
#else
    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
    [UpdateBefore(typeof(EndStatusEffectEntityCommandBufferSystem))]
#endif
    [BurstCompile]
    public partial struct ModulesSystem : ISystem
    {
        EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents>().Build();

            state.RequireForUpdate<ModuleDynamicTypeHandles>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buffer = SystemAPI.GetSingletonBuffer<ModuleDynamicTypeHandles>();

            if(buffer.IsEmpty)
                return;
            
            var typeHandles = new UnsafeHashMap<TypeIndex, DynamicComponentTypeHandle>(buffer.Length, Allocator.TempJob);
#if NETCODE
            var endStatusEffectEntityCommandBuffer = SystemAPI.GetSingleton<EndPredictedStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
#else
            var endStatusEffectEntityCommandBuffer = SystemAPI.GetSingleton<EndStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
#endif

            for (int i = 0; i < buffer.Length; i++)
            {
                var handle = buffer[i];
                if (!typeHandles.ContainsKey(handle.TypeIndex))
                    typeHandles.Add(handle.TypeIndex, state.GetDynamicComponentTypeHandle(ComponentType.FromTypeIndex(handle.TypeIndex)));
            }

            var job = new ModulesJob()
            {
                References = SystemAPI.GetSingleton<StatusReferences>(),
                CommandBuffer = endStatusEffectEntityCommandBuffer,
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(true),
                TypeHandles = typeHandles
            };
            state.Dependency = job.ScheduleParallelByRef(m_EntityQuery, state.Dependency);

            state.Dependency = typeHandles.Dispose(state.Dependency);
        }

        [BurstCompile]
        public struct ModulesJob : IJobChunk
        {
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public BufferTypeHandle<StatusEffectEvents> StatusEffectEventsHandle;
            public UnsafeHashMap<TypeIndex, DynamicComponentTypeHandle> TypeHandles;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BufferAccessor<StatusEffectEvents> statusEffectEventsAccessor = chunk.GetBufferAccessorRO(ref StatusEffectEventsHandle);
                ModuleInfo moduleInfo;
                UnsafeUntypedBufferAccessor bufferAccessor;
                
                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    var statusEffectEvents = statusEffectEventsAccessor[i];

                    foreach (var statusEffectEvent in statusEffectEvents)
                    {
                        if (!References.TryGetReference(statusEffectEvent.StatusEffectDataId, out var reference))
                            continue;
                        
                        ref var data = ref reference.Value;
                        ref var modules = ref data.Modules;

                        switch (statusEffectEvent.Event)
                        {
                            case StatusEffectEvent.Added:
                                for (int v = 0; v < modules.Length; v++)
                                {
                                    //moduleInfo = modules[v];
                                    //ref var typeHandle = ref TypeHandles.TryGetValueByRef(moduleInfo.TypeIndex, out var typeFound);
                                    
                                    //if (!typeFound)
                                    //    continue;

                                    //var readOnly = typeHandle.CopyToReadOnly();
                                    //UnityEngine.Debug.Log($"Type handle is readonly: {readOnly}");
                                    //bufferAccessor = chunk.GetUntypedBufferAccessor(ref readOnly);
                                    //UnityEngine.Debug.Log($"Adding module for type: {moduleInfo.TypeIndex} checking the size of chunk: {chunk.Count} accessor size: {bufferAccessor.Length}");
                                    //if (!chunk.Has(ref typeHandle))
                                    //    bufferAccessor = chunk.GetUntypedBufferAccessor(ref typeHandle);
                                    //bufferAccessor.GetUnsafePtrAndLength(i, out var ptr, out var length);
                                    //if (!foundBuffer)
                                    //{
                                    //    foundBuffer = true;
                                    //    buffer = CommandBuffer.AddBuffer<Modules<HealModuleStruct>>(sortKey, entity);
                                    //}
                                    //StatusEffectsECSUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffectEvent.Id);
                                }

                                break;
                            //case StatusEffectEvent.Removed:
                            //    if (foundBuffer)
                            //        StatusEffectsECSUtility.RemoveModulesFromBuffer(ref buffer, statusEffectEvent.Id);
                            //    break;
                            //case StatusEffectEvent.Updated:
                                
                            //    break;
                        }
                    }
                }
            }
        }
    }
}
#endif