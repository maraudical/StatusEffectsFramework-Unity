#if ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static StatusEffects.Modules.DamageOverTimeModule;

namespace StatusEffects.Entities.Example
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct StatusEffectDamageOverTimeSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<DamageOverTimeEntityModule, Module>().Build();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var damageOverTimeJob = new DamageOverTimeJob
            {
                TimeDelta = SystemAPI.Time.DeltaTime,
                PlayerLookup = SystemAPI.GetComponentLookup<ExamplePlayer>()
            };
            state.Dependency = damageOverTimeJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct DamageOverTimeJob : IJobEntity
        {
            public float TimeDelta;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<ExamplePlayer> PlayerLookup;

            public void Execute([ChunkIndexInQuery] int sortKey, ref DamageOverTimeEntityModule damageOverTime, in Module module)
            {
                Entity targetEntity = module.Target;

                if (PlayerLookup.TryGetRefRW(targetEntity, out var playerRW))
                {
                    ref var player = ref playerRW.ValueRW;
                    damageOverTime.CurrentSeconds -= TimeDelta;
                    while (damageOverTime.CurrentSeconds <= 0)
                    {
                        damageOverTime.CurrentSeconds += damageOverTime.InvervalSeconds;
                        player.Health -= module.BaseValue * module.Stacks;
                        player.Health = math.max(player.Health, 0);
                    }
                }
            }
        }
    }
}
#endif