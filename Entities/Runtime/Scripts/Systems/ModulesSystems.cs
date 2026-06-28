#if ENTITIES
using System.Linq;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace StatusEffectFramework.Entities
{
    [BurstCompile]
    public unsafe struct ModulesJob : IJobChunk
    {
        public FixedString128Bytes SystemName;
        public bool IsServer;
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
            
            var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(StatusReferences.k_ModuleCollectionsInitialCapacity, Allocator.Temp);
            var typeToLength = new UnsafeHashMap<TypeIndex, int>(StatusReferences.k_ModuleCollectionsInitialCapacity, Allocator.Temp);

            var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (enumerator.NextEntityIndex(out var i))
            {
                var entity = entities[i];
                var statusEffectEvents = statusEffectEventsAccessor[i];
                typeToLength.Clear();

                foreach (var statusEffectEvent in statusEffectEvents)
                {
                    if (!Hint.Unlikely(References.TryGetReference(statusEffectEvent.StatusEffectDataId, out var reference)))
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
                                if (IsServer)
                                    UnityEngine.Debug.Log("SERVER system: " + SystemName + " is adding " + componentType.GetDebugTypeName());
                                var value = (byte*)UnsafeUtility.Malloc(sizeOfModule, info.TypeInfo.AlignmentInBytes, Allocator.Temp);
                                var sizeOfInt = UnsafeUtility.SizeOf<uint>();
                                UnsafeUtility.MemCpy(value, &statusEffectEvent.Id, sizeOfInt);
                                UnsafeUtility.MemCpy(value + sizeOfInt, moduleInfo.Ptr.ToPointer(), moduleInfo.Size);
                                StatusEffectsECSInternals.AppendToBuffer(ref CommandBuffer, unfilteredChunkIndex, entity, componentType, sizeOfModule, value);
                                UnsafeUtility.Free(value, Allocator.Temp);
                                break;
                            case StatusEffectEvent.Removed:
                                if (info.IndexInTypeArray < 0)
                                    break;

                                var header = StatusEffectsECSInternals.GetComponentDataWithTypeRW(chunk, i, info.IndexInTypeArray, GlobalSystemVersion);

                                if (Hint.Unlikely(!StatusEffectsECSInternals.TryGetElementPointerAndLength(header, out var buffer, out var length)))
                                    UnityEngine.Debug.LogError($"There was an issue with the provided module type <b>{info.TypeInfo.DebugTypeName}</b>.");

                                ref var removeLength = ref typeToLength.TryGetValueByRef(moduleInfo.TypeIndex, out bool rFoundLength);

                                for (int n = length - 1; n >= 0; n--)
                                {
                                    int id = *(int*)(buffer + sizeOfModule * n);
                                    if (id != statusEffectEvent.Id)
                                        continue;

                                    StatusEffectsECSInternals.RemoveAtSwapBack(header, sizeOfModule, n);

                                    if (rFoundLength)
                                        removeLength--;
                                    else
                                        typeToLength.TryAdd(moduleInfo.TypeIndex, length - 1);

                                    break;
                                }
                                break;
                            case StatusEffectEvent.Updated:
                                if (info.IndexInTypeArray < 0)
                                    break;

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

    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndStatusEffectEntityCommandBufferSystem))]
    [BurstCompile]
    public unsafe partial struct ModulesSystem : ISystem
    {
        EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents>().Build();

            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<StatusReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new ModulesJob()
            {
                SystemName = new FixedString128Bytes("Regular"),
                IsServer = state.WorldUnmanaged.IsServer(),
                References = SystemAPI.GetSingleton<StatusReferences>(),
                CommandBuffer = SystemAPI.GetSingleton<EndStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(true),
                GlobalSystemVersion = state.GlobalSystemVersion,
            };
            state.Dependency = job.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }
    }
