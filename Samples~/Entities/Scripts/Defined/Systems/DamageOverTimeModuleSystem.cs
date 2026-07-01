using StatusEffectFramework.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[assembly: RegisterGenericComponentType(typeof(Modules<StatusEffectFramework.Entities.Samples.DamageOverTimeModuleStruct>))]

namespace StatusEffectFramework.Entities.Samples
{
    public struct DamageOverTimeModuleStruct
    {
        public float IntervalSeconds;
        public int TimesDamaged;
    }

#if NETCODE
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
#else
    [UpdateInGroup(typeof(SimulationSystemGroup))]
#endif
    [BurstCompile]
    public partial struct DamageOverTimeModuleSystem : ISystem
    {
#if NETCODE
        private EntityQuery m_FirstPredictionTickQuery;
#endif
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
#if NETCODE
            m_FirstPredictionTickQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, Modules<DamageOverTimeModuleStruct>>().WithAll<Simulate>().Build();
            m_FirstPredictionTickQuery.AddChangedVersionFilter(ComponentType.ReadWrite<Modules<DamageOverTimeModuleStruct>>());
#endif
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, ExamplePlayerComponent, Modules<DamageOverTimeModuleStruct>>().WithAll<Simulate>().Build();
            
            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<StatusReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusReferences = SystemAPI.GetSingleton<StatusReferences>();
            var commandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var lookup = SystemAPI.GetBufferLookup<Modules<DamageOverTimeModuleStruct>>();
            var playerLookup = SystemAPI.GetComponentLookup<ExamplePlayerComponent>();
            var statusFloatsLookup = SystemAPI.GetBufferLookup<StatusFloats>();
#if NETCODE
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();

            if (networkTime.IsFirstPredictionTick)
            {
                var firstPredictionTickJob = new DamageOverTimeModuleFirstPredictionTickJob
                {
                    NetworkTime = networkTime,
                    TickRate = tickRate,
                    References = statusReferences,
                };
                state.Dependency = firstPredictionTickJob.ScheduleParallelByRef(m_FirstPredictionTickQuery, state.Dependency);
            }
#endif

            var damageOverTimeJob = new DamageOverTimeJob
            {
#if NETCODE
                NetworkTime = networkTime,
                TickRate = tickRate,
#else
                ElapsedTime = SystemAPI.Time.ElapsedTime,
#endif
                References = statusReferences,
            };
            state.Dependency = damageOverTimeJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }
#if NETCODE

        [BurstCompile]
        partial struct DamageOverTimeModuleFirstPredictionTickJob : IJobEntity
        {
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;
            public StatusReferences References;

            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<StatusEffects> statusEffects, ref DynamicBuffer<Modules<DamageOverTimeModuleStruct>> damageOverTimeModules)
            {
                for (int i = 0; i < damageOverTimeModules.Length; i++)
                {
                    ref var module = ref damageOverTimeModules.ElementAt(i);

                    if (!StatusEffectsECSUtility.TryGetStatusEffect(statusEffects, module.Id, out var statusEffect))
                        continue;
                    
                    float timeSinceAdded = NetworkTime.ServerTick.TimeSince(statusEffect.TickAdded, NetworkTime.ServerTickFraction, TickRate);
                    module.Value.TimesDamaged = (int)(timeSinceAdded / module.Value.IntervalSeconds);
                }
            }
        }
#endif

        [BurstCompile]
        partial struct DamageOverTimeJob : IJobEntity
        {
            public StatusReferences References;
#if NETCODE
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

                    if (!StatusEffectsECSUtility.TryGetStatusEffect(statusEffects, module.Id, out statusEffect))
                        continue;

                    if (!References.TryGetReference(statusEffect.StatusEffectDataId, out var reference))
                        continue;

                    ref var data = ref reference.Value;

#if NETCODE
                    float timeSinceAdded = NetworkTime.ServerTick.TimeSince(statusEffect.TickAdded, NetworkTime.ServerTickFraction, TickRate);
                    while (timeSinceAdded >= module.Value.IntervalSeconds * module.Value.TimesDamaged)
#else
                    while (Time >= module.Value.TimesDamaged * module.Value.IntervalSeconds + statusEffect.TimeAdded)
#endif
                    {
                        module.Value.TimesDamaged++;
                        player.Health -= data.BaseValue * statusEffect.Stacks;
                    }
                }

                player.Health = math.max(player.Health, 0);
            }
        }
    }
}