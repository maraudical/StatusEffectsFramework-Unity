using StatusEffectFramework.Entities;
using StatusEffectFramework.Entities.Samples;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace StatusEffectsFramework.Entities.Samples
{
    public struct CoinMultiplierToSpeedComponent : IComponentData
    {
        public float Value;
    }

    [UpdateInGroup(typeof(DynamicEffectPostEvaluateSystemGroup))]
    [BurstCompile]
    public partial struct CoinMultiplierToSpeedSystem : ISystem
    {
        private EntityQuery m_EntityQuery;
        
        private TypeIndex m_TypeIndex;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Note that we only need to update this system when changes to the PreEvaluationValue
            // of the coin multiplier has been made. The StatusVariablePostEvaluateUpdate will
            // always be enabled after pre evaluation updates so it can be used to check for changes.

            // If you do not check for StatusVariablePostEvaluateUpdate, you should enable the component 
            // if changes to the dynamic floats were made.
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusInts, ExamplePlayerComponent, DynamicFloats>().WithAll<StatusVariablePostEvaluateUpdate, CoinMultiplierToSpeedComponent, Simulate>().Build();

            m_TypeIndex = TypeManager.GetTypeIndex<CoinMultiplierToSpeedComponent>();

            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var coinMultiplierToSpeedJob = new CoinMultiplierToSpeedJob
            {
                TypeIndex = m_TypeIndex,
            };
            state.Dependency = coinMultiplierToSpeedJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }
        
        [BurstCompile]
        partial struct CoinMultiplierToSpeedJob : IJobEntity
        {
            public TypeIndex TypeIndex;

            public void Execute(in DynamicBuffer<StatusInts> statusInts, ref ExamplePlayerComponent player, ref DynamicBuffer<DynamicFloats> dynamicFloats)
            {
                if (player.CoinMultiplier.TryGetElement(player.ComponentId, statusInts, out var coinMultiplier))
                {
                    float value = math.max(0, coinMultiplier.PreEvaluationValue - 1);
                    for (int i = 0; i < dynamicFloats.Length; i++)
                    {
                        ref var dynamicFloat = ref dynamicFloats.ElementAt(i);

                        if (dynamicFloat.PostEvaluate && dynamicFloat.TypeIndex == TypeIndex)
                            dynamicFloat.Value = value;
                    }
                }
            }
        }
    }
}