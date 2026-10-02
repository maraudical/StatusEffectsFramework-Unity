using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Applies post evaluated effects on top of each status variable's pre evaluation value to get
    /// its final value. Runs last, after the <see cref="DynamicEffectPostEvaluateSystemGroup"/>.
    /// </summary>
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
        private StatusTypeDependencies m_Dependencies;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusFloats, StatusInts, StatusBools>().WithAll<StatusVariablePostEvaluateUpdate, Simulate>().Build();
            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<UnmanagedStatusRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>();
            m_Dependencies.Register(ref state, registry.Version, ref registry.DynamicEffectTypes, isReadOnly: true);

            var statusVariablePostEvaluateJob = new StatusVariablePostEvaluateJob
            {
                Registry = registry,
                StatusEffectsHandle = SystemAPI.GetBufferTypeHandle<StatusEffects>(true),
                StatusFloatsHandle = SystemAPI.GetBufferTypeHandle<StatusFloats>(),
                StatusIntsHandle = SystemAPI.GetBufferTypeHandle<StatusInts>(),
                StatusBoolsHandle = SystemAPI.GetBufferTypeHandle<StatusBools>(),
                StatusVariablePostEvaluateUpdateHandle = SystemAPI.GetComponentTypeHandle<StatusVariablePostEvaluateUpdate>(),
            };
            state.Dependency = statusVariablePostEvaluateJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusVariablePostEvaluateJob : IJobChunk
        {
            public UnmanagedStatusRegistry Registry;
            [ReadOnly]
            public BufferTypeHandle<StatusEffects> StatusEffectsHandle;
            public BufferTypeHandle<StatusFloats> StatusFloatsHandle;
            public BufferTypeHandle<StatusInts> StatusIntsHandle;
            public BufferTypeHandle<StatusBools> StatusBoolsHandle;
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
                    var statusEffects = statusEffectsAccessor[i];
                    var statusFloats = statusFloatsAccessor[i];
                    var statusInts = statusIntsAccessor[i];
                    var statusBools = statusBoolsAccessor[i];

                    effectCollector.BeginEntity(statusEffects);

                    foreach (var statusEffect in statusEffects)
                    {
                        ref var data = ref Registry.GetStatusEffectDataOrNullRefDebug(statusEffect.Id, out bool exists);
                        if (!exists)
                            continue;

                        for (int v = 0; v < data.Effects.Length; v++)
                        {
                            ref var effect = ref data.Effects[v];

                            if (effect.ValueSource == ValueSource.DynamicValue)
                                effectCollector.AddDynamic(chunk, i, ref effect, postEvaluate: true);
                        }
                    }

                    for (int v = 0; v < statusFloats.Length; v++)
                    {
                        ref var statusFloat = ref statusFloats.ElementAt(v);
                        statusFloat.PostEvaluationValue = effectCollector.Evaluate(statusFloat.Id, statusFloat.PreEvaluationValue, statusFloat.SignProtected);
                    }

                    for (int v = 0; v < statusInts.Length; v++)
                    {
                        ref var statusInt = ref statusInts.ElementAt(v);
                        statusInt.PostEvaluationValue = effectCollector.Evaluate(statusInt.Id, statusInt.PreEvaluationValue, statusInt.SignProtected);
                    }

                    for (int v = 0; v < statusBools.Length; v++)
                    {
                        ref var statusBool = ref statusBools.ElementAt(v);
                        statusBool.PostEvaluationValue = effectCollector.Evaluate(statusBool.Id, statusBool.PreEvaluationValue);
                    }
                }

                chunk.SetComponentEnabledForAll(ref StatusVariablePostEvaluateUpdateHandle, false);
            }
        }
    }
}
