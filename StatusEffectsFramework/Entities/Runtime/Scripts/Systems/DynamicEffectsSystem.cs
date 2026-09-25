#if ENTITIES
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
            var job = new DynamicEffectsJob()
            {
                Registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>(),
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

                var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                var typeToLength = new UnsafeHashMap<TypeIndex, int>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                var sizeOfUint = UnsafeUtility.SizeOf<uint>();
                var sizeOfUshort = UnsafeUtility.SizeOf<ushort>();
                var sizeOfValueModifier = UnsafeUtility.SizeOf<ValueModifier>();
                var sizeOfBool = UnsafeUtility.SizeOf<bool>();
                var sizeOfInt = UnsafeUtility.SizeOf<int>();
                var sizeOfFloat = UnsafeUtility.SizeOf<float>();

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

                while (enumerator.NextEntityIndex(out var i))
                {
                    var entity = entities[i];
                    var statusEffectEvents = statusEffectEventsAccessor[i];
                    typeToLength.Clear();

                    foreach (var statusEffectEvent in statusEffectEvents)
                    {
                        ref var data = ref Registry.GetStatusEffectData(statusEffectEvent.Id);

                        for (int v = 0; v < data.Effects.Length; v++)
                        {
                            ref var effect = ref data.Effects[v];

                            if (effect.ValueSource != ValueSource.DynamicValue)
                                continue;

                            typeIndex = effect.DynamicEffectInfo.TypeIndex;

                            var effectStructPtr = effect.DynamicEffectInfo.Bytes.GetUnsafePtr();

                            if (!typeToIndexAndTypeInfo.TryGetValue(typeIndex, out var info))
                            {
                                info = (StatusEffectsECSInternals.GetIndexInTypeArray(chunk, typeIndex), TypeManager.GetTypeInfo(typeIndex));
                                typeToIndexAndTypeInfo.TryAdd(typeIndex, info);
                            }

                            int sizeOfDynamicEffect = info.TypeInfo.ElementSize;

                            switch (statusEffectEvent.Event)
                            {
                                // The actual adding to the buffer will be done in another system since there is a
                                // chance we will have to wait for structural changes before making any changes.
                                case StatusEffectEvent.Added:
                                    ref var addLength = ref typeToLength.TryGetValueByRef(typeIndex, out bool aFoundLength);
                                    var componentType = ComponentType.FromTypeIndex(typeIndex);

                                    if (aFoundLength)
                                        addLength++;
                                    else
                                    {
                                        typeToLength.TryAdd(typeIndex, 1);
                                        if (info.IndexInTypeArray < 0)
                                            CommandBuffer.AddComponent(unfilteredChunkIndex, entity, componentType);
                                    }

                                    var ptr = (byte*)UnsafeUtility.Malloc(sizeOfDynamicEffect, info.TypeInfo.AlignmentInBytes, Allocator.Temp);
                                    UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.InstanceIdOffset, &statusEffectEvent.InstanceId, sizeOfUint);
                                    switch (effect.ValueType)
                                    {
                                        case ValueType.Float:
                                        case ValueType.Int:
                                            UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.IdOffset, UnsafeUtility.AddressOf(ref effect.Id), sizeOfUshort);
                                            UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.ValueModifierOffset, UnsafeUtility.AddressOf(ref effect.ValueModifier), sizeOfValueModifier);
                                            UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.PostEvaluateOffset, UnsafeUtility.AddressOf(ref effect.PostEvaluate), sizeOfBool);
                                            UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.PriorityOffset, UnsafeUtility.AddressOf(ref effect.Priority), sizeOfInt);
                                            UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.StructOffset, effectStructPtr, effect.DynamicEffectInfo.Size);
                                            break;
                                        case ValueType.Bool:
                                            UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.IdOffset, UnsafeUtility.AddressOf(ref effect.Id), sizeOfUshort);
                                            UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.PostEvaluateOffset, UnsafeUtility.AddressOf(ref effect.PostEvaluate), sizeOfBool);
                                            UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.PriorityOffset, UnsafeUtility.AddressOf(ref effect.Priority), sizeOfInt);
                                            UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.StructOffset, effectStructPtr, effect.DynamicEffectInfo.Size);
                                            break;
                                        default:
                                            UnityEngine.Debug.LogError($"The value type <b>{effect.ValueType}</b> is not supported for dynamic effects in Entities.");
                                            break;
                                    }
                                    StatusEffectsECSInternals.AppendToBuffer(ref CommandBuffer, unfilteredChunkIndex, entity, componentType, sizeOfDynamicEffect, ptr);
                                    UnsafeUtility.Free(ptr, Allocator.Temp);
                                    break;
                                case StatusEffectEvent.Removed:
                                    if (info.IndexInTypeArray < 0)
                                        break;

                                    var header = StatusEffectsECSInternals.GetComponentDataWithTypeRW(chunk, i, info.IndexInTypeArray, GlobalSystemVersion);

                                    if (Hint.Unlikely(!StatusEffectsECSInternals.TryGetElementPointerAndLength(header, out var buffer, out var length)))
                                        UnityEngine.Debug.LogError($"There was an issue with the provided dynamic effect type <b>{info.TypeInfo.DebugTypeName}</b>.");

                                    ref var removeLength = ref typeToLength.TryGetValueByRef(typeIndex, out bool rFoundLength);

                                    for (int n = length - 1; n >= 0; n--)
                                    {
                                        uint id = *(uint*)(buffer + sizeOfDynamicEffect * n + effect.DynamicEffectInfo.InstanceIdOffset);
                                        if (id != statusEffectEvent.InstanceId)
                                            continue;

                                        StatusEffectsECSInternals.RemoveAtSwapBack(header, sizeOfDynamicEffect, n);

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
#if NETCODE

    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateBefore(typeof(DynamicEffectsSystem))]
    [BurstCompile]
    public unsafe partial struct FirstPredictionTickDynamicEffectsSystem : ISystem
    {
        EntityQuery m_EntityQuery;

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

            var firstPredictionTickJob = new DynamicEffectsFirstPredictionTickJob()
            {
                NetworkTime = networkTime,
                Registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>(),
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
            public NetworkTime NetworkTime;
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

                var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                var interpolatedTypes = new UnsafeHashSet<TypeIndex>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                var typeAlreadyProcessed = new UnsafeHashSet<TypeIndex>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                var sizeOfUint = UnsafeUtility.SizeOf<uint>();
                var sizeOfUshort = UnsafeUtility.SizeOf<ushort>();
                var sizeOfValueModifier = UnsafeUtility.SizeOf<ValueModifier>();
                var sizeOfBool = UnsafeUtility.SizeOf<bool>();
                var sizeOfInt = UnsafeUtility.SizeOf<int>();
                var sizeOfFloat = UnsafeUtility.SizeOf<float>();

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

                        ref var data = ref Registry.GetStatusEffectData(interpolatedStatusEffect.Id);

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
                        if (index >= 0 && statusEffectEvents[index].Event is StatusEffectEvent.Added)
                            continue;

                        ref var data = ref Registry.GetStatusEffectData(statusEffect.Id);

                        for (int v = 0; v < data.Effects.Length; v++)
                        {
                            ref var effect = ref data.Effects[v];

                            if (effect.ValueSource != ValueSource.DynamicValue)
                                continue;

                            typeIndex = effect.DynamicEffectInfo.TypeIndex;

                            var effectStructPtr = effect.DynamicEffectInfo.Bytes.GetUnsafePtr();
                            var componentType = ComponentType.FromTypeIndex(typeIndex);

                            interpolatedTypes.Remove(typeIndex);

                            if (!typeToIndexAndTypeInfo.TryGetValue(typeIndex, out var info))
                            {
                                info = (StatusEffectsECSInternals.GetIndexInTypeArray(chunk, typeIndex), TypeManager.GetTypeInfo(typeIndex));

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
                                UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.InstanceIdOffset, &statusEffect.InstanceId, sizeOfUint);
                                switch (effect.ValueType)
                                {
                                    case ValueType.Float:
                                    case ValueType.Int:
                                        UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.IdOffset, UnsafeUtility.AddressOf(ref effect.Id), sizeOfUshort);
                                        UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.ValueModifierOffset, UnsafeUtility.AddressOf(ref effect.ValueModifier), sizeOfValueModifier);
                                        UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.PostEvaluateOffset, UnsafeUtility.AddressOf(ref effect.PostEvaluate), sizeOfBool);
                                        UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.PriorityOffset, UnsafeUtility.AddressOf(ref effect.Priority), sizeOfInt);
                                        UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.StructOffset, effectStructPtr, effect.DynamicEffectInfo.Size);
                                        break;
                                    case ValueType.Bool:
                                        UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.IdOffset, UnsafeUtility.AddressOf(ref effect.Id), sizeOfUshort);
                                        UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.PostEvaluateOffset, UnsafeUtility.AddressOf(ref effect.PostEvaluate), sizeOfBool);
                                        UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.PriorityOffset, UnsafeUtility.AddressOf(ref effect.Priority), sizeOfInt);
                                        UnsafeUtility.MemCpy(ptr + effect.DynamicEffectInfo.StructOffset, effectStructPtr, effect.DynamicEffectInfo.Size);
                                        break;
                                    default:
                                        UnityEngine.Debug.LogError($"The value type <b>{effect.ValueType}</b> is not supported for dynamic effects in Entities.");
                                        break;
                                }
                                StatusEffectsECSInternals.AppendToBuffer(ref CommandBuffer, unfilteredChunkIndex, entity, componentType, sizeOfDynamicEffect, ptr);
                                UnsafeUtility.Free(ptr, Allocator.Temp);
                            }
                            else
                            {
                                var header = StatusEffectsECSInternals.GetComponentDataWithTypeRW(chunk, i, info.IndexInTypeArray, GlobalSystemVersion);

                                if (Hint.Unlikely(!StatusEffectsECSInternals.TryGetElementPointerAndLength(header, out var buffer, out var length)))
                                    UnityEngine.Debug.LogError($"There was an issue with the provided module type <b>{info.TypeInfo.DebugTypeName}</b>.");

                                ref var lengthAsRef = ref StatusEffectsECSInternals.LengthAsRef(header);

                                if (!typeAlreadyProcessed.Contains(typeIndex))
                                {
                                    lengthAsRef = 0;
                                    typeAlreadyProcessed.Add(typeIndex);
                                }

                                StatusEffectsECSInternals.EnsureCapacity(header, lengthAsRef + 1, sizeOfDynamicEffect, info.TypeInfo.AlignmentInBytes);

                                var newElement = buffer + lengthAsRef * sizeOfDynamicEffect;
                                UnsafeUtility.MemCpy(newElement + effect.DynamicEffectInfo.InstanceIdOffset, &statusEffect.InstanceId, sizeOfUint);
                                switch (effect.ValueType)
                                {
                                    case ValueType.Float:
                                    case ValueType.Int:
                                        UnsafeUtility.MemCpy(newElement + effect.DynamicEffectInfo.IdOffset, UnsafeUtility.AddressOf(ref effect.Id), sizeOfUshort);
                                        UnsafeUtility.MemCpy(newElement + effect.DynamicEffectInfo.ValueModifierOffset, UnsafeUtility.AddressOf(ref effect.ValueModifier), sizeOfValueModifier);
                                        UnsafeUtility.MemCpy(newElement + effect.DynamicEffectInfo.PostEvaluateOffset, UnsafeUtility.AddressOf(ref effect.PostEvaluate), sizeOfBool);
                                        UnsafeUtility.MemCpy(newElement + effect.DynamicEffectInfo.PriorityOffset, UnsafeUtility.AddressOf(ref effect.Priority), sizeOfInt);
                                        UnsafeUtility.MemCpy(newElement + effect.DynamicEffectInfo.StructOffset, effectStructPtr, effect.DynamicEffectInfo.Size);
                                        break;
                                    case ValueType.Bool:
                                        UnsafeUtility.MemCpy(newElement + effect.DynamicEffectInfo.IdOffset, UnsafeUtility.AddressOf(ref effect.Id), sizeOfUshort);
                                        UnsafeUtility.MemCpy(newElement + effect.DynamicEffectInfo.PostEvaluateOffset, UnsafeUtility.AddressOf(ref effect.PostEvaluate), sizeOfBool);
                                        UnsafeUtility.MemCpy(newElement + effect.DynamicEffectInfo.PriorityOffset, UnsafeUtility.AddressOf(ref effect.Priority), sizeOfInt);
                                        UnsafeUtility.MemCpy(newElement + effect.DynamicEffectInfo.StructOffset, effectStructPtr, effect.DynamicEffectInfo.Size);
                                        break;
                                    default:
                                        UnityEngine.Debug.LogError($"The value type <b>{effect.ValueType}</b> is not supported for dynamic effects in Entities.");
                                        break;
                                }
                                lengthAsRef++;
                            }
                        }
                    }
                    // These are old module buffers leftover from before rollback.
                    foreach (var t in interpolatedTypes)
                        CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, ComponentType.FromTypeIndex(t));
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