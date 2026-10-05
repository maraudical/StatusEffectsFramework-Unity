using StatusEffectsFramework.Entities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[assembly: RegisterGenericComponentType(typeof(StatusEffectsFramework.Entities.Modules<StatusEffectsFramework.Samples.DamageOverTimeModuleStruct>))]

namespace StatusEffectsFramework.Samples
{
    public struct DamageOverTimeModuleStruct
    {
        public float IntervalSeconds;
        public int TimesDamaged;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct DamageOverTimeModuleSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, ExamplePlayerComponent, Modules<DamageOverTimeModuleStruct>>().WithAll<Simulate>().Build();
            
            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<UnmanagedStatusRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            var damageOverTimeJob = new DamageOverTimeJob
            {
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                Registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>(),
            };
            state.Dependency = damageOverTimeJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile]
        partial struct DamageOverTimeJob : IJobEntity
        {
            public UnmanagedStatusRegistry Registry;
            public double ElapsedTime;

            public void Execute(in DynamicBuffer<StatusEffects> statusEffects,
                ref ExamplePlayerComponent player,
                ref DynamicBuffer<Modules<DamageOverTimeModuleStruct>> modules)
            {
                StatusEffects statusEffect;
                
                for (int i = 0; i < modules.Length; i++)
                {
                    ref var module = ref modules.ElementAt(i);

                    if (!StatusEffects.TryGetStatusEffect(statusEffects, module.Id, out statusEffect))
                        continue;
                    
                    ref var data = ref Registry.GetStatusEffectDataOrNullRef(statusEffect.Id, out bool exists);
                    if (!exists)
                        continue;

                    while (Time >= module.Struct.TimesDamaged * module.Struct.IntervalSeconds + statusEffect.TimeAdded)
                    {
                        module.Struct.TimesDamaged++;
                        player.Health -= data.BaseValue * statusEffect.Stacks;
                    }
                }

                player.Health = math.max(player.Health, 0);
            }
        }
    }
}