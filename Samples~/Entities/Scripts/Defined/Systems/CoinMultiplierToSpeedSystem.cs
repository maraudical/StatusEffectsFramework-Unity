using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[assembly: RegisterGenericComponentType(typeof(StatusEffectsFramework.Entities.DynamicFloats<StatusEffectsFramework.Entities.Samples.CoinMultiplierToSpeedStruct>))]


namespace StatusEffectsFramework.Entities.Samples
{
    public struct CoinMultiplierToSpeedStruct { }

    [UpdateInGroup(typeof(DynamicEffectPostEvaluateSystemGroup))]
    [BurstCompile]
    public partial struct CoinMultiplierToSpeedSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Note that we only need to update this system when changes to the PreEvaluationValue
            // of the coin multiplier has been made. The StatusVariablePostEvaluateUpdate will
            // always be enabled after pre evaluation updates so it can be used to check for changes.

            // If you do not check for StatusVariablePostEvaluateUpdate, you should enable the component 
            // manually if changes to the dynamic floats were made.
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusInts, ExamplePlayerComponent, DynamicFloats<CoinMultiplierToSpeedStruct>>().WithAll<StatusVariablePostEvaluateUpdate, Simulate>().Build();

            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new CoinMultiplierToSpeedJob().ScheduleParallel(m_EntityQuery, state.Dependency);
        }
        
        [BurstCompile]
        partial struct CoinMultiplierToSpeedJob : IJobEntity
        {
            public void Execute(in DynamicBuffer<StatusInts> statusInts, ref ExamplePlayerComponent player, ref DynamicBuffer<DynamicFloats<CoinMultiplierToSpeedStruct>> dynamicFloats)
            {
                if (!player.CoinMultiplier.TryGetElement(player.ComponentId, statusInts, out var coinMultiplier))
                    return;

                float value = math.max(0, coinMultiplier.PreEvaluationValue - 1);
                for (int i = 0; i < dynamicFloats.Length; i++)
                {
                    ref var dynamicFloat = ref dynamicFloats.ElementAt(i);
                    dynamicFloat.Value = value;
                }
            }
        }
    }
}