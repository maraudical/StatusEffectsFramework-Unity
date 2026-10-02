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
#if NETCODE
    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndPredictedStatusEffectEntityCommandBufferSystem))]
#else
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndStatusEffectEntityCommandBufferSystem))]
#endif
    [BurstCompile]
    public partial struct DynamicEffectsSystem : ISystem
    {
        EntityQuery m_EntityQuery;
        private StatusTypeDependencies m_Dependencies;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents, Simulate>().Build();

            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<UnmanagedStatusRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>();
            m_Dependencies.Register(ref state, registry.Version, ref registry.DynamicEffectTypes, isReadOnly: false);

            var job = new DynamicEffectsJob()
            {
                Registry = registry,
#if NETCODE
                CommandBuffer = SystemAPI.GetSingleton<EndPredictedStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
#else
                CommandBuffer = SystemAPI.GetSingleton<EndStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
#endif
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                StatusEffectEventsHandle = SystemAPI.GetBufferTypeHandle<StatusEffectEvents>(true),
                GlobalSystemVersion = state.GlobalSystemVersion,
            };
            state.Dependency = job.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        /// <summary>
        /// Writes a new dynamic effect element for <paramref name="effect"/> to <paramref name="element"/>.
        /// </summary>
        internal static unsafe void WriteDynamicElement(byte* element, uint instanceId, ref UnmanagedEffect effect)
        {
            ref var info = ref effect.DynamicEffectInfo;

            *(uint*)(element + info.InstanceIdOffset) = instanceId;
            *(ushort*)(element + info.IdOffset) = effect.Id;
            *(bool*)(element + info.PostEvaluateOffset) = effect.PostEvaluate;
            *(int*)(element + info.PriorityOffset) = effect.Priority;

            // Value is written by user systems, so new elements start from the default.
            if (effect.ValueType == ValueType.Bool)
                *(bool*)(element + info.ValueOffset) = false;
            else
            {
                *(ValueModifier*)(element + info.ValueModifierOffset) = effect.ValueModifier;
                *(int*)(element + info.ValueOffset) = 0; // 0 and 0f share the same bits.
            }

            UnsafeUtility.MemCpy(element + info.StructOffset, info.Bytes.GetUnsafePtr(), info.Size);
        }

        [BurstCompile]
        internal unsafe struct DynamicEffectsJob : IJobChunk
        {
            public UnmanagedStatusRegistry Registry;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public EntityTypeHandle EntityTypeHandle;
            public BufferTypeHandle<StatusEffectEvents> StatusEffectEventsHandle;
            public uint GlobalSystemVersion;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                BufferAccessor<StatusEffectEvents> statusEffectEventsAccessor = chunk.GetBufferAccessorRO(ref StatusEffectEventsHandle);
                TypeIndex typeIndex;

                using var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var typeToLength = new UnsafeHashMap<TypeIndex, int>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

                while (enumerator.NextEntityIndex(out var i))
                {
                    var entity = entities[i];
                    var statusEffectEvents = statusEffectEventsAccessor[i];
                    typeToLength.Clear();

                    foreach (var statusEffectEvent in statusEffectEvents)
                    {
                        ref var data = ref Registry.GetStatusEffectDataOrNullRefDebug(statusEffectEvent.Id, out bool exists);
                        if (!exists)
                            continue;

                        for (int v = 0; v < data.Effects.Length; v++)
                        {
                            ref var effect = ref data.Effects[v];

                            if (effect.ValueSource != ValueSource.DynamicValue)
                                continue;

                            typeIndex = effect.DynamicEffectInfo.TypeIndex;


                            if (!typeToIndexAndTypeInfo.TryGetValue(typeIndex, out var info))
                            {
                                info = (StatusEffectsUtility.GetIndexInTypeArray(chunk, typeIndex), TypeManager.GetTypeInfo(typeIndex));
                                typeToIndexAndTypeInfo.TryAdd(typeIndex, info);
                            }

                            int sizeOfDynamicEffect = info.TypeInfo.ElementSize;

                            switch (statusEffectEvent.Event)
                            {
                                // The actual adding to the buffer will be done in another system since there is a
                                // chance we will have to wait for structural changes before making any changes.
                                case StatusEffectEvent.Added:
                                    ref var addLength = ref typeToLength.GetValueRefOrNullRef(typeIndex, out bool aFoundLength);
                                    var componentType = ComponentType.FromTypeIndex(typeIndex);

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
                                        typeToLength.TryAdd(typeIndex, currentLength + 1);
                                    }

                                    var ptr = (byte*)UnsafeUtility.Malloc(sizeOfDynamicEffect, info.TypeInfo.AlignmentInBytes, Allocator.Temp);
                                    WriteDynamicElement(ptr, statusEffectEvent.InstanceId, ref effect);
                                    StatusEffectsUtility.AppendToBuffer(ref CommandBuffer, unfilteredChunkIndex, entity, componentType, sizeOfDynamicEffect, ptr);
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
                                    ref var removeLength = ref typeToLength.GetValueRefOrNullRef(typeIndex, out bool rFoundLength);

                                    for (int n = length - 1; n >= 0; n--)
                                    {
                                        uint id = *(uint*)(buffer + sizeOfDynamicEffect * n + effect.DynamicEffectInfo.InstanceIdOffset);
                                        if (id != statusEffectEvent.InstanceId)
                                            continue;

                                        StatusEffectsUtility.RemoveAtSwapBack(header, sizeOfDynamicEffect, n);

                                        if (rFoundLength)
                                            removeLength--;
                                        else
                                            typeToLength.TryAdd(typeIndex, length - 1);

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

                    foreach (var kvp in typeToLength)
                    {
                        if (kvp.Value <= 0)
                            CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, ComponentType.FromTypeIndex(kvp.Key));
                    }
                }
            }
        }
    }
#if NETCODE

    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateBefore(typeof(DynamicEffectsSystem))]
    [BurstCompile]
    public unsafe partial struct FirstPredictionTickDynamicEffectsSystem : ISystem
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
            m_Dependencies.Register(ref state, registry.Version, ref registry.DynamicEffectTypes, isReadOnly: false);

            var firstPredictionTickJob = new DynamicEffectsFirstPredictionTickJob()
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
        internal struct DynamicEffectsFirstPredictionTickJob : IJobChunk
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
                TypeIndex typeIndex;

                using var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var interpolatedTypes = new UnsafeHashSet<TypeIndex>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var typeAlreadyProcessed = new UnsafeHashSet<TypeIndex>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var addedTypes = new UnsafeHashSet<TypeIndex>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    var statusEffects = statusEffectsAccessor[i];
                    var interpolatedStatusEffects = interpolatedStatusEffectsAccessor[i];

                    interpolatedTypes.Clear();
                    typeAlreadyProcessed.Clear();
                    addedTypes.Clear();

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

                        for (int e = 0; e < data.Effects.Length; e++)
                            if (data.Effects[e].ValueSource == ValueSource.DynamicValue)
                                interpolatedTypes.Add(data.Effects[e].DynamicEffectInfo.TypeIndex);
                    }

                    if (noChange && statusEffects.Length == interpolatedStatusEffects.Length)
                        continue;

                    var entity = entities[i];
                    var statusEffectEvents = statusEffectEventsAccessor[i].AsNativeArray();

                    foreach (var statusEffect in statusEffects)
                    {
                        int index = statusEffectEvents.IndexOf(statusEffect.InstanceId);
                        bool addedThisTick = index >= 0 && statusEffectEvents[index].Event is StatusEffectEvent.Added;

                        ref var data = ref Registry.GetStatusEffectDataOrNullRefDebug(statusEffect.Id, out bool exists);
                        if (!exists)
                            continue;

                        for (int v = 0; v < data.Effects.Length; v++)
                        {
                            ref var effect = ref data.Effects[v];

                            if (effect.ValueSource != ValueSource.DynamicValue)
                                continue;

                            typeIndex = effect.DynamicEffectInfo.TypeIndex;

                            interpolatedTypes.Remove(typeIndex);

                            // DynamicEffectsSystem appends effects added this tick, so only keep their type.
                            if (addedThisTick)
                            {
                                addedTypes.Add(typeIndex);
                                continue;
                            }

                            var componentType = ComponentType.FromTypeIndex(typeIndex);

                            if (!typeToIndexAndTypeInfo.TryGetValue(typeIndex, out var info))
                            {
                                info = (StatusEffectsUtility.GetIndexInTypeArray(chunk, typeIndex), TypeManager.GetTypeInfo(typeIndex));

                                typeToIndexAndTypeInfo.TryAdd(typeIndex, info);
                            }

                            int sizeOfDynamicEffect = info.TypeInfo.ElementSize;

                            if (info.IndexInTypeArray < 0)
                            {
                                if (!typeAlreadyProcessed.Contains(typeIndex))
                                {
                                    CommandBuffer.AddComponent(unfilteredChunkIndex, entity, componentType);
                                    typeAlreadyProcessed.Add(typeIndex);
                                }
                                var ptr = (byte*)UnsafeUtility.Malloc(sizeOfDynamicEffect, info.TypeInfo.AlignmentInBytes, Allocator.Temp);
                                DynamicEffectsSystem.WriteDynamicElement(ptr, statusEffect.InstanceId, ref effect);
                                StatusEffectsUtility.AppendToBuffer(ref CommandBuffer, unfilteredChunkIndex, entity, componentType, sizeOfDynamicEffect, ptr);
                                UnsafeUtility.Free(ptr, Allocator.Temp);
                            }
                            else
                            {
                                var header = (BufferHeader*)StatusEffectsUtility.GetComponentDataWithTypeRW(chunk, i, info.IndexInTypeArray, GlobalSystemVersion);

                                if (Hint.Unlikely(header == null))
                                    continue;

                                ref var length = ref header->Length;

                                if (!typeAlreadyProcessed.Contains(typeIndex))
                                {
                                    length = 0;
                                    typeAlreadyProcessed.Add(typeIndex);
                                }

                                BufferHeader.EnsureCapacity(header, length + 1, sizeOfDynamicEffect, info.TypeInfo.AlignmentInBytes, BufferHeader.TrashMode.RetainOldData, false, 0);
                                
                                var buffer = BufferHeader.GetElementPointer(header);

                                var newElement = buffer + length * sizeOfDynamicEffect;
                                DynamicEffectsSystem.WriteDynamicElement(newElement, statusEffect.InstanceId, ref effect);
                                length++;
                            }
                        }
                    }
                    // Types only needed by effects added this tick weren't rebuilt above, so any elements
                    // in them are leftover from before rollback.
                    foreach (var t in addedTypes)
                    {
                        if (typeAlreadyProcessed.Contains(t))
                            continue;

                        if (!typeToIndexAndTypeInfo.TryGetValue(t, out var info))
                        {
                            info = (StatusEffectsUtility.GetIndexInTypeArray(chunk, t), TypeManager.GetTypeInfo(t));
                            typeToIndexAndTypeInfo.TryAdd(t, info);
                        }

                        if (info.IndexInTypeArray < 0)
                            continue;

                        var header = (BufferHeader*)StatusEffectsUtility.GetComponentDataWithTypeRW(chunk, i, info.IndexInTypeArray, GlobalSystemVersion);

                        if (Hint.Unlikely(header == null))
                            continue;

                        header->Length = 0;
                    }

                    // These are old dynamic effect buffers leftover from before rollback.
                    foreach (var t in interpolatedTypes)
                        CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, ComponentType.FromTypeIndex(t));
                }
            }
        }
    }
#endif
}