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
        private ulong m_StableTypeHash;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Note that we only need to update this system when changes to the PreEvaluationValue
            // of the coin multiplier has been made. The StatusVariablePostEvaluateUpdate will
            // always be enabled after pre evaluation updates so it can be used to check for changes.

            // If you do not check for StatusVariablePostEvaluateUpdate, you should enable the component 
            // manually if changes to the dynamic floats were made.
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusInts, ExamplePlayerComponent, DynamicFloats<CoinMultiplierToSpeedStruct>>().WithAll<StatusVariablePostEvaluateUpdate, Simulate>().Build();
            m_StableTypeHash = TypeManager.GetTypeInfo<ExamplePlayerComponent>().StableTypeHash;

            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<UnmanagedStatusRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new CoinMultiplierToSpeedJob
            {
                StableTypeHash = m_StableTypeHash,
                Registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>(),
            };
            state.Dependency = job.ScheduleParallel(m_EntityQuery, state.Dependency);
        }
        
        [BurstCompile]
        partial struct CoinMultiplierToSpeedJob : IJobEntity
        {
            public ulong StableTypeHash;
            public UnmanagedStatusRegistry Registry;

            public void Execute(in DynamicBuffer<StatusInts> statusInts, ref ExamplePlayerComponent player, ref DynamicBuffer<DynamicFloats<CoinMultiplierToSpeedStruct>> dynamicFloats)
            {
                if (!player.CoinMultiplier.TryGetElement(StableTypeHash, Registry, statusInts, out var coinMultiplier))
                    return;

                float value = math.max(0, coinMultiplier.PreEvaluationValue - coinMultiplier.BaseValue);
                
                for (int i = 0; i < dynamicFloats.Length; i++)
                {
                    ref var dynamicFloat = ref dynamicFloats.ElementAt(i);
                    dynamicFloat.Value = value;
                }
            }
        }
    }
}