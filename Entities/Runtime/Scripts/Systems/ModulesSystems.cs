#if ENTITIES
using Unity.Assertions;
using Unity.Burst;
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

                var typeToIndex = new UnsafeHashMap<TypeIndex, int>(1, Allocator.Temp);
                var bufferLengthInEntity = new UnsafeHashMap<TypeIndex, (int Length, TypeIndex EventType)>(1, Allocator.Temp);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    var entity = entities[i];
                    var statusEffectEvents = statusEffectEventsAccessor[i];
                    bufferLengthInEntity.Clear();

                    foreach (var statusEffectEvent in statusEffectEvents)
                    {
                        if (!References.TryGetReference(statusEffectEvent.StatusEffectDataId, out var reference))
                            continue;
                        
                        ref var data = ref reference.Value;
                        ref var modules = ref data.Modules;

                        for (int v = 0; v < modules.Length; v++)
                        {
                            moduleInfo = modules[v];

                            if (!typeToIndex.TryGetValue(moduleInfo.TypeIndex, out var indexInTypeArray))
                            {
                                indexInTypeArray = StatusEffectsECSInternals.GetIndexInTypeArray(chunk, moduleInfo.TypeIndex);
                                typeToIndex.TryAdd(moduleInfo.TypeIndex, indexInTypeArray);
                            }

                            switch (statusEffectEvent.Event)
                            {
                                // The actual adding to the buffer will be done in another system since there is a
                                // chance we will have to wait for structural changes before making any changes.
                                case StatusEffectEvent.Added:
                                    ref var addInfo = ref bufferLengthInEntity.TryGetValueByRef(moduleInfo.TypeIndex, out bool aFoundLength);

                                    if (aFoundLength)
                                        addInfo.Length++;
                                    else
                                        bufferLengthInEntity.TryAdd(moduleInfo.TypeIndex, (1, moduleInfo.EventsTypeIndex));
                                    break;
                                case StatusEffectEvent.Removed:
                                    Assert.IsTrue(StatusEffectsECSInternals.TryGetBufferWithTypeRW(chunk, i, typeToIndex[moduleInfo.TypeIndex], GlobalSystemVersion, out var header, out var buffer, out var length));

                                    ref var removeLength = ref bufferLengthInEntity.TryGetValueByRef(moduleInfo.TypeIndex, out bool rFoundLength);

                                    if (rFoundLength)
                                        removeLength.Length--;
                                    else
                                        bufferLengthInEntity.TryAdd(moduleInfo.TypeIndex, (length, moduleInfo.EventsTypeIndex));

                                    // Handle add/remove

                                    break;
                                case StatusEffectEvent.Updated:

                                    break;
                            }
                        }
                    }

                    foreach (var kvp in bufferLengthInEntity)
                    {
                        if (kvp.Value.Length > 0)
                        {

                            // Check if the buffer exists in this chunk, if it doesn't, we have to add it.
                            if (StatusEffectsECSInternals.TryGetBufferWithTypeRW(chunk, i, typeToIndex[kvp.Key], GlobalSystemVersion, out var header, out var buffer, out var length))
                                continue;

                            UnityEngine.Debug.Log("this shit don't exist in the chunk yet bro");
                            CommandBuffer.AddComponent(unfilteredChunkIndex, entity, ComponentType.FromTypeIndex(kvp.Key));
                            CommandBuffer.AddComponent(unfilteredChunkIndex, entity, ComponentType.FromTypeIndex(kvp.Value.EventType));
                        }
                        else
                        {

                        }
                    }
                }

                typeToIndex.Dispose();
                bufferLengthInEntity.Dispose();
            }
        }
    }
}
#endif