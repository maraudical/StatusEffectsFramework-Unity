using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Calculates each status variable's pre evaluation value from its base value and the active
    /// effects that aren't post evaluated. Runs after the end status effect command buffer system and
    /// the <see cref="DynamicEffectPreEvaluateSystemGroup"/>.
    /// </summary>
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
        private StatusTypeDependencies m_Dependencies;

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
            var registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>();
            m_Dependencies.Register(ref state, registry.Version, ref registry.DynamicEffectTypes, isReadOnly: true);

            var statusVariablePreEvaluateJob = new StatusVariablePreEvaluateJob
            {
                Registry = registry,
                StatusEffectsHandle = SystemAPI.GetBufferTypeHandle<StatusEffects>(true),
                StatusFloatsHandle = SystemAPI.GetBufferTypeHandle<StatusFloats>(),
                StatusIntsHandle = SystemAPI.GetBufferTypeHandle<StatusInts>(),
                StatusBoolsHandle = SystemAPI.GetBufferTypeHandle<StatusBools>(),
                StatusVariablePreEvaluateUpdateHandle = SystemAPI.GetComponentTypeHandle<StatusVariablePreEvaluateUpdate>(),
                StatusVariablePostEvaluateUpdateHandle = SystemAPI.GetComponentTypeHandle<StatusVariablePostEvaluateUpdate>(),
            };
            state.Dependency = statusVariablePreEvaluateJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusVariablePreEvaluateJob : IJobChunk
        {
            public UnmanagedStatusRegistry Registry;
            [ReadOnly]
            public BufferTypeHandle<StatusEffects> StatusEffectsHandle;
            public BufferTypeHandle<StatusFloats> StatusFloatsHandle;
            public BufferTypeHandle<StatusInts> StatusIntsHandle;
            public BufferTypeHandle<StatusBools> StatusBoolsHandle;
            public ComponentTypeHandle<StatusVariablePreEvaluateUpdate> StatusVariablePreEvaluateUpdateHandle;
            public ComponentTypeHandle<StatusVariablePostEvaluateUpdate> StatusVariablePostEvaluateUpdateHandle;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BufferAccessor<StatusEffects> statusEffectsAccessor = chunk.GetBufferAccessorRO(ref StatusEffectsHandle);
                BufferAccessor<StatusFloats> statusFloatsAccessor = chunk.GetBufferAccessorRW(ref StatusFloatsHandle);
                BufferAccessor<StatusInts> statusIntsAccessor = chunk.GetBufferAccessorRW(ref StatusIntsHandle);
                BufferAccessor<StatusBools> statusBoolsAccessor = chunk.GetBufferAccessorRW(ref StatusBoolsHandle);

                using var effectCollector = new EffectCollector(UnmanagedStatusRegistry.CollectionsInitialCapacity, Allocator.Temp);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    chunk.SetComponentEnabled(ref StatusVariablePostEvaluateUpdateHandle, i, true);

                    var statusEffects = statusEffectsAccessor[i];
                    var statusFloats = statusFloatsAccessor[i];
                    var statusInts = statusIntsAccessor[i];
                    var statusBools = statusBoolsAccessor[i];

                    effectCollector.BeginEntity(statusEffects);

                    // The status effect's index is its order, which decides ties between effects of equal priority.
                    for (int order = 0; order < statusEffects.Length; order++)
                    {
                        var statusEffect = statusEffects[order];

                        ref var data = ref Registry.GetStatusEffectDataOrNullRefDebug(statusEffect.Id, out bool exists);
                        if (!exists)
                            continue;

                        for (int v = 0; v < data.Effects.Length; v++)
                        {
                            ref var effect = ref data.Effects[v];

                            if (effect.ValueSource == ValueSource.DynamicValue)
                                effectCollector.AddDynamic(chunk, i, ref effect, postEvaluate: false);
                            else
                                effectCollector.AddStatic(ref effect, statusEffect.Stacks, order, data.BaseValue);
                        }
                    }

                    for (int v = 0; v < statusFloats.Length; v++)
                    {
                        ref var statusFloat = ref statusFloats.ElementAt(v);
                        statusFloat.PreEvaluationValue = effectCollector.Evaluate(statusFloat.Id, statusFloat.BaseValue, statusFloat.SignProtected);
                    }

                    for (int v = 0; v < statusInts.Length; v++)
                    {
                        ref var statusInt = ref statusInts.ElementAt(v);
                        statusInt.PreEvaluationValue = effectCollector.Evaluate(statusInt.Id, statusInt.BaseValue, statusInt.SignProtected);
                    }

                    for (int v = 0; v < statusBools.Length; v++)
                    {
                        ref var statusBool = ref statusBools.ElementAt(v);
                        statusBool.PreEvaluationValue = effectCollector.Evaluate(statusBool.Id, statusBool.BaseValue);
                    }
                }

                chunk.SetComponentEnabledForAll(ref StatusVariablePreEvaluateUpdateHandle, false);
            }
        }
    }
}
