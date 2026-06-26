#if ENTITIES
using System.Drawing;
using System.Reflection;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;

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
    public unsafe partial struct ModulesSystem : ISystem
    {
        EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents>().Build();

            state.RequireForUpdate<StatusReferences>();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
#if NETCODE
            var endStatusEffectEntityCommandBuffer = SystemAPI.GetSingleton<EndPredictedStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
#else
            var endStatusEffectEntityCommandBuffer = SystemAPI.GetSingleton<EndStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
#endif
            
            var job = new ModulesJob()
            {
                References = SystemAPI.GetSingleton<StatusReferences>(),
                CommandBuffer = endStatusEffectEntityCommandBuffer,
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(true),
                GlobalSystemVersion = state.GlobalSystemVersion,
            };
            state.Dependency = job.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile]
        public struct ModulesJob : IJobChunk
        {
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public EntityTypeHandle EntityTypeHandle;
            public BufferTypeHandle<StatusEffectEvents> StatusEffectEventsHandle;
            public uint GlobalSystemVersion;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                BufferAccessor<StatusEffectEvents> statusEffectEventsAccessor = chunk.GetBufferAccessorRO(ref StatusEffectEventsHandle);
                ModuleInfo moduleInfo;

                var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(1, Allocator.Temp);
                var typeToLength = new UnsafeHashMap<TypeIndex, int>(1, Allocator.Temp);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    var entity = entities[i];
                    var statusEffectEvents = statusEffectEventsAccessor[i];
                    typeToLength.Clear();

                    foreach (var statusEffectEvent in statusEffectEvents)
                    {
                        if (!References.TryGetReference(statusEffectEvent.StatusEffectDataId, out var reference))
                            continue;
                        
                        ref var data = ref reference.Value;
                        ref var modules = ref data.Modules;

                        for (int v = 0; v < modules.Length; v++)
                        {
                            moduleInfo = modules[v];

                            if (!typeToIndexAndTypeInfo.TryGetValue(moduleInfo.TypeIndex, out var info))
                            {
                                info = (StatusEffectsECSInternals.GetIndexInTypeArray(chunk, moduleInfo.TypeIndex), TypeManager.GetTypeInfo(moduleInfo.TypeIndex));
                                
                                typeToIndexAndTypeInfo.TryAdd(moduleInfo.TypeIndex, info);
                            }

                            int sizeOfModule = info.TypeInfo.ElementSize;

                            switch (statusEffectEvent.Event)
                            {
                                // The actual adding to the buffer will be done in another system since there is a
                                // chance we will have to wait for structural changes before making any changes.
                                case StatusEffectEvent.Added:
                                    ref var addLength = ref typeToLength.TryGetValueByRef(moduleInfo.TypeIndex, out bool aFoundLength);
                                    var componentType = ComponentType.FromTypeIndex(moduleInfo.TypeIndex);

                                    if (aFoundLength)
                                        addLength++;
                                    else
                                    {
                                        typeToLength.TryAdd(moduleInfo.TypeIndex, 1);
                                        if (info.IndexInTypeArray < 0)
                                            CommandBuffer.AddComponent(unfilteredChunkIndex, entity, componentType);
                                    }
                                    
                                    var value = (byte*)UnsafeUtility.Malloc(sizeOfModule, info.TypeInfo.AlignmentInBytes, Allocator.Temp);
                                    var sizeOfInt = UnsafeUtility.SizeOf<uint>();
                                    UnsafeUtility.MemCpy(value, &statusEffectEvent.Id, sizeOfInt);
                                    UnsafeUtility.MemCpy(value + sizeOfInt, moduleInfo.Ptr.ToPointer(), moduleInfo.Size);
                                    StatusEffectsECSInternals.AppendToBuffer(ref CommandBuffer, unfilteredChunkIndex, entity, componentType, sizeOfModule, value);
                                    UnsafeUtility.Free(value, Allocator.Temp);
                                    break;
                                case StatusEffectEvent.Removed:
                                    var header = StatusEffectsECSInternals.GetComponentDataWithTypeRW(chunk, i, info.IndexInTypeArray, GlobalSystemVersion);

                                    if (Hint.Unlikely(!StatusEffectsECSInternals.TryGetElementPointerAndLength(header, out var buffer, out var length)))
                                        UnityEngine.Debug.LogError($"There was an issue with the provided module type <b>{info.TypeInfo.DebugTypeName}</b>.");

                                    ref var removeLength = ref typeToLength.TryGetValueByRef(moduleInfo.TypeIndex, out bool rFoundLength);

                                    if (rFoundLength)
                                        removeLength--;
                                    else
                                        typeToLength.TryAdd(moduleInfo.TypeIndex, length - 1);

                                    for (int n = length - 1; n >= 0; n--)
                                    {
                                        int id = *(int*)(buffer + sizeOfModule * n);
                                        if (id != statusEffectEvent.Id)
                                            continue;

                                        StatusEffectsECSInternals.RemoveAtSwapBack(header, sizeOfModule, n);
                                        break;
                                    }
                                    break;
                                case StatusEffectEvent.Updated:
                                    StatusEffectsECSInternals.SetChangeVersion(chunk, info.IndexInTypeArray, GlobalSystemVersion);
                                    break;
                            }
                        }
                    }

                    foreach (var kvp in typeToLength)
                    {
                        if (kvp.Value <= 0)
                            CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, ComponentType.FromTypeIndex(kvp.Key));
                    }
                }

                typeToIndexAndTypeInfo.Dispose();
                typeToLength.Dispose();
            }
        }
    }
}
#endif