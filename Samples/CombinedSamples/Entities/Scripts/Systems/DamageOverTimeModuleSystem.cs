using StatusEffectsFramework.Entities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
#if Netcode_for_Entities
using Unity.NetCode;
#endif

[assembly: RegisterGenericComponentType(typeof(StatusEffectsFramework.Entities.Modules<StatusEffectsFramework.Samples.DamageOverTimeModuleStruct>))]

namespace StatusEffectsFramework.Samples
{
    public struct DamageOverTimeModuleStruct
    {
        public float IntervalSeconds;
        public int TimesDamaged;
    }

#if Netcode_for_Entities
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
#else
    [UpdateInGroup(typeof(SimulationSystemGroup))]
#endif
    [BurstCompile]
    public partial struct DamageOverTimeModuleSystem : ISystem
    {
#if Netcode_for_Entities
        private EntityQuery m_FirstPredictionTickQuery;
#endif
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
#if Netcode_for_Entities
            m_FirstPredictionTickQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, Modules<DamageOverTimeModuleStruct>>().WithAll<Simulate>().Build();
            m_FirstPredictionTickQuery.AddChangedVersionFilter(ComponentType.ReadWrite<Modules<DamageOverTimeModuleStruct>>());
#endif
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, ExamplePlayerComponent, Modules<DamageOverTimeModuleStruct>>().WithAll<Simulate>().Build();
            
            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<UnmanagedStatusRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
#if Netcode_for_Entities
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();

            if (networkTime.IsFirstPredictionTick)
            {
                var firstPredictionTickJob = new DamageOverTimeModuleFirstPredictionTickJob
                {
                    NetworkTime = networkTime,
                    TickRate = tickRate,
                };
                state.Dependency = firstPredictionTickJob.ScheduleParallelByRef(m_FirstPredictionTickQuery, state.Dependency);
            }
#endif

            var damageOverTimeJob = new DamageOverTimeJob
            {
#if Netcode_for_Entities
                NetworkTime = networkTime,
                TickRate = tickRate,
#else
                ElapsedTime = SystemAPI.Time.ElapsedTime,
#endif
                Registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>(),
            };
            state.Dependency = damageOverTimeJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }
#if Netcode_for_Entities

        [BurstCompile]
        partial struct DamageOverTimeModuleFirstPredictionTickJob : IJobEntity
        {
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;

            public void Execute(in DynamicBuffer<StatusEffects> statusEffects, ref DynamicBuffer<Modules<DamageOverTimeModuleStruct>> damageOverTimeModules)
            {
                for (int i = 0; i < damageOverTimeModules.Length; i++)
                {
                    ref var module = ref damageOverTimeModules.ElementAt(i);

                    if (!StatusEffects.TryGetStatusEffect(statusEffects, module.Id, out var statusEffect))
                        continue;
                    
                    float timeSinceAdded = NetworkTime.ServerTick.TimeSince(statusEffect.TickAdded, NetworkTime.ServerTickFraction, TickRate);
                    module.Struct.TimesDamaged = (int)(timeSinceAdded / module.Struct.IntervalSeconds);
                }
            }
        }
#endif

        [BurstCompile]
        partial struct DamageOverTimeJob : IJobEntity
        {
            public UnmanagedStatusRegistry Registry;
#if Netcode_for_Entities
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;
#else
            public double ElapsedTime;
#endif

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

#if Netcode_for_Entities
                    float timeSinceAdded = NetworkTime.ServerTick.TimeSince(statusEffect.TickAdded, NetworkTime.ServerTickFraction, TickRate);
                    while (timeSinceAdded >= module.Struct.IntervalSeconds * module.Struct.TimesDamaged)
#else
                    while (ElapsedTime >= module.Struct.TimesDamaged * module.Struct.IntervalSeconds + statusEffect.TimeAdded)
#endif
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