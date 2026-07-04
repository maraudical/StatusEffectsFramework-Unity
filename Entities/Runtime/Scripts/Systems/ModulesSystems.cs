#if ENTITIES
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    internal unsafe struct ModulesJob : IJobChunk
    {
        public StatusReferences References;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        public EntityTypeHandle EntityTypeHandle;
        public BufferTypeHandle<StatusEffectEvents> StatusEffectEventsHandle;
        public BufferTypeHandle<ZeroLengthModules> ZeroLengthModulesHandle;
        public uint GlobalSystemVersion;

        [BurstCompile]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
            BufferAccessor<StatusEffectEvents> statusEffectEventsAccessor = chunk.GetBufferAccessorRO(ref StatusEffectEventsHandle);
            BufferAccessor<ZeroLengthModules> zeroLengthModulesAccessor = chunk.GetBufferAccessorRW(ref ZeroLengthModulesHandle);
            ModuleInfo moduleInfo;
            
            var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);
            var typeToLength = new UnsafeHashMap<TypeIndex, int>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);
            var sizeOfUint = UnsafeUtility.SizeOf<uint>();

            var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (enumerator.NextEntityIndex(out var i))
            {
                var entity = entities[i];
                var statusEffectEvents = statusEffectEventsAccessor[i];
                var zeroLengthModules = zeroLengthModulesAccessor[i].Reinterpret<TypeIndex>();
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

                                var value = (byte*)UnsafeUtility.Malloc(sizeOfModule, info.TypeInfo.AlignmentInBytes, Allocator.Temp);
                                UnsafeUtility.MemCpy(value, &statusEffectEvent.Id, sizeOfUint);
                                UnsafeUtility.MemCpy(value + References.ModuleOffsets.Struct, moduleInfo.Ptr.ToPointer(), moduleInfo.Size);
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

                foreach (var typeIndex in zeroLengthModules)
                {
                    if (!typeToIndexAndTypeInfo.TryGetValue(typeIndex, out var info))
                        info = (StatusEffectsECSInternals.GetIndexInTypeArray(chunk, typeIndex), TypeManager.GetTypeInfo(typeIndex));

                    if (info.IndexInTypeArray < 0)
                        continue;

                    if (typeToLength.TryGetValue(typeIndex, out var length))
                        if (length > 0)
                            continue;

                    if (StatusEffectsECSInternals.IsEmpty(chunk, i, info.IndexInTypeArray))
                        CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, ComponentType.FromTypeIndex(typeIndex));
                }

                zeroLengthModules.Clear();

                foreach (var kvp in typeToLength)
                {
                    if (kvp.Value <= 0)
                        zeroLengthModules.Add(kvp.Key);
                }
            }

            typeToIndexAndTypeInfo.Dispose();
            typeToLength.Dispose();
        }
    }

#if NETCODE
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
#endif
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndStatusEffectEntityCommandBufferSystem))]
    [BurstCompile]
    public partial struct ModulesSystem : ISystem
    {
        EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
#if NETCODE
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents, ZeroLengthModules>().WithNone<PredictedGhost>().Build();
#else
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents, ZeroLengthModules>().Build();
#endif

            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<StatusReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new ModulesJob()
            {
                References = SystemAPI.GetSingleton<StatusReferences>(),
                CommandBuffer = SystemAPI.GetSingleton<EndStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(true),
                ZeroLengthModulesHandle = SystemAPI.GetBufferTypeHandle<ZeroLengthModules>(),
                GlobalSystemVersion = state.GlobalSystemVersion,
            };
            state.Dependency = job.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }
    }
#if NETCODE

    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndPredictedStatusEffectEntityCommandBufferSystem))]
    [BurstCompile]
    public partial struct PredictedModulesSystem : ISystem
    {
        EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents, ZeroLengthModules, Simulate>().Build();

            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<StatusReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new ModulesJob()
            {
                References = SystemAPI.GetSingleton<StatusReferences>(),
                CommandBuffer = SystemAPI.GetSingleton<EndPredictedStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(true),
                ZeroLengthModulesHandle = SystemAPI.GetBufferTypeHandle<ZeroLengthModules>(),
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
                NetworkTime = networkTime,
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
        internal struct ModulesFirstPredictionTickJob : IJobChunk
        {
            public NetworkTime NetworkTime;
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

                var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);
                var interpolatedTypes = new UnsafeHashSet<TypeIndex>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);
                var typeAlreadyProcessed = new UnsafeHashSet<TypeIndex>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);
                var sizeOfUint = UnsafeUtility.SizeOf<uint>();

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    var statusEffects = statusEffectsAccessor[i];
                    var interpolatedStatusEffects = interpolatedStatusEffectsAccessor[i];

                    interpolatedTypes.Clear();
                    typeAlreadyProcessed.Clear();

                    bool noChange = true;
                    
                    for (int v = 0; v < interpolatedStatusEffects.Length; v++)
                    {
                        var interpolatedStatusEffect = interpolatedStatusEffects[v];

                        if (v >= statusEffects.Length || statusEffects[v].Id != interpolatedStatusEffect.Id)
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

                            if (info.IndexInTypeArray < 0)
                            {
                                if (!typeAlreadyProcessed.Contains(moduleInfo.TypeIndex))
                                {
                                    CommandBuffer.AddComponent(unfilteredChunkIndex, entity, componentType);
                                    typeAlreadyProcessed.Add(moduleInfo.TypeIndex);
                                }
                                var value = (byte*)UnsafeUtility.Malloc(sizeOfModule, info.TypeInfo.AlignmentInBytes, Allocator.Temp);
                                UnsafeUtility.MemCpy(value, &statusEffect.Id, sizeOfUint);
                                UnsafeUtility.MemCpy(value + References.ModuleOffsets.Struct, moduleInfo.Ptr.ToPointer(), moduleInfo.Size);
                                StatusEffectsECSInternals.AppendToBuffer(ref CommandBuffer, unfilteredChunkIndex, entity, componentType, sizeOfModule, value);
                                UnsafeUtility.Free(value, Allocator.Temp);
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
                                
                                var newElement = buffer + lengthAsRef * sizeOfModule;
                                UnsafeUtility.MemCpy(newElement, &statusEffect.Id, sizeOfUint);
                                UnsafeUtility.MemCpy(newElement + References.ModuleOffsets.Struct, moduleInfo.Ptr.ToPointer(), moduleInfo.Size);
                                lengthAsRef++;
                            }
                        }
                    }
                    // These are old module buffers leftover from before rollback.
                    foreach (var typeIndex in interpolatedTypes)
                        CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, ComponentType.FromTypeIndex(typeIndex));
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