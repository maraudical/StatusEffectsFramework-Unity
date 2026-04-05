#if ENTITIES
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
    public partial struct StatusVariableUpdateSystem : ISystem
    {
        private EntityQuery m_StatusVariableUpdateQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_StatusVariableUpdateQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusFloats, StatusInts, StatusBools>().WithAll<StatusEffectEvents, Simulate>().Build();
            state.RequireForUpdate(m_StatusVariableUpdateQuery);
            state.RequireForUpdate<StatusReferences>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // If any entities were actually changed we update their StatusVariables.
            // This is done at the start of frame before StatusEffects structural changes.
            var statusVariableUpdateJob = new StatusVariableUpdateJob
            {
                References = SystemAPI.GetSingleton<StatusReferences>()
            };
            state.Dependency = statusVariableUpdateJob.ScheduleParallelByRef(m_StatusVariableUpdateQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusVariableUpdateJob : IJobEntity
        {
            [ReadOnly]
            public StatusReferences References;

            public void Execute(Entity entity, 
                in DynamicBuffer<StatusEffects> statusEffects, 
                ref DynamicBuffer<StatusFloats> statusFloats, 
                ref DynamicBuffer<StatusInts> statusInts, 
                ref DynamicBuffer<StatusBools> statusBools)
            {
                using var effectIdToStatusEffect = new NativeParallelMultiHashMap<Hash128, StatusEffects>(statusEffects.Length, Allocator.Temp);
                // Map ids to status effects to quickly find all status effects affecting a specific status variable.
                foreach (var statusEffect in statusEffects)
                {
                    if (!References.TryGetReference(statusEffect.StatusEffectDataId, out var blob))
                        continue;

                    ref UnmanagedStatusEffectData data = ref blob.Value;
                    for (int i = 0; i < data.Effects.Length; i++)
                        effectIdToStatusEffect.Add(data.Effects[i].StatusName, statusEffect);
                }

                for (int i = 0; i < statusFloats.Length; i++)
                {
                    ref var statusFloat = ref statusFloats.ElementAt(i);
                    GetValue(ref statusFloat, effectIdToStatusEffect.GetValuesForKey(statusFloat.StatusName), References);
                }

                for (int i = 0; i < statusInts.Length; i++)
                {
                    ref var statusInt = ref statusInts.ElementAt(i);
                    GetValue(ref statusInt, effectIdToStatusEffect.GetValuesForKey(statusInt.StatusName), References);
                }

                for (int i = 0; i < statusBools.Length; i++)
                {
                    ref var statusBool = ref statusBools.ElementAt(i);
                    GetValue(ref statusBool, effectIdToStatusEffect.GetValuesForKey(statusBool.StatusName), , References);
                }
            }

            // Copied from regular StatusFloat.GetValue() with burstable types and math.
            public void GetValue(ref StatusFloats statusFloat, 
                in NativeParallelMultiHashMap<Hash128, StatusEffects>.Enumerator statusEffects, 
                in StatusReferences references)
            {
                UnmanagedEffect effect;

                bool positive = math.sign(statusFloat.BaseValue) >= 0;
                float additiveValue = 0;
                float multiplicativeValue = 1;
                float postAdditiveValue = 0;
                int minimumPriority = -1;
                float minimumValue = float.NegativeInfinity;
                int maximumPriority = -1;
                float maximumValue = float.PositiveInfinity;
                int overwritePriority = -1;
                float overwriteValue = 0;

                float effectValue;
                
                foreach (var statusEffect in statusEffects)
                {
                    if (!references.TryGetReference(statusEffect.StatusEffectDataId, out var blob))
                        continue;

                    ref UnmanagedStatusEffectData data = ref blob.Value;

                    for (int i = 0; i < data.Effects.Length; i++)
                    {
                        effect = data.Effects[i];
                        
                        if (effect.StatusName != statusFloat.StatusName)
                            continue;
                        
                        effectValue = statusEffect.Stacks * (effect.UseBaseValue ? data.BaseValue : effect.FloatValue);
                        
                        switch (effect.ValueModifier)
                        {
                            case ValueModifier.Additive:
                                additiveValue += effectValue;
                                break;
                            case ValueModifier.Multiplicative:
                                multiplicativeValue += effectValue;
                                break;
                            case ValueModifier.PostAdditive:
                                postAdditiveValue += effectValue;
                                break;
                            case ValueModifier.Minimum:
                                if (minimumPriority < effect.Priority)
                                {
                                    minimumPriority = effect.Priority;
                                    minimumValue = effectValue;
                                }
                                else if (minimumPriority == effect.Priority)
                                    minimumValue = math.max(minimumValue, effectValue);
                                break;
                            case ValueModifier.Maximum:
                                if (maximumPriority < effect.Priority)
                                {
                                    maximumPriority = effect.Priority;
                                    maximumValue = effectValue;
                                }
                                else if (maximumPriority == effect.Priority)
                                    maximumValue = math.min(maximumValue, effectValue);
                                break;
                            case ValueModifier.Overwrite:
                                if (overwritePriority <= effect.Priority)
                                {
                                    overwritePriority = effect.Priority;
                                    overwriteValue = effectValue;
                                }
                                break;
                        }
                    }
                }
                
                if (overwritePriority >= 0)
                    statusFloat.Value = math.clamp(overwriteValue, overwritePriority <= minimumPriority ? minimumValue : float.NegativeInfinity, overwritePriority <= maximumPriority ? maximumValue : float.PositiveInfinity);
                else if (statusFloat.SignProtected)
                    statusFloat.Value = math.clamp((statusFloat.BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue, math.max(positive ? 0 : float.NegativeInfinity, minimumValue), math.min(positive ? float.PositiveInfinity : 0, maximumValue));
                else
                    statusFloat.Value = math.clamp((statusFloat.BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue, minimumValue, maximumValue);
            }

            // Copied from regular StatusInt.GetValue() with burstable types and math.
            public void GetValue(ref StatusInts statusInt, 
                in NativeParallelMultiHashMap<Hash128, StatusEffects>.Enumerator statusEffects,
                in NativeParallelMultiHashMap<Hash128, DynamicInts>.Enumerator dynamicInts,
                in StatusReferences references)
            {
                UnmanagedEffect effect;

                var statusIntValue = new StatusIntValue(statusInt.BaseValue, statusInt.SignProtected);

                int effectValue = default;

                foreach (var statusEffect in statusEffects)
                {
                    ref UnmanagedStatusEffectData data = ref references.IdToStatusEffectDataMap.Value[statusEffect.StatusEffectDataId].Value;

                    for (int i = 0; i < data.Effects.Length; i++)
                    {
                        RRrrr//
                        ValueTuple<UnmanagedEffect, StatusEffects, float> test;
                        // in one group include the effect, se, and base val
                        // for dynamics I need too  include stack count
                        effect = data.Effects[i];

                        if (effect.StatusName != statusInt.StatusName)
                            continue;

                        switch (effect.ValueType)
                        {
                            case ValueType.ExplicitValue:
                                effectValue = statusEffect.Stacks * effect.IntValue;
                                break;
                            case ValueType.BaseValue:
                                effectValue = statusEffect.Stacks * (int)data.BaseValue;
                                break;
                            case ValueType.DynamicValue:
                                continue;
                        }

                        statusIntValue.ApplyEffect(effect.ValueModifier, effectValue, effect.Priority);
                    }
                }
                hmmmm//deal with stacks
                foreach (var dynamicInt in dynamicInts)
                    if (!dynamicInt.PostEvaluate)
                        statusIntValue.ApplyEffect(dynamicInt.ValueModifier, dynamicInt.Value, dynamicInt.Priority);

                statusInt.Value = statusIntValue.GetValue();
            }
            
            public void GetValue(ref StatusBools statusBool, 
                in NativeParallelMultiHashMap<Hash128, StatusEffects>.Enumerator statusEffects,
                in NativeParallelMultiHashMap<Hash128, DynamicBools>.Enumerator dynamicBools,
                in StatusReferences references)
            {
                UnmanagedEffect effect;

                var statusBoolValue = new StatusBoolValue(statusBool.BaseValue);

                bool effectValue = default;

                foreach (var statusEffect in statusEffects)
                {
                    ref UnmanagedStatusEffectData data = ref references.IdToStatusEffectDataMap.Value[statusEffect.StatusEffectDataId].Value;

                    for (int i = 0; i < data.Effects.Length; i++)
                    {
                        effect = data.Effects[i];

                        if (effect.StatusName != statusBool.StatusName)
                            continue;

                        switch (effect.ValueType)
                        {
                            case ValueType.ExplicitValue:
                                effectValue = effect.BoolValue;
                                break;
                            case ValueType.BaseValue:
                                effectValue = Convert.ToBoolean(data.BaseValue);
                                break;
                            case ValueType.DynamicValue:
                                continue;
                        }

                        statusBoolValue.ApplyEffect(effectValue, effect.Priority);
                    }
                }

                foreach (var dynamicBool in dynamicBools)
                    if (!dynamicBool.PostEvaluate)
                        statusBoolValue.ApplyEffect(dynamicBool.Value, dynamicBool.Priority);

                statusBool.PreEvaluationValue = statusBoolValue.GetValue();
            }
        }
    }
}
#endif