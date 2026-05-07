using StatusEffectFramework.Entities;
using StatusEffectFramework.Entities.Samples;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace StatusEffectsFramework.Entities.Samples
{
    public struct InverseHealthToSpeedComponent : IComponentData
    {
        public float Value;
    }

    [UpdateInGroup(typeof(DynamicEffectPreEvaluateSystemGroup))]
    [BurstCompile]
    public partial struct InverseHealthToSpeedSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        private TypeIndex m_TypeIndex;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusFloats, ExamplePlayerComponent, DynamicFloats>().WithAll<InverseHealthToSpeedComponent, Simulate>(). WithPresentRW<StatusVariablePreEvaluateUpdate>().Build();

            m_TypeIndex = TypeManager.GetTypeIndex<InverseHealthToSpeedComponent>();

            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var inverseHealthToSpeedJob = new InverseHealthToSpeedJob
            {
                TypeIndex = m_TypeIndex,
            };
            state.Dependency = inverseHealthToSpeedJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile]
        partial struct InverseHealthToSpeedJob : IJobEntity
        {
            public TypeIndex TypeIndex;

            public void Execute(EnabledRefRW<StatusVariablePreEvaluateUpdate> preEvaluateUpdate, in DynamicBuffer<StatusFloats> statusFloats, ref ExamplePlayerComponent player, ref DynamicBuffer<DynamicFloats> dynamicFloats)
            {
                /*if (player.MaxHealth.TryGetValue(player.ComponentId, statusFloats, out var maxHealth))
                {
                    float value = math.max(0, 1f - player.Health / maxHealth) * maxHealth * ConversionRatio;
                    for (int i = 0; i < dynamicFloats.Length; i++)
                    {
                        ref var dynamicFloat = ref dynamicFloats.ElementAt(i);

                        if (!dynamicFloat.PostEvaluate && dynamicFloat.TypeIndex == TypeIndex)
                            dynamicFloat.Value = value;
                    }
                }*/
            }
        }
    }
}