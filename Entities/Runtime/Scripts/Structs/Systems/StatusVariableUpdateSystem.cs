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
            var commandBufferParallel = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // If any entities were actually changed we update their StatusVariables.
            // This is done at the start of frame before StatusEffects structural changes.
            new StatusVariableUpdateJob
            {
                CommandBuffer = commandBufferParallel,
                StatusFloatsLookup = SystemAPI.GetBufferLookup<StatusFloats>(),
                StatusIntsLookup = SystemAPI.GetBufferLookup<StatusInts>(),
                StatusBoolsLookup = SystemAPI.GetBufferLookup<StatusBools>(),
                References = references
            }.ScheduleParallel(m_StatusVariableUpdateQuery);
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

            public void Execute([EntityIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<StatusEffects> statusEffects, in StatusVariableUpdate statusVariableUpdate)
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
                        }
                    }
                }

                if (statusFloat.SignProtected)
                    statusFloat.Value = math.clamp((statusFloat.BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue, positive ? 0 : float.NegativeInfinity, positive ? float.PositiveInfinity : 0);
                else
                    statusFloat.Value = (statusFloat.BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue;
            }

            // Copied from regular StatusInt.GetValue() with burstable types and math.
            public void GetValue(ref StatusInts statusInt, in DynamicBuffer<StatusEffects> statusEffects, in StatusReferences references)
            {
                Effect effect;

                bool positive = math.sign(statusInt.BaseValue) >= 0;
                int additiveValue = 0;
                int multiplicativeValue = 1;
                int postAdditiveValue = 0;

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
                        }
                    }
                }

                if (statusInt.SignProtected)
                    statusInt.Value = math.clamp((statusInt.BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue, positive ? 0 : int.MinValue, positive ? int.MaxValue : 0);
                else
                    statusInt.Value = (statusInt.BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue;
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