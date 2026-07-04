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
            state.RequireForUpdate<StatusReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusVariablePreEvaluateJob = new StatusVariablePreEvaluateJob
            {
                References = SystemAPI.GetSingleton<StatusReferences>(),
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
            public StatusReferences References;
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

                var typeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo)>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);

                var valueTypeToDynamicEffectTypes = new UnsafeParallelMultiHashMap<int, TypeIndex>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);
                var idToStatusEffect = new UnsafeHashMap<uint, StatusEffects>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);
                var statusNameToEffect = new UnsafeParallelMultiHashMap<Hash128, (UnmanagedEffect, int, float)>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);
                var statusNameToDynamicFloat = new UnsafeParallelMultiHashMap<Hash128, (ValueModifier, float, int, int)>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);
                var statusNameToDynamicInt = new UnsafeParallelMultiHashMap<Hash128, (ValueModifier, int, int, int)>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);
                var statusNameToDynamicBool = new UnsafeParallelMultiHashMap<Hash128, (bool, int)>(StatusReferences.k_CollectionsInitialCapacity, Allocator.Temp);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    chunk.SetComponentEnabled(ref StatusVariablePreEvaluateUpdateHandle, i, true);

                    valueTypeToDynamicEffectTypes.Clear();
                    idToStatusEffect.Clear();
                    statusNameToEffect.Clear();
                    statusNameToDynamicFloat.Clear();
                    statusNameToDynamicInt.Clear();
                    statusNameToDynamicBool.Clear();

                    var entity = entities[i];
                    var statusEffects = statusEffectsAccessor[i];
                    var statusFloats = statusFloatsAccessor[i];
                    var statusInts = statusIntsAccessor[i];
                    var statusBools = statusBoolsAccessor[i];

                    // Map ids to status effects to quickly find all status effects affecting a specific status variable.
                    foreach (var statusEffect in statusEffects)
                    {
                        if (!References.TryGetReference(statusEffect.StatusEffectDataId, out var blob))
                            continue;

                        ref UnmanagedStatusEffectData data = ref blob.Value;
                        for (int v = 0; v < data.Effects.Length; v++)
                        {
                            ref var effect = ref data.Effects[v];
                            statusNameToEffect.Add(effect.StatusName, (effect, statusEffect.Stacks, data.BaseValue));
                            if (effect.ValueSource == ValueSource.DynamicValue)
                                valueTypeToDynamicEffectTypes.Add((int)effect.ValueType, effect.DynamicEffectInfo.TypeIndex);
                        }

                        idToStatusEffect.Add(statusEffect.Id, statusEffect);
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
                            if (*(bool*)(element + References.DynamicFloatOffsets.PostEvaluate))
                                continue;
                            statusNameToDynamicFloat.Add(*(Hash128*)(element + References.DynamicFloatOffsets.StatusName), (*(ValueModifier*)(element + References.DynamicFloatOffsets.ValueModifier), *(float*)(element + References.DynamicFloatOffsets.Value), *(int*)(element + References.DynamicFloatOffsets.Priority), idToStatusEffect[*(uint*)element].Stacks));
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
                            if (*(bool*)(element + References.DynamicIntOffsets.PostEvaluate))
                                continue;
                            statusNameToDynamicInt.Add(*(Hash128*)(element + References.DynamicIntOffsets.StatusName), (*(ValueModifier*)(element + References.DynamicIntOffsets.ValueModifier), *(int*)(element + References.DynamicIntOffsets.Value), *(int*)(element + References.DynamicIntOffsets.Priority), idToStatusEffect[*(uint*)element].Stacks));
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
                            if (*(bool*)(element + References.DynamicBoolOffsets.PostEvaluate))
                                continue;
                            statusNameToDynamicBool.Add(*(Hash128*)(element + References.DynamicBoolOffsets.StatusName), (*(bool*)(element + References.DynamicBoolOffsets.Value), *(int*)(element + References.DynamicBoolOffsets.Priority)));
                        }
                    }

                    for (int v = 0; v < statusFloats.Length; v++)
                    {
                        ref var statusFloat = ref statusFloats.ElementAt(v);
                        GetValue(ref statusFloat, statusNameToEffect.GetValuesForKey(statusFloat.StatusName), statusNameToDynamicFloat.GetValuesForKey(statusFloat.StatusName));
                    }

                    for (int v = 0; v < statusInts.Length; v++)
                    {
                        ref var statusInt = ref statusInts.ElementAt(v);
                        GetValue(ref statusInt, statusNameToEffect.GetValuesForKey(statusInt.StatusName), statusNameToDynamicInt.GetValuesForKey(statusInt.StatusName));
                    }

                    for (int v = 0; v < statusBools.Length; v++)
                    {
                        ref var statusBool = ref statusBools.ElementAt(v);
                        GetValue(ref statusBool, statusNameToEffect.GetValuesForKey(statusBool.StatusName), statusNameToDynamicBool.GetValuesForKey(statusBool.StatusName));
                    }
                }

                chunk.SetComponentEnabledForAll(ref StatusVariablePreEvaluateUpdateHandle, false);

                typeToIndexAndTypeInfo.Dispose();
                valueTypeToDynamicEffectTypes.Dispose();
                idToStatusEffect.Dispose();
                statusNameToEffect.Dispose();
                statusNameToDynamicFloat.Dispose();
                statusNameToDynamicInt.Dispose();
                statusNameToDynamicBool.Dispose();
            }

            public void GetValue(ref StatusFloats statusFloat,
                in UnsafeParallelMultiHashMap<Hash128, (UnmanagedEffect Effect, int Stacks, float BaseValue)>.Enumerator effects,
                in UnsafeParallelMultiHashMap<Hash128, (ValueModifier ValueModifier, float Value, int Priority, int Stacks)>.Enumerator dynamicFloats)
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
                in UnsafeParallelMultiHashMap<Hash128, (UnmanagedEffect Effect, int Stacks, float BaseValue)>.Enumerator effects,
                in UnsafeParallelMultiHashMap<Hash128, (ValueModifier ValueModifier, int Value, int Priority, int Stacks)>.Enumerator dynamicInts)
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
                in UnsafeParallelMultiHashMap<Hash128, (UnmanagedEffect Effect, int Stacks, float BaseValue)>.Enumerator effects,
                in UnsafeParallelMultiHashMap<Hash128, (bool Value, int Priority)>.Enumerator dynamicBools)
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