#if NETCODE

    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndPredictedStatusEffectEntityCommandBufferSystem))]
    [BurstCompile]
    public unsafe partial struct PredictedModulesSystem : ISystem
    {
        EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents>().Build();

            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<StatusReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new ModulesJob()
            {
                SystemName = new FixedString128Bytes("PREdicted"),
                IsServer = state.WorldUnmanaged.IsServer(),
                References = SystemAPI.GetSingleton<StatusReferences>(),
                CommandBuffer = SystemAPI.GetSingleton<EndPredictedStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(true),
                GlobalSystemVersion = state.GlobalSystemVersion,
            };
            state.Dependency = job.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }
    }

    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateBefore(typeof(PredictedModulesSystem))]
    [BurstCompile]
    public unsafe partial struct FirstPredictionTickModulesSystem : ISystem
    {
        EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, InterpolatedStatusEffects, Simulate>().WithPresent<StatusEffectEvents>().Build();

            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<StatusReferences>();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstPredictionTick)
                return;
            
            var firstPredictionTickJob = new ModulesFirstPredictionTickJob()
            {
                References = SystemAPI.GetSingleton<StatusReferences>(),
                CommandBuffer = SystemAPI.GetSingleton<EndPredictedStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(true),
                StatusEffectsHandle = SystemAPI.GetBufferTypeHandle<StatusEffects>(true),
                InterpolatedStatusEffectsHandle = SystemAPI.GetBufferTypeHandle<InterpolatedStatusEffects>(true),
                GlobalSystemVersion = state.GlobalSystemVersion,
            };
            state.Dependency = firstPredictionTickJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile]
        partial struct ModulesFirstPredictionTickJob : IJobChunk
        {
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public EntityTypeHandle EntityTypeHandle;
            public BufferTypeHandle<StatusEffectEvents> StatusEffectEventsHandle;
            public BufferTypeHandle<StatusEffects> StatusEffectsHandle;
            public BufferTypeHandle<InterpolatedStatusEffects> InterpolatedStatusEffectsHandle;
            public uint GlobalSystemVersion;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                BufferAccessor<StatusEffectEvents> statusEffectEventsAccessor = chunk.GetBufferAccessorRO(ref StatusEffectEventsHandle);
                BufferAccessor<StatusEffects> statusEffectsAccessor = chunk.GetBufferAccessorRO(ref StatusEffectsHandle);
                BufferAccessor<InterpolatedStatusEffects> interpolatedStatusEffectsAccessor = chunk.GetBufferAccessorRO(ref InterpolatedStatusEffectsHandle);

                ModuleInfo moduleInfo;

                var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(StatusReferences.k_ModuleCollectionsInitialCapacity, Allocator.Temp);
                var interpolatedTypes = new UnsafeHashSet<TypeIndex>(StatusReferences.k_ModuleCollectionsInitialCapacity, Allocator.Temp);
                var typeAlreadyProcessed = new UnsafeHashSet<TypeIndex>(StatusReferences.k_ModuleCollectionsInitialCapacity, Allocator.Temp);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    var statusEffects = statusEffectsAccessor[i];
                    var interpolatedStatusEffects = interpolatedStatusEffectsAccessor[i];

                    interpolatedTypes.Clear();
                    typeAlreadyProcessed.Clear();

                    bool noChange = true;
                    int statusEffectsLength = math.min(statusEffects.Length, interpolatedStatusEffects.Length);
                    
                    for (int v = 0; v < statusEffectsLength; v++)
                    {
                        var interpolatedStatusEffect = interpolatedStatusEffects[v];

                        if (statusEffects[v].Id != interpolatedStatusEffect.Id)
                            noChange = false;
                        else
                            continue;

                        if (!Hint.Unlikely(References.TryGetReference(interpolatedStatusEffect.StatusEffectDataId, out var reference)))
                            continue;

                        ref var data = ref reference.Value;
                        ref var modules = ref data.Modules;

                        for (int m = 0; m < modules.Length; m++)
                            interpolatedTypes.Add(modules[m].TypeIndex);
                    }
                    
                    if (noChange && statusEffects.Length == interpolatedStatusEffects.Length)
                        continue;
                    UnityEngine.Debug.Log("prediction rollback for modules in chunk " + unfilteredChunkIndex);
                    var entity = entities[i];
                    var statusEffectEvents = statusEffectEventsAccessor[i].AsNativeArray();

                    foreach (var statusEffect in statusEffects)
                    {
                        int index = statusEffectEvents.IndexOf(statusEffect.Id);
                        if (index >= 0 && statusEffectEvents[index].Event is StatusEffectEvent.Added)
                            continue;

                        if (!Hint.Unlikely(References.TryGetReference(statusEffect.StatusEffectDataId, out var reference)))
                            continue;

                        ref var data = ref reference.Value;
                        ref var modules = ref data.Modules;

                        for (int v = 0; v < modules.Length; v++)
                        {
                            moduleInfo = modules[v];
                            var componentType = ComponentType.FromTypeIndex(moduleInfo.TypeIndex);

                            interpolatedTypes.Remove(moduleInfo.TypeIndex);

                            if (!typeToIndexAndTypeInfo.TryGetValue(moduleInfo.TypeIndex, out var info))
                            {
                                info = (StatusEffectsECSInternals.GetIndexInTypeArray(chunk, moduleInfo.TypeIndex), TypeManager.GetTypeInfo(moduleInfo.TypeIndex));

                                typeToIndexAndTypeInfo.TryAdd(moduleInfo.TypeIndex, info);
                            }

                            int sizeOfModule = info.TypeInfo.ElementSize;
                            var sizeOfInt = UnsafeUtility.SizeOf<uint>();

                            if (info.IndexInTypeArray < 0)
                            {
                                if (!typeAlreadyProcessed.Contains(moduleInfo.TypeIndex))
                                {
                                    CommandBuffer.AddComponent(unfilteredChunkIndex, entity, componentType);
                                    typeAlreadyProcessed.Add(moduleInfo.TypeIndex);
                                }
                                var value = (byte*)UnsafeUtility.Malloc(sizeOfModule, info.TypeInfo.AlignmentInBytes, Allocator.Temp);
                                UnsafeUtility.MemCpy(value, &statusEffect.Id, sizeOfInt);
                                UnsafeUtility.MemCpy(value + sizeOfInt, moduleInfo.Ptr.ToPointer(), moduleInfo.Size);
                                StatusEffectsECSInternals.AppendToBuffer(ref CommandBuffer, unfilteredChunkIndex, entity, componentType, sizeOfModule, value);
                                UnsafeUtility.Free(value, Allocator.Temp);
                                UnityEngine.Debug.Log(componentType.GetDebugTypeName() + " appendign to " + entity);
                            }
                            else
                            {
                                var header = StatusEffectsECSInternals.GetComponentDataWithTypeRW(chunk, i, info.IndexInTypeArray, GlobalSystemVersion);

                                if (Hint.Unlikely(!StatusEffectsECSInternals.TryGetElementPointerAndLength(header, out var buffer, out var length)))
                                    UnityEngine.Debug.LogError($"There was an issue with the provided module type <b>{info.TypeInfo.DebugTypeName}</b>.");

                                ref var lengthAsRef = ref StatusEffectsECSInternals.LengthAsRef(header);

                                if (!typeAlreadyProcessed.Contains(moduleInfo.TypeIndex))
                                {
                                    lengthAsRef = 0;
                                    typeAlreadyProcessed.Add(moduleInfo.TypeIndex);
                                }

                                StatusEffectsECSInternals.EnsureCapacity(header, lengthAsRef + 1, sizeOfModule, info.TypeInfo.AlignmentInBytes);

                                var newElement = buffer + length * sizeOfModule;
                                UnsafeUtility.MemCpy(newElement, &statusEffect.Id, sizeOfInt);
                                UnsafeUtility.MemCpy(newElement + sizeOfInt, moduleInfo.Ptr.ToPointer(), moduleInfo.Size);
                                
                                lengthAsRef++;
                            }
                        }
                    }
                    // These are old module buffers leftover from before rollback.
                    foreach (var typeIndex in interpolatedTypes)
                    {
                        UnityEngine.Debug.Log(ComponentType.FromTypeIndex(typeIndex).GetDebugTypeName() + " was removed");
                        CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, ComponentType.FromTypeIndex(typeIndex));
                    }
                }

                typeToIndexAndTypeInfo.Dispose();
                interpolatedTypes.Dispose();
                typeAlreadyProcessed.Dispose();
            }
        }
    }
#endif
}
#endif