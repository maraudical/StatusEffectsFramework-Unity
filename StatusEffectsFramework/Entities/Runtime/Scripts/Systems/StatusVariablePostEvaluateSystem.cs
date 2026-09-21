using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
#if NETCODE
    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(StatusVariablePreEvaluateSystem))]
#else
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(StatusVariablePreEvaluateSystem))]
#endif
    [BurstCompile]
    public partial struct StatusVariablePostEvaluateSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusFloats, StatusInts, StatusBools>().WithAll<StatusVariablePostEvaluateUpdate, Simulate>().Build();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusVariablePostEvaluateJob = new StatusVariablePostEvaluateJob
            {
                Registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                StatusEffectsHandle = SystemAPI.GetBufferTypeHandle<StatusEffects>(true),
                StatusFloatsHandle = SystemAPI.GetBufferTypeHandle<StatusFloats>(),
                StatusIntsHandle = SystemAPI.GetBufferTypeHandle<StatusInts>(),
                StatusBoolsHandle = SystemAPI.GetBufferTypeHandle<StatusBools>(),
                StatusVariablePostEvaluateUpdateHandle = SystemAPI.GetComponentTypeHandle<StatusVariablePostEvaluateUpdate>(),
                GlobalSystemVersion = state.GlobalSystemVersion
            };
            state.Dependency = statusVariablePostEvaluateJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusVariablePostEvaluateJob : IJobChunk
        {
            public UnmanagedStatusRegistry Registry;
            public EntityTypeHandle EntityTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<StatusEffects> StatusEffectsHandle;
            public BufferTypeHandle<StatusFloats> StatusFloatsHandle;
            public BufferTypeHandle<StatusInts> StatusIntsHandle;
            public BufferTypeHandle<StatusBools> StatusBoolsHandle;
            public ComponentTypeHandle<StatusVariablePostEvaluateUpdate> StatusVariablePostEvaluateUpdateHandle;
            public uint GlobalSystemVersion;

            [BurstCompile]
            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                BufferAccessor<StatusEffects> statusEffectsAccessor = chunk.GetBufferAccessorRO(ref StatusEffectsHandle);
                BufferAccessor<StatusFloats> statusFloatsAccessor = chunk.GetBufferAccessorRW(ref StatusFloatsHandle);
                BufferAccessor<StatusInts> statusIntsAccessor = chunk.GetBufferAccessorRW(ref StatusIntsHandle);
                BufferAccessor<StatusBools> statusBoolsAccessor = chunk.GetBufferAccessorRW(ref StatusBoolsHandle);

                var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);

                using var valueTypeToDynamicEffectTypes = new UnsafeParallelMultiHashMap<int, TypeIndex>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var instanceIdToStatusEffect = new UnsafeHashMap<uint, StatusEffects>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var idToDynamicFloat = new UnsafeParallelMultiHashMap<ushort, (ValueModifier, float, int, int)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var idToDynamicInt = new UnsafeParallelMultiHashMap<ushort, (ValueModifier, int, int, int)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var idToDynamicBool = new UnsafeParallelMultiHashMap<ushort, (bool, int)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    valueTypeToDynamicEffectTypes.Clear();
                    instanceIdToStatusEffect.Clear();
                    idToDynamicFloat.Clear();
                    idToDynamicInt.Clear();
                    idToDynamicBool.Clear();

                    var entity = entities[i];
                    var statusEffects = statusEffectsAccessor[i];
                    var statusFloats = statusFloatsAccessor[i];
                    var statusInts = statusIntsAccessor[i];
                    var statusBools = statusBoolsAccessor[i];

                    // Map ids to status effects to quickly find all status effects affecting a specific status variable.
                    foreach (var statusEffect in statusEffects)
                    {
                        ref var data = ref Registry.GetStatusEffectData(statusEffect.Id);

                        for (int v = 0; v < data.Effects.Length; v++)
                        {
                            ref var effect = ref data.Effects[v];
                            if (effect.ValueSource == ValueSource.DynamicValue)
                                valueTypeToDynamicEffectTypes.Add((int)effect.ValueType, effect.DynamicEffectInfo.TypeIndex);
                        }

                        instanceIdToStatusEffect.Add(statusEffect.InstanceId, statusEffect);
                    }

                    foreach (var typeIndex in valueTypeToDynamicEffectTypes.GetValuesForKey((int)ValueType.Float))
                    {
                        if (!typeToIndexAndTypeInfo.TryGetValue(typeIndex, out var info))
                        {
                            info = (StatusEffectsECSInternals.GetIndexInTypeArray(chunk, typeIndex), TypeManager.GetTypeInfo(typeIndex));
                            typeToIndexAndTypeInfo.TryAdd(typeIndex, info);
                        }

                        if (Hint.Unlikely(info.IndexInTypeArray <= 0))
                            continue;

                        var header = StatusEffectsECSInternals.GetComponentDataWithTypeRO(chunk, i, info.IndexInTypeArray);

                        if (Hint.Unlikely(!StatusEffectsECSInternals.TryGetElementPointerAndLength(header, out var buffer, out var length)))
                            UnityEngine.Debug.LogError($"There was an issue with the provided dynamic effect type <b>{info.TypeInfo.DebugTypeName}</b>.");

                        int sizeOfDynamicEffect = info.TypeInfo.ElementSize;

                        for (int n = 0; n < length; n++)
                        {
                            var element = buffer + sizeOfDynamicEffect * n;
                            if (!*(bool*)(element + Registry.DynamicFloatOffsets.PostEvaluate))
                                continue;
                            idToDynamicFloat.Add(*(ushort*)(element + Registry.DynamicFloatOffsets.Id), (*(ValueModifier*)(element + Registry.DynamicFloatOffsets.ValueModifier), *(float*)(element + Registry.DynamicFloatOffsets.Value), *(int*)(element + Registry.DynamicFloatOffsets.Priority), instanceIdToStatusEffect[*(uint*)element].Stacks));
                        }
                    }

                    foreach (var typeIndex in valueTypeToDynamicEffectTypes.GetValuesForKey((int)ValueType.Int))
                    {
                        if (!typeToIndexAndTypeInfo.TryGetValue(typeIndex, out var info))
                        {
                            info = (StatusEffectsECSInternals.GetIndexInTypeArray(chunk, typeIndex), TypeManager.GetTypeInfo(typeIndex));
                            typeToIndexAndTypeInfo.TryAdd(typeIndex, info);
                        }

                        if (Hint.Unlikely(info.IndexInTypeArray <= 0))
                            continue;

                        var header = StatusEffectsECSInternals.GetComponentDataWithTypeRO(chunk, i, info.IndexInTypeArray);

                        if (Hint.Unlikely(!StatusEffectsECSInternals.TryGetElementPointerAndLength(header, out var buffer, out var length)))
                            UnityEngine.Debug.LogError($"There was an issue with the provided dynamic effect type <b>{info.TypeInfo.DebugTypeName}</b>.");

                        int sizeOfDynamicEffect = info.TypeInfo.ElementSize;

                        for (int n = 0; n < length; n++)
                        {
                            var element = buffer + sizeOfDynamicEffect * n;
                            if (!*(bool*)(element + Registry.DynamicIntOffsets.PostEvaluate))
                                continue;
                            idToDynamicInt.Add(*(ushort*)(element + Registry.DynamicIntOffsets.Id), (*(ValueModifier*)(element + Registry.DynamicIntOffsets.ValueModifier), *(int*)(element + Registry.DynamicIntOffsets.Value), *(int*)(element + Registry.DynamicIntOffsets.Priority), instanceIdToStatusEffect[*(uint*)element].Stacks));
                        }
                    }

                    foreach (var typeIndex in valueTypeToDynamicEffectTypes.GetValuesForKey((int)ValueType.Bool))
                    {
                        if (!typeToIndexAndTypeInfo.TryGetValue(typeIndex, out var info))
                        {
                            info = (StatusEffectsECSInternals.GetIndexInTypeArray(chunk, typeIndex), TypeManager.GetTypeInfo(typeIndex));
                            typeToIndexAndTypeInfo.TryAdd(typeIndex, info);
                        }

                        if (Hint.Unlikely(info.IndexInTypeArray <= 0))
                            continue;

                        var header = StatusEffectsECSInternals.GetComponentDataWithTypeRO(chunk, i, info.IndexInTypeArray);

                        if (Hint.Unlikely(!StatusEffectsECSInternals.TryGetElementPointerAndLength(header, out var buffer, out var length)))
                            UnityEngine.Debug.LogError($"There was an issue with the provided dynamic effect type <b>{info.TypeInfo.DebugTypeName}</b>.");

                        int sizeOfDynamicEffect = info.TypeInfo.ElementSize;

                        for (int n = 0; n < length; n++)
                        {
                            var element = buffer + sizeOfDynamicEffect * n;
                            if (!*(bool*)(element + Registry.DynamicBoolOffsets.PostEvaluate))
                                continue;
                            idToDynamicBool.Add(*(ushort*)(element + Registry.DynamicBoolOffsets.Id), (*(bool*)(element + Registry.DynamicBoolOffsets.Value), *(int*)(element + Registry.DynamicBoolOffsets.Priority)));
                        }
                    }

                    for (int v = 0; v < statusFloats.Length; v++)
                    {
                        ref var statusFloat = ref statusFloats.ElementAt(v);
                        GetValue(ref statusFloat, idToDynamicFloat.GetValuesForKey(statusFloat.Id));
                    }

                    for (int v = 0; v < statusInts.Length; v++)
                    {
                        ref var statusInt = ref statusInts.ElementAt(v);
                        GetValue(ref statusInt, idToDynamicInt.GetValuesForKey(statusInt.Id));
                    }

                    for (int v = 0; v < statusBools.Length; v++)
                    {
                        ref var statusBool = ref statusBools.ElementAt(v);
                        GetValue(ref statusBool, idToDynamicBool.GetValuesForKey(statusBool.Id));
                    }
                }

                chunk.SetComponentEnabledForAll(ref StatusVariablePostEvaluateUpdateHandle, false);
            }

            public void GetValue(ref StatusFloats statusFloat,
                in UnsafeParallelMultiHashMap<ushort, (ValueModifier ValueModifier, float Value, int Priority, int Stacks)>.Enumerator dynamicFloats)
            {
                var statusFloatValue = new StatusFloatValue(statusFloat.PreEvaluationValue, statusFloat.SignProtected);

                foreach (var dynamicFloat in dynamicFloats)
                    statusFloatValue.ApplyEffect(dynamicFloat.ValueModifier, dynamicFloat.Stacks * dynamicFloat.Value, dynamicFloat.Priority);
                statusFloat.PostEvaluationValue = statusFloatValue.GetValue();
            }

            public void GetValue(ref StatusInts statusInt,
                in UnsafeParallelMultiHashMap<ushort, (ValueModifier ValueModifier, int Value, int Priority, int Stacks)>.Enumerator dynamicInts)
            {
                var statusIntValue = new StatusIntValue(statusInt.PreEvaluationValue, statusInt.SignProtected);

                foreach (var dynamicInt in dynamicInts)
                    statusIntValue.ApplyEffect(dynamicInt.ValueModifier, dynamicInt.Stacks * dynamicInt.Value, dynamicInt.Priority);

                statusInt.PostEvaluationValue = statusIntValue.GetValue();
            }

            public void GetValue(ref StatusBools statusBool,
                in UnsafeParallelMultiHashMap<ushort, (bool Value, int Priority)>.Enumerator dynamicBools)
            {
                var statusBoolValue = new StatusBoolValue(statusBool.PreEvaluationValue);

                foreach (var dynamicBool in dynamicBools)
                    statusBoolValue.ApplyEffect(dynamicBool.Value, dynamicBool.Priority);

                statusBool.PostEvaluationValue = statusBoolValue.GetValue();
            }
        }
    }
}
