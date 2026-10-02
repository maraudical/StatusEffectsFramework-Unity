using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    internal unsafe struct ModulesJob : IJobChunk
    {
        public UnmanagedStatusRegistry Registry;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        // Records ZeroLengthModules entries. See ZeroLengthModules for the removal chain.
        public EntityCommandBuffer.ParallelWriter LateCommandBuffer;
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
            
            using var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
            using var typeToLength = new UnsafeHashMap<TypeIndex, int>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);

            var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (enumerator.NextEntityIndex(out var i))
            {
                var entity = entities[i];
                var statusEffectEvents = statusEffectEventsAccessor[i];
                var zeroLengthModules = zeroLengthModulesAccessor[i].Reinterpret<TypeIndex>();
                var zeroLengthModulesPtr = (TypeIndex*)zeroLengthModules.GetUnsafePtr();
                typeToLength.Clear();

                foreach (var statusEffectEvent in statusEffectEvents)
                {
                    ref var data = ref Registry.GetStatusEffectDataOrNullRefDebug(statusEffectEvent.Id, out bool exists);
                    if (!exists)
                        continue;
                    
                    for (int v = 0; v < data.Modules.Length; v++)
                    {
                        ref var moduleInfo = ref data.Modules[v];
                        
                        if (!typeToIndexAndTypeInfo.TryGetValue(moduleInfo.TypeIndex, out var info))
                        {
                            info = (StatusEffectsUtility.GetIndexInTypeArray(chunk, moduleInfo.TypeIndex), TypeManager.GetTypeInfo(moduleInfo.TypeIndex));
                            typeToIndexAndTypeInfo.TryAdd(moduleInfo.TypeIndex, info);
                        }

                        int sizeOfModule = info.TypeInfo.ElementSize;

                        switch (statusEffectEvent.Event)
                        {
                            // The actual adding to the buffer will be done in another system since there is a
                            // chance we will have to wait for structural changes before making any changes.
                            case StatusEffectEvent.Added:
                                ref var addLength = ref typeToLength.GetValueRefOrNullRef(moduleInfo.TypeIndex, out bool aFoundLength);
                                var componentType = ComponentType.FromTypeIndex(moduleInfo.TypeIndex);

                                if (aFoundLength)
                                    addLength++;
                                else
                                {
                                    // Seed from the real length so later removals this update count correctly.
                                    int currentLength = 0;
                                    if (info.IndexInTypeArray < 0)
                                        CommandBuffer.AddComponent(unfilteredChunkIndex, entity, componentType);
                                    else
                                    {
                                        var addHeader = (BufferHeader*)StatusEffectsUtility.GetComponentDataWithTypeRO(chunk, i, info.IndexInTypeArray);
                                        if (Hint.Likely(addHeader != null))
                                            currentLength = addHeader->Length;
                                    }
                                    typeToLength.TryAdd(moduleInfo.TypeIndex, currentLength + 1);
                                }

                                var ptr = (byte*)UnsafeUtility.Malloc(sizeOfModule, info.TypeInfo.AlignmentInBytes, Allocator.Temp);
                                ModulesSystem.WriteModuleElement(ptr, statusEffectEvent.InstanceId, ref moduleInfo);
                                StatusEffectsUtility.AppendToBuffer(ref CommandBuffer, unfilteredChunkIndex, entity, componentType, sizeOfModule, ptr);
                                UnsafeUtility.Free(ptr, Allocator.Temp);
                                break;
                            case StatusEffectEvent.Removed:
                                if (info.IndexInTypeArray < 0)
                                    break;

                                var header = (BufferHeader*)StatusEffectsUtility.GetComponentDataWithTypeRW(chunk, i, info.IndexInTypeArray, GlobalSystemVersion);

                                if (Hint.Unlikely(header == null))
                                    continue;

                                var buffer = BufferHeader.GetElementPointer(header);
                                var length = header->Length;
                                ref var removeLength = ref typeToLength.GetValueRefOrNullRef(moduleInfo.TypeIndex, out bool rFoundLength);

                                for (int n = length - 1; n >= 0; n--)
                                {
                                    uint id = *(uint*)(buffer + sizeOfModule * n + moduleInfo.IdOffset);
                                    if (id != statusEffectEvent.InstanceId)
                                        continue;
                                    
                                    StatusEffectsUtility.RemoveAtSwapBack(header, sizeOfModule, n);

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

                                StatusEffectsUtility.SetChangeVersion(chunk, info.IndexInTypeArray, GlobalSystemVersion);
                                break;
                        }
                    }
                }

                // Empty buffers aren't removed here. They are queued in ZeroLengthModules and removed
                // later by the ZeroLengthModulesJob only if they are still empty then. A type that has
                // elements again cancels any removal already queued for it.
                bool hasZeroLengthModules = false;
                bool zeroLengthModulesSorted = false;

                foreach (var kvp in typeToLength)
                {
                    if (kvp.Value <= 0)
                    {
                        hasZeroLengthModules = true;
                        LateCommandBuffer.AppendToBuffer(unfilteredChunkIndex, entity, new ZeroLengthModules { TypeIndex = kvp.Key });
                    }
                    else
                    {
                        if (zeroLengthModules.Length == 0)
                            continue;

                        // Only sort once there is something to search for.
                        if (!zeroLengthModulesSorted)
                        {
                            NativeSortExtension.Sort(zeroLengthModulesPtr, zeroLengthModules.Length);
                            zeroLengthModulesSorted = true;
                        }

                        int last = NativeSortExtensions.BinarySearchLast(zeroLengthModulesPtr, zeroLengthModules.Length, kvp.Key);
                        if (last >= 0)
                        {
                            int first = last;
                            while (first > 0 && zeroLengthModules[first - 1].Value == kvp.Key)
                                first--;

                            // RemoveRange keeps the buffer sorted for the binary searches of the remaining types.
                            zeroLengthModules.RemoveRange(first, last - first + 1);
                        }
                    }
                }

                if (hasZeroLengthModules)
                    LateCommandBuffer.SetComponentEnabled<ZeroLengthModules>(unfilteredChunkIndex, entity, true);
            }
        }
    }

    /// <summary>
    /// The last step of the deferred removal described on <see cref="ZeroLengthModules"/>. Removes each
    /// queued module type whose buffer is still empty, then clears and disables the queue.
    /// </summary>
    [BurstCompile]
    internal struct ZeroLengthModulesJob : IJobChunk
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        public EntityTypeHandle EntityTypeHandle;
        public BufferTypeHandle<ZeroLengthModules> ZeroLengthModulesHandle;

        [BurstCompile]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
            BufferAccessor<ZeroLengthModules> zeroLengthModulesAccessor = chunk.GetBufferAccessorRW(ref ZeroLengthModulesHandle);

            using var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
            
            var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (enumerator.NextEntityIndex(out var i))
            {
                var entity = entities[i];
                var zeroLengthModules = zeroLengthModulesAccessor[i].Reinterpret<TypeIndex>();

                foreach (var typeIndex in zeroLengthModules)
                {
                    if (!typeToIndexAndTypeInfo.TryGetValue(typeIndex, out var info))
                    {
                        info = (StatusEffectsUtility.GetIndexInTypeArray(chunk, typeIndex), TypeManager.GetTypeInfo(typeIndex));
                        typeToIndexAndTypeInfo.TryAdd(typeIndex, info);
                    }

                    if (info.IndexInTypeArray < 0)
                        continue;

                    if (StatusEffectsUtility.IsEmpty(chunk, i, info.IndexInTypeArray))
                        CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, ComponentType.FromTypeIndex(typeIndex));
                }
                
                zeroLengthModules.Clear();
            }

            chunk.SetComponentEnabledForAll(ref ZeroLengthModulesHandle, false);
        }
    }

    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndStatusEffectEntityCommandBufferSystem))]
    [BurstCompile]
    public partial struct ModulesSystem : ISystem
    {
        EntityQuery m_ModulesQuery;
        EntityQuery m_ZeroLengthModulesQuery;
        private StatusTypeDependencies m_Dependencies;

        /// <summary>
        /// Writes a new module element for <paramref name="moduleInfo"/> to <paramref name="element"/>.
        /// </summary>
        internal static unsafe void WriteModuleElement(byte* element, uint instanceId, ref ModuleInfo moduleInfo)
        {
            *(uint*)(element + moduleInfo.IdOffset) = instanceId;
            UnsafeUtility.MemCpy(element + moduleInfo.StructOffset, moduleInfo.Bytes.GetUnsafePtr(), moduleInfo.Size);
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
#if NETCODE
            m_ModulesQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents>().WithPresent<ZeroLengthModules>().WithNone<PredictedGhost>().Build();
            m_ZeroLengthModulesQuery = SystemAPI.QueryBuilder().WithAll<ZeroLengthModules>().Build();
#else
            m_ModulesQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents>().WithPresent<ZeroLengthModules>().Build();
            m_ZeroLengthModulesQuery = SystemAPI.QueryBuilder().WithAll<ZeroLengthModules>().Build();
#endif

            state.RequireForUpdate<StatusEffects>();
            state.RequireForUpdate<UnmanagedStatusRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>();
            m_Dependencies.Register(ref state, registry.Version, ref registry.ModuleTypes, isReadOnly: false);

            var entityTypeHandle = SystemAPI.GetEntityTypeHandle();
            var zeroLengthModulesHandle = SystemAPI.GetBufferTypeHandle<ZeroLengthModules>();

            var modulesJob = new ModulesJob()
            {
                Registry = registry,
                CommandBuffer = SystemAPI.GetSingleton<EndStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                LateCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                EntityTypeHandle = entityTypeHandle,
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(true),
                ZeroLengthModulesHandle = zeroLengthModulesHandle,
                GlobalSystemVersion = state.GlobalSystemVersion,
            };
#if NETCODE
            // On the server every event is created and handled by the PredictedModulesSystem. The client
            // still needs this for the events the InterpolatedStatusEffectEventsSystem creates.
            if (!state.WorldUnmanaged.IsServer())
#endif
            state.Dependency = modulesJob.ScheduleParallelByRef(m_ModulesQuery, state.Dependency);

            // Runs in every world, including the server, since the PredictedModulesSystem queues removals
            // here too. The removals play back at the start of the next frame.
            var zeroLengthModulesJob = new ZeroLengthModulesJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                EntityTypeHandle = entityTypeHandle,
                ZeroLengthModulesHandle = zeroLengthModulesHandle,
            };
            state.Dependency = zeroLengthModulesJob.ScheduleParallelByRef(m_ZeroLengthModulesQuery, state.Dependency);
        }
    }
