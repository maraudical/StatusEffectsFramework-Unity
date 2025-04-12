#if ENTITIES
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace StatusEffects.Entities
{
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(StatusManagerSystem))]
    [UpdateBefore(typeof(EndStatusEffectEntityCommandBufferSystem))]
    [BurstCompile]
    public partial struct StatusVariableUpdateSystem : ISystem
    {
        private EntityQuery m_StatusVariableUpdateQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_StatusVariableUpdateQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusVariableUpdate>().Build();
            state.RequireForUpdate(m_StatusVariableUpdateQuery);
            state.RequireForUpdate<StatusReferences>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var references = SystemAPI.GetSingleton<StatusReferences>();
            var commandBufferParallel = SystemAPI.GetSingleton<EndStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // If any entities were actually changed we update their StatusVariables.
            // This is done at the start of frame before StatusEffects structural changes.
            var statusVariableUpdateJob = new StatusVariableUpdateJob
            {
                CommandBuffer = commandBufferParallel,
                StatusFloatsLookup = SystemAPI.GetBufferLookup<StatusFloats>(),
                StatusIntsLookup = SystemAPI.GetBufferLookup<StatusInts>(),
                StatusBoolsLookup = SystemAPI.GetBufferLookup<StatusBools>(),
                References = references
            };
            state.Dependency = statusVariableUpdateJob.ScheduleByRef(m_StatusVariableUpdateQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusVariableUpdateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [NativeDisableParallelForRestriction]
            public BufferLookup<StatusFloats> StatusFloatsLookup;
            [NativeDisableParallelForRestriction]
            public BufferLookup<StatusInts> StatusIntsLookup;
            [NativeDisableParallelForRestriction]
            public BufferLookup<StatusBools> StatusBoolsLookup;
            [ReadOnly]
            public StatusReferences References;

            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<StatusEffects> statusEffects, in StatusVariableUpdate statusVariableUpdate)
            {
                // We can guarantee that this entity has a StatusEffect buffer
                // because if a StatusEffect was added to an entity that wasn't
                // baked from a StatusManager it would throw an error before this.
                if (StatusFloatsLookup.HasBuffer(entity))
                {
                    var statusFloatBuffer = StatusFloatsLookup[entity];
                    for (int i = 0; i < statusFloatBuffer.Length; i++)
                        GetValue(ref statusFloatBuffer.ElementAt(i), statusEffects, References);
                }

                if (StatusIntsLookup.HasBuffer(entity))
                {
                    var statusIntBuffer = StatusIntsLookup[entity];
                    for (int i = 0; i < statusIntBuffer.Length; i++)
                        GetValue(ref statusIntBuffer.ElementAt(i), statusEffects, References);
                }

                if (StatusBoolsLookup.HasBuffer(entity))
                {
                    var statusBoolBuffer = StatusBoolsLookup[entity];
                    for (int i = 0; i < statusBoolBuffer.Length; i++)
                        GetValue(ref statusBoolBuffer.ElementAt(i), statusEffects, References);
                }

                CommandBuffer.SetComponentEnabled<StatusVariableUpdate>(sortKey, entity, false);
            }

            // Copied from regular StatusFloat.GetValue() with burstable types and math.
            public void GetValue(ref StatusFloats statusFloat, in DynamicBuffer<StatusEffects> statusEffects, in StatusReferences references)
            {
                Effect effect;

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
                    ref StatusEffectData data = ref references.BlobAsset.Value[statusEffect.Id].Value;

                    for (int i = 0; i < data.Effects.Length; i++)
                    {
                        effect = data.Effects[i];
                        
                        if (effect.Id != statusFloat.Id)
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
            public void GetValue(ref StatusInts statusInt, in DynamicBuffer<StatusEffects> statusEffects, in StatusReferences references)
            {
                Effect effect;

                bool positive = math.sign(statusInt.BaseValue) >= 0;
                int additiveValue = 0;
                int multiplicativeValue = 1;
                int postAdditiveValue = 0;
                int minimumPriority = -1;
                int minimumValue = int.MinValue;
                int maximumPriority = -1;
                int maximumValue = int.MaxValue;
                int overwritePriority = -1;
                int overwriteValue = 0;

                int effectValue;

                foreach (var statusEffect in statusEffects)
                {
                    ref StatusEffectData data = ref references.BlobAsset.Value[statusEffect.Id].Value;

                    for (int i = 0; i < data.Effects.Length; i++)
                    {
                        effect = data.Effects[i];

                        if (effect.Id != statusInt.Id)
                            continue;

                        effectValue = statusEffect.Stacks * (effect.UseBaseValue ? (int)data.BaseValue : effect.IntValue);

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
                    statusInt.Value = math.clamp(overwriteValue, overwritePriority <= minimumPriority ? minimumValue : int.MinValue, overwritePriority <= maximumPriority ? maximumValue : int.MaxValue);
                else if (statusInt.SignProtected)
                    statusInt.Value = math.clamp((statusInt.BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue, math.max(positive ? 0 : int.MinValue, minimumValue), math.min(positive ? int.MaxValue : 0, maximumValue));
                else
                    statusInt.Value = math.clamp((statusInt.BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue, minimumValue, maximumValue);
            }

            // Copied from regular StatusBool.GetValue() with burstable types and math.
            public void GetValue(ref StatusBools statusInt, in DynamicBuffer<StatusEffects> statusEffects, in StatusReferences references)
            {
                Effect effect;

                bool value = statusInt.BaseValue;
                int priority = -1;

                bool effectValue;

                foreach (var statusEffect in statusEffects)
                {
                    ref StatusEffectData data = ref references.BlobAsset.Value[statusEffect.Id].Value;

                    for (int i = 0; i < data.Effects.Length; i++)
                    {
                        effect = data.Effects[i];

                        if (effect.Id != statusInt.Id)
                            continue;

                        effectValue = effect.UseBaseValue ? Convert.ToBoolean(data.BaseValue) : effect.BoolValue;

                        if (priority < effect.Priority)
                        {
                            priority = effect.Priority;
                            value = effectValue;
                        }
                    }
                }

                statusInt.Value = value;
            }
        }
    }
}
#endif