using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffectFramework.Entities
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
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, DynamicFloats, DynamicInts, DynamicBools, StatusFloats, StatusInts, StatusBools>().WithAll<StatusVariablePostEvaluateUpdate, Simulate>().Build();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new StatusVariablePostEvaluateJob().ScheduleParallel(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusVariablePostEvaluateJob : IJobEntity
        {
            public void Execute(Entity entity,
                in DynamicBuffer<StatusEffects> statusEffects,
                in DynamicBuffer<DynamicFloats> dynamicFloats,
                in DynamicBuffer<DynamicInts> dynamicInts,
                in DynamicBuffer<DynamicBools> dynamicBools,
                ref DynamicBuffer<StatusFloats> statusFloats,
                ref DynamicBuffer<StatusInts> statusInts,
                ref DynamicBuffer<StatusBools> statusBools,
                EnabledRefRW<StatusVariablePostEvaluateUpdate> postEvaluateUpdate)
            {
                postEvaluateUpdate.ValueRW = false;
                
                using var idToStatusEffect = new NativeHashMap<uint, StatusEffects>(statusEffects.Length, Allocator.Temp);
                using var statusNameToDynamicFloat = new NativeParallelMultiHashMap<Hash128, (DynamicFloats, int)>(statusEffects.Length, Allocator.Temp);
                using var statusNameToDynamicInt = new NativeParallelMultiHashMap<Hash128, (DynamicInts, int)>(statusEffects.Length, Allocator.Temp);
                using var statusNameToDynamicBool = new NativeParallelMultiHashMap<Hash128, (DynamicBools, int)>(statusEffects.Length, Allocator.Temp);
                // Map ids to status effects to quickly find all status effects affecting a specific status variable.
                foreach (var statusEffect in statusEffects)
                {
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
                    GetValue(ref statusFloat, statusNameToDynamicFloat.GetValuesForKey(statusFloat.StatusName));
                }

                for (int i = 0; i < statusInts.Length; i++)
                {
                    ref var statusInt = ref statusInts.ElementAt(i);
                    GetValue(ref statusInt, statusNameToDynamicInt.GetValuesForKey(statusInt.StatusName));
                }

                for (int i = 0; i < statusBools.Length; i++)
                {
                    ref var statusBool = ref statusBools.ElementAt(i);
                    GetValue(ref statusBool, statusNameToDynamicBool.GetValuesForKey(statusBool.StatusName));
                }
            }

            public void GetValue(ref StatusFloats statusFloat,
                in NativeParallelMultiHashMap<Hash128, (DynamicFloats DynamicFloat, int Stacks)>.Enumerator dynamicFloats)
            {
                var statusFloatValue = new StatusFloatValue(statusFloat.PreEvaluationValue, statusFloat.SignProtected);

                foreach (var dynamicFloat in dynamicFloats)
                    if (dynamicFloat.DynamicFloat.PostEvaluate)
                        statusFloatValue.ApplyEffect(dynamicFloat.DynamicFloat.ValueModifier, dynamicFloat.Stacks * dynamicFloat.DynamicFloat.Value, dynamicFloat.DynamicFloat.Priority);

                statusFloat.PostEvaluationValue = statusFloatValue.GetValue();
            }

            public void GetValue(ref StatusInts statusInt,
                in NativeParallelMultiHashMap<Hash128, (DynamicInts DynamicInt, int Stacks)>.Enumerator dynamicInts)
            {
                var statusIntValue = new StatusIntValue(statusInt.PreEvaluationValue, statusInt.SignProtected);

                foreach (var dynamicInt in dynamicInts)
                    if (dynamicInt.DynamicInt.PostEvaluate)
                        statusIntValue.ApplyEffect(dynamicInt.DynamicInt.ValueModifier, dynamicInt.Stacks * dynamicInt.DynamicInt.Value, dynamicInt.DynamicInt.Priority);

                statusInt.PostEvaluationValue = statusIntValue.GetValue();
            }

            public void GetValue(ref StatusBools statusBool,
                in NativeParallelMultiHashMap<Hash128, (DynamicBools DynamicBool, int Stacks)>.Enumerator dynamicBools)
            {
                var statusBoolValue = new StatusBoolValue(statusBool.PreEvaluationValue);

                foreach (var dynamicBool in dynamicBools)
                    if (dynamicBool.DynamicBool.PostEvaluate)
                        statusBoolValue.ApplyEffect(dynamicBool.DynamicBool.Value, dynamicBool.DynamicBool.Priority);

                statusBool.PostEvaluationValue = statusBoolValue.GetValue();
            }
        }
    }
}