#if NETCODE

    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndPredictedStatusEffectEntityCommandBufferSystem))]
    [BurstCompile]
    public partial struct PredictedModulesSystem : ISystem
    {
        EntityQuery m_ModulesQuery;
        private StatusTypeDependencies m_Dependencies;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModulesQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents, Simulate>().WithPresent<ZeroLengthModules>().Build();

            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<UnmanagedStatusRegistry>();
            state.RequireForUpdate(m_ModulesQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>();
            m_Dependencies.Register(ref state, registry.Version, ref registry.ModuleTypes, isReadOnly: false);

            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            // ZeroLengthModules entries recorded on the first full prediction of a tick play back at the
            // start of the next frame. Entries from resimulated or partial ticks play back at the end of
            // that prediction tick, before the ModulesSystem checks them later this frame.
            var lateCommandBuffer = networkTime.IsFirstTimeFullyPredictingTick ? SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter() 
                : SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var modulesJob = new ModulesJob()
            {
                Registry = registry,
                CommandBuffer = SystemAPI.GetSingleton<EndPredictedStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                LateCommandBuffer = lateCommandBuffer,
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(true),
                ZeroLengthModulesHandle = SystemAPI.GetBufferTypeHandle<ZeroLengthModules>(),
                GlobalSystemVersion = state.GlobalSystemVersion,
            };
            state.Dependency = modulesJob.ScheduleParallelByRef(m_ModulesQuery, state.Dependency);
        }
    }

    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateBefore(typeof(PredictedModulesSystem))]
    [BurstCompile]
    public unsafe partial struct FirstPredictionTickModulesSystem : ISystem
    {
        EntityQuery m_EntityQuery;
        private StatusTypeDependencies m_Dependencies;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, InterpolatedStatusEffects, Simulate>().WithPresent<StatusEffectEvents>().Build();

            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<UnmanagedStatusRegistry>();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstPredictionTick)
                return;

            var registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>();
            m_Dependencies.Register(ref state, registry.Version, ref registry.ModuleTypes, isReadOnly: false);
            
            var firstPredictionTickJob = new ModulesFirstPredictionTickJob()
            {
                Registry = registry,
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
            public UnmanagedStatusRegistry Registry;
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


                using var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var interpolatedTypes = new UnsafeHashSet<TypeIndex>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var typeAlreadyProcessed = new UnsafeHashSet<TypeIndex>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);

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

                        if (v >= statusEffects.Length || statusEffects[v].InstanceId != interpolatedStatusEffect.InstanceId)
                            noChange = false;
                        else
                            continue;

                        ref var data = ref Registry.GetStatusEffectDataOrNullRefDebug(interpolatedStatusEffect.Id, out bool exists);
                        if (!exists)
                            continue;

                        for (int m = 0; m < data.Modules.Length; m++)
                            interpolatedTypes.Add(data.Modules[m].TypeIndex);
                    }
                    
                    if (noChange && statusEffects.Length == interpolatedStatusEffects.Length)
                        continue;
                    
                    var entity = entities[i];
                    var statusEffectEvents = statusEffectEventsAccessor[i].AsNativeArray();

                    foreach (var statusEffect in statusEffects)
                    {
                        int index = statusEffectEvents.IndexOf(statusEffect.InstanceId);
                        if (index >= 0 && statusEffectEvents[index].Event is StatusEffectEvent.Added)
                            continue;

                        ref var data = ref Registry.GetStatusEffectDataOrNullRefDebug(statusEffect.Id, out bool exists);
                        if (!exists)
                            continue;

                        for (int v = 0; v < data.Modules.Length; v++)
                        {
                            ref var moduleInfo = ref data.Modules[v];
                            var componentType = ComponentType.FromTypeIndex(moduleInfo.TypeIndex);

                            interpolatedTypes.Remove(moduleInfo.TypeIndex);

                            if (!typeToIndexAndTypeInfo.TryGetValue(moduleInfo.TypeIndex, out var info))
                            {
                                info = (StatusEffectsUtility.GetIndexInTypeArray(chunk, moduleInfo.TypeIndex), TypeManager.GetTypeInfo(moduleInfo.TypeIndex));

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

                                var ptr = (byte*)UnsafeUtility.Malloc(sizeOfModule, info.TypeInfo.AlignmentInBytes, Allocator.Temp);
                                ModulesSystem.WriteModuleElement(ptr, statusEffect.InstanceId, ref moduleInfo);
                                StatusEffectsUtility.AppendToBuffer(ref CommandBuffer, unfilteredChunkIndex, entity, componentType, sizeOfModule, ptr);
                                UnsafeUtility.Free(ptr, Allocator.Temp);
                            }
                            else
                            {
                                var header = (BufferHeader*)StatusEffectsUtility.GetComponentDataWithTypeRW(chunk, i, info.IndexInTypeArray, GlobalSystemVersion);

                                if (Hint.Unlikely(header == null))
                                    continue;

                                ref var length = ref header->Length;

                                if (!typeAlreadyProcessed.Contains(moduleInfo.TypeIndex))
                                {
                                    length = 0;
                                    typeAlreadyProcessed.Add(moduleInfo.TypeIndex);
                                }

                                BufferHeader.EnsureCapacity(header, length + 1, sizeOfModule, info.TypeInfo.AlignmentInBytes, BufferHeader.TrashMode.RetainOldData, false, 0);
                                
                                var buffer = BufferHeader.GetElementPointer(header);

                                var newElement = buffer + length * sizeOfModule;
                                ModulesSystem.WriteModuleElement(newElement, statusEffect.InstanceId, ref moduleInfo);
                                length++;
                            }
                        }
                    }

                    // These are old module buffers leftover from before rollback. They are emptied now and
                    // queued in ZeroLengthModules so the ZeroLengthModulesJob removes them if they stay empty.
                    bool hasZeroLengthModules = false;
                    foreach (var typeIndex in interpolatedTypes)
                    {
                        if (!typeToIndexAndTypeInfo.TryGetValue(typeIndex, out var info))
                        {
                            info = (StatusEffectsUtility.GetIndexInTypeArray(chunk, typeIndex), TypeManager.GetTypeInfo(typeIndex));

                            typeToIndexAndTypeInfo.TryAdd(typeIndex, info);
                        }

                        // Not on this entity, so there is nothing to clear or remove.
                        if (info.IndexInTypeArray < 0)
                            continue;

                        var header = (BufferHeader*)StatusEffectsUtility.GetComponentDataWithTypeRW(chunk, i, info.IndexInTypeArray, GlobalSystemVersion);

                        if (Hint.Unlikely(header == null))
                            continue;
                        
                        header->Length = 0;
                        
                        CommandBuffer.AppendToBuffer(unfilteredChunkIndex, entity, new ZeroLengthModules { TypeIndex = typeIndex });
                        hasZeroLengthModules = true;
                    }

                    if (hasZeroLengthModules)
                        CommandBuffer.SetComponentEnabled<ZeroLengthModules>(unfilteredChunkIndex, entity, true);
                }
            }
        }
    }
#endif
}