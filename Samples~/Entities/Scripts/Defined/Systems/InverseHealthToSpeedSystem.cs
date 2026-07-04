using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[assembly: RegisterGenericComponentType(typeof(StatusEffectsFramework.Entities.DynamicFloats<StatusEffectsFramework.Entities.Samples.InverseHealthToSpeedStruct>))]

namespace StatusEffectsFramework.Entities.Samples
{
    public struct InverseHealthToSpeedStruct
    {
        public float ConversionRatio;
    }

    [UpdateInGroup(typeof(DynamicEffectPreEvaluateSystemGroup))]
    [BurstCompile]
    public partial struct InverseHealthToSpeedSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusFloats, ExamplePlayerComponent, DynamicFloats<InverseHealthToSpeedStruct>>().WithAll<Simulate>().WithPresentRW<StatusVariablePreEvaluateUpdate>().Build();

            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new InverseHealthToSpeedJob().ScheduleParallel(m_EntityQuery, state.Dependency);
        }

        [BurstCompile]
        partial struct InverseHealthToSpeedJob : IJobEntity
        {
            public void Execute(EnabledRefRW<StatusVariablePreEvaluateUpdate> preEvaluateUpdate, 
                in DynamicBuffer<StatusFloats> statusFloats, 
                ref ExamplePlayerComponent player, 
                ref DynamicBuffer<DynamicFloats<InverseHealthToSpeedStruct>> dynamicFloats)
            {
                bool update = false;

                if (player.MaxHealth.TryGetValue(player.ComponentId, statusFloats, out var maxHealth))
                {
                    float value = math.max(0, 1f - player.Health / maxHealth) * maxHealth;
                    for (int i = 0; i < dynamicFloats.Length; i++)
                    {
                        ref var dynamicFloat = ref dynamicFloats.ElementAt(i);

                        var convertedValue = value * dynamicFloat.Struct.ConversionRatio;
                        if (convertedValue != dynamicFloat.Value)
                        {
                            dynamicFloat.Value = convertedValue;
                            update = true;
                        }
                    }
                }

                if (update)
                    preEvaluateUpdate.ValueRW = true;
            }
        }
    }
}