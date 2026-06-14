#if ENTITIES
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using UnityEngine.Rendering;

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
            var typeHandles = new UnsafeHashMap<TypeIndex, DynamicComponentTypeHandle>(buffer.Length, Allocator.TempJob);

            for (int i = 0; i < buffer.Length; i++)
            {
                var handle = buffer[i];
                if (!typeHandles.ContainsKey(handle.TypeIndex))
                    typeHandles.Add(handle.TypeIndex, state.GetDynamicComponentTypeHandle(ComponentType.FromTypeIndex(handle.TypeIndex)));
            }

            var job = new ModulesJob()
            {
                References = SystemAPI.GetSingleton<StatusReferences>(),
                CommandBuffer = 
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(true),
                TypeHandles = typeHandles
            };
            state.Dependency = job.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile]
        public struct ModulesJob : IJobChunk
        {
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public BufferTypeHandle<StatusEffectEvents> StatusEffectEventsHandle;
            [DeallocateOnJobCompletion]
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
                                    moduleInfo = modules[v];
                                    ref var typeHandle = ref TypeHandles.TryGetValueByRef(moduleInfo.TypeIndex, out var typeFound);

                                    if (!typeFound)
                                        continue;

                                    bufferAccessor = chunk.GetUntypedBufferAccessor(ref typeHandle);
                                    UnityEngine.Debug.Log(bufferAccessor.Length);
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