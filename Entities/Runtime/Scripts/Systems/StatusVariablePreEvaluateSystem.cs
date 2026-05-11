#if ENTITIES
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffectFramework.Entities
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
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, DynamicFloats, DynamicInts, DynamicBools, StatusFloats, StatusInts, StatusBools>().WithAll<StatusVariablePreEvaluateUpdate, Simulate>().WithPresentRW<StatusVariablePostEvaluateUpdate>().Build();
            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<StatusReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // If any entities were actually changed we update their StatusVariables.
            // This is done at the start of frame before StatusEffects structural changes.
            var statusVariablePreEvaluateJob = new StatusVariablePreEvaluateJob
            {
                References = SystemAPI.GetSingleton<StatusReferences>()
            };
            state.Dependency = statusVariablePreEvaluateJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusVariablePreEvaluateJob : IJobEntity
        {
            [ReadOnly]
            public StatusReferences References;

            public void Execute(Entity entity, 
                in DynamicBuffer<StatusEffects> statusEffects,
                in DynamicBuffer<DynamicFloats> dynamicFloats,
                in DynamicBuffer<DynamicInts> dynamicInts,
                in DynamicBuffer<DynamicBools> dynamicBools,
                ref DynamicBuffer<StatusFloats> statusFloats, 
                ref DynamicBuffer<StatusInts> statusInts, 
                ref DynamicBuffer<StatusBools> statusBools,
                EnabledRefRW<StatusVariablePreEvaluateUpdate> preEvaluateUpdate,
                EnabledRefRW<StatusVariablePostEvaluateUpdate> postEvaluateUpdate)
            {
                preEvaluateUpdate.ValueRW = false;
                postEvaluateUpdate.ValueRW = true;

                using var statusNameToEffect = new NativeParallelMultiHashMap<Hash128, (UnmanagedEffect, int, float)>(statusEffects.Length, Allocator.Temp);
                using var idToStatusEffect = new NativeHashMap<uint, StatusEffects>(statusEffects.Length, Allocator.Temp);
                using var statusNameToDynamicFloat = new NativeParallelMultiHashMap<Hash128, (DynamicFloats, int)>(statusEffects.Length, Allocator.Temp);
                using var statusNameToDynamicInt = new NativeParallelMultiHashMap<Hash128, (DynamicInts, int)>(statusEffects.Length, Allocator.Temp);
                using var statusNameToDynamicBool = new NativeParallelMultiHashMap<Hash128, (DynamicBools, int)>(statusEffects.Length, Allocator.Temp);
                // Map ids to status effects to quickly find all status effects affecting a specific status variable.
                foreach (var statusEffect in statusEffects)
                {
                    if (!References.TryGetReference(statusEffect.StatusEffectDataId, out var blob))
                        continue;

                    ref UnmanagedStatusEffectData data = ref blob.Value;
                    for (int i = 0; i < data.Effects.Length; i++)
                    {
                        ref var effect = ref data.Effects[i];
                        statusNameToEffect.Add(effect.StatusName, (effect, statusEffect.Stacks, data.BaseValue));
                    }

                    idToStatusEffect.Add(statusEffect.Id, statusEffect);
                }

                foreach (var dynamicFloat in dynamicFloats)
                    statusNameToDynamicFloat.Add(dynamicFloat.StatusName, (dynamicFloat, idToStatusEffect[dynamicFloat.Id].Stacks));

                foreach (var dynamicInt in dynamicInts)
                    statusNameToDynamicInt.Add(dynamicInt.StatusName, (dynamicInt, idToStatusEffect[dynamicInt.Id].Stacks));

                foreach (var dynamicBool in dynamicBools)
                    statusNameToDynamicBool.Add(dynamicBool.StatusName, (dynamicBool, idToStatusEffect[dynamicBool.Id].Stacks));

                for (int i = 0; i < statusFloats.Length; i++)
                {
                    ref var statusFloat = ref statusFloats.ElementAt(i);
                    GetValue(ref statusFloat, statusNameToEffect.GetValuesForKey(statusFloat.StatusName), statusNameToDynamicFloat.GetValuesForKey(statusFloat.StatusName));
                }

                for (int i = 0; i < statusInts.Length; i++)
                {
                    ref var statusInt = ref statusInts.ElementAt(i);
                    GetValue(ref statusInt, statusNameToEffect.GetValuesForKey(statusInt.StatusName), statusNameToDynamicInt.GetValuesForKey(statusInt.StatusName));
                }

                for (int i = 0; i < statusBools.Length; i++)
                {
                    ref var statusBool = ref statusBools.ElementAt(i);
                    GetValue(ref statusBool, statusNameToEffect.GetValuesForKey(statusBool.StatusName), statusNameToDynamicBool.GetValuesForKey(statusBool.StatusName));
                }
            }

            public void GetValue(ref StatusFloats statusFloat,
                in NativeParallelMultiHashMap<Hash128, (UnmanagedEffect Effect, int Stacks, float BaseValue)>.Enumerator effects,
                in NativeParallelMultiHashMap<Hash128, (DynamicFloats DynamicFloat, int Stacks)>.Enumerator dynamicFloats)
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
                    if (!dynamicFloat.DynamicFloat.PostEvaluate)
                        statusFloatValue.ApplyEffect(dynamicFloat.DynamicFloat.ValueModifier, dynamicFloat.Stacks * dynamicFloat.DynamicFloat.Value, dynamicFloat.DynamicFloat.Priority);

                statusFloat.PreEvaluationValue = statusFloatValue.GetValue();
            }

            public void GetValue(ref StatusInts statusInt,
                in NativeParallelMultiHashMap<Hash128, (UnmanagedEffect Effect, int Stacks, float BaseValue)>.Enumerator effects,
                in NativeParallelMultiHashMap<Hash128, (DynamicInts DynamicInt, int Stacks)>.Enumerator dynamicInts)
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
                    if (!dynamicInt.DynamicInt.PostEvaluate)
                        statusIntValue.ApplyEffect(dynamicInt.DynamicInt.ValueModifier, dynamicInt.Stacks * dynamicInt.DynamicInt.Value, dynamicInt.DynamicInt.Priority);

                statusInt.PreEvaluationValue = statusIntValue.GetValue();
            }

            public void GetValue(ref StatusBools statusBool,
                in NativeParallelMultiHashMap<Hash128, (UnmanagedEffect Effect, int Stacks, float BaseValue)>.Enumerator effects,
                in NativeParallelMultiHashMap<Hash128, (DynamicBools DynamicBool, int Stacks)>.Enumerator dynamicBools)
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
                    if (!dynamicBool.DynamicBool.PostEvaluate)
                        statusBoolValue.ApplyEffect(dynamicBool.DynamicBool.Value, dynamicBool.DynamicBool.Priority);

                statusBool.PreEvaluationValue = statusBoolValue.GetValue();
            }
        }
    }
}
#endif