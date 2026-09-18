#if ENTITIES
using System;
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
    [UpdateAfter(typeof(EndPredictedStatusEffectEntityCommandBufferSystem))]
#else
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(EndStatusEffectEntityCommandBufferSystem))]
#endif
    [BurstCompile]
    public partial struct StatusVariablePreEvaluateSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusFloats, StatusInts, StatusBools>().WithAll<StatusVariablePreEvaluateUpdate, Simulate>().WithPresentRW<StatusVariablePostEvaluateUpdate>().Build();
            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<UnmanagedStatusRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusVariablePreEvaluateJob = new StatusVariablePreEvaluateJob
            {
                Registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                StatusEffectsHandle = SystemAPI.GetBufferTypeHandle<StatusEffects>(true),
                StatusFloatsHandle = SystemAPI.GetBufferTypeHandle<StatusFloats>(),
                StatusIntsHandle = SystemAPI.GetBufferTypeHandle<StatusInts>(),
                StatusBoolsHandle = SystemAPI.GetBufferTypeHandle<StatusBools>(),
                StatusVariablePreEvaluateUpdateHandle = SystemAPI.GetComponentTypeHandle<StatusVariablePreEvaluateUpdate>(),
                StatusVariablePostEvaluateUpdateHandle = SystemAPI.GetComponentTypeHandle<StatusVariablePostEvaluateUpdate>(),
                GlobalSystemVersion = state.GlobalSystemVersion
            };
            state.Dependency = statusVariablePreEvaluateJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusVariablePreEvaluateJob : IJobChunk
        {
            public UnmanagedStatusRegistry Registry;
            public EntityTypeHandle EntityTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<StatusEffects> StatusEffectsHandle;
            public BufferTypeHandle<StatusFloats> StatusFloatsHandle;
            public BufferTypeHandle<StatusInts> StatusIntsHandle;
            public BufferTypeHandle<StatusBools> StatusBoolsHandle;
            public ComponentTypeHandle<StatusVariablePreEvaluateUpdate> StatusVariablePreEvaluateUpdateHandle;
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
                using var idToEffect = new UnsafeParallelMultiHashMap<ushort, (UnmanagedEffect, int, float)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var idToDynamicFloat = new UnsafeParallelMultiHashMap<ushort, (ValueModifier, float, int, int)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var idToDynamicInt = new UnsafeParallelMultiHashMap<ushort, (ValueModifier, int, int, int)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);
                using var idToDynamicBool = new UnsafeParallelMultiHashMap<ushort, (bool, int)>(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    chunk.SetComponentEnabled(ref StatusVariablePostEvaluateUpdateHandle, i, true);

                    valueTypeToDynamicEffectTypes.Clear();
                    instanceIdToStatusEffect.Clear();
                    idToEffect.Clear();
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
                        if (!Registry.TryGetStatusEffectData(statusEffect.Id, out var blob))
                            continue;

                        ref UnmanagedStatusEffectData data = ref blob.Value;
                        for (int v = 0; v < data.Effects.Length; v++)
                        {
                            ref var effect = ref data.Effects[v];
                            idToEffect.Add(effect.Id, (effect, statusEffect.Stacks, data.BaseValue));
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
                            if (*(bool*)(element + Registry.DynamicFloatOffsets.PostEvaluate))
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
                            if (*(bool*)(element + Registry.DynamicIntOffsets.PostEvaluate))
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
                            if (*(bool*)(element + Registry.DynamicBoolOffsets.PostEvaluate))
                                continue;
                            idToDynamicBool.Add(*(ushort*)(element + Registry.DynamicBoolOffsets.Id), (*(bool*)(element + Registry.DynamicBoolOffsets.Value), *(int*)(element + Registry.DynamicBoolOffsets.Priority)));
                        }
                    }

                    for (int v = 0; v < statusFloats.Length; v++)
                    {
                        ref var statusFloat = ref statusFloats.ElementAt(v);
                        GetValue(ref statusFloat, idToEffect.GetValuesForKey(statusFloat.Id), idToDynamicFloat.GetValuesForKey(statusFloat.Id));
                    }

                    for (int v = 0; v < statusInts.Length; v++)
                    {
                        ref var statusInt = ref statusInts.ElementAt(v);
                        GetValue(ref statusInt, idToEffect.GetValuesForKey(statusInt.Id), idToDynamicInt.GetValuesForKey(statusInt.Id));
                    }

                    for (int v = 0; v < statusBools.Length; v++)
                    {
                        ref var statusBool = ref statusBools.ElementAt(v);
                        GetValue(ref statusBool, idToEffect.GetValuesForKey(statusBool.Id), idToDynamicBool.GetValuesForKey(statusBool.Id));
                    }
                }

                chunk.SetComponentEnabledForAll(ref StatusVariablePreEvaluateUpdateHandle, false);
            }

            public void GetValue(ref StatusFloats statusFloat,
                in UnsafeParallelMultiHashMap<ushort, (UnmanagedEffect Effect, int Stacks, float BaseValue)>.Enumerator effects,
                in UnsafeParallelMultiHashMap<ushort, (ValueModifier ValueModifier, float Value, int Priority, int Stacks)>.Enumerator dynamicFloats)
            {
                var statusFloatValue = new StatusFloatValue(statusFloat.BaseValue, statusFloat.SignProtected);

                float effectValue = default;

                foreach (var effect in effects)
                {
                    switch (effect.Effect.ValueSource)
                    {
                        case ValueSource.ExplicitValue:
                            effectValue = effect.Stacks * effect.Effect.FloatValue;
                            break;
                        case ValueSource.BaseValue:
                            effectValue = effect.Stacks * effect.BaseValue;
                            break;
                        case ValueSource.DynamicValue:
                            continue;
                    }

                    statusFloatValue.ApplyEffect(effect.Effect.ValueModifier, effectValue, effect.Effect.Priority);
                }

                foreach (var dynamicFloat in dynamicFloats)
                    statusFloatValue.ApplyEffect(dynamicFloat.ValueModifier, dynamicFloat.Stacks * dynamicFloat.Value, dynamicFloat.Priority);

                statusFloat.PreEvaluationValue = statusFloatValue.GetValue();
            }

            public void GetValue(ref StatusInts statusInt,
                in UnsafeParallelMultiHashMap<ushort, (UnmanagedEffect Effect, int Stacks, float BaseValue)>.Enumerator effects,
                in UnsafeParallelMultiHashMap<ushort, (ValueModifier ValueModifier, int Value, int Priority, int Stacks)>.Enumerator dynamicInts)
            {
                var statusIntValue = new StatusIntValue(statusInt.BaseValue, statusInt.SignProtected);

                int effectValue = default;

                foreach (var effect in effects)
                {
                    switch (effect.Effect.ValueSource)
                    {
                        case ValueSource.ExplicitValue:
                            effectValue = effect.Stacks * effect.Effect.IntValue;
                            break;
                        case ValueSource.BaseValue:
                            effectValue = effect.Stacks * (int)effect.BaseValue;
                            break;
                        case ValueSource.DynamicValue:
                            continue;
                    }

                    statusIntValue.ApplyEffect(effect.Effect.ValueModifier, effectValue, effect.Effect.Priority);
                }

                foreach (var dynamicInt in dynamicInts)
                    statusIntValue.ApplyEffect(dynamicInt.ValueModifier, dynamicInt.Stacks * dynamicInt.Value, dynamicInt.Priority);

                statusInt.PreEvaluationValue = statusIntValue.GetValue();
            }

            public void GetValue(ref StatusBools statusBool,
                in UnsafeParallelMultiHashMap<ushort, (UnmanagedEffect Effect, int Stacks, float BaseValue)>.Enumerator effects,
                in UnsafeParallelMultiHashMap<ushort, (bool Value, int Priority)>.Enumerator dynamicBools)
            {
                var statusBoolValue = new StatusBoolValue(statusBool.BaseValue);

                bool effectValue = default;

                foreach (var effect in effects)
                {
                    switch (effect.Effect.ValueSource)
                    {
                        case ValueSource.ExplicitValue:
                            effectValue = effect.Effect.BoolValue;
                            break;
                        case ValueSource.BaseValue:
                            effectValue = Convert.ToBoolean(effect.BaseValue);
                            break;
                        case ValueSource.DynamicValue:
                            continue;
                    }

                    statusBoolValue.ApplyEffect(effectValue, effect.Effect.Priority);
                }

                foreach (var dynamicBool in dynamicBools)
                    statusBoolValue.ApplyEffect(dynamicBool.Value, dynamicBool.Priority);

                statusBool.PreEvaluationValue = statusBoolValue.GetValue();
            }
        }
    }
}
#endif