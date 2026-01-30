#if ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static StatusEffects.Modules.HealModule;

namespace StatusEffects.Entities.Example
{
    public struct HealCleanupComponent : ICleanupComponentData { }

    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    // Special case where on the same frame we are dealing damage over time
    // and healing. In this example the heal comes after the damage over time.
    [UpdateAfter(typeof(StatusEffectDamageOverTimeSystem))]
    public partial struct StatusEffectHealUpdateSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<HealEntityModule, Module>().Build();
            m_EntityQuery.AddChangedVersionFilter(ComponentType.ReadOnly<Module>());
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var healUpdateJob = new HealUpdateJob
            {
                PlayerLookup = SystemAPI.GetComponentLookup<ExamplePlayer>(),
                StatusFloatsLookup = SystemAPI.GetBufferLookup<StatusFloats>(true),
            };
            state.Dependency = healUpdateJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct HealDestroyJob : IJobEntity
        {
            public ComponentLookup<ExamplePlayer> PlayerLookup;
            [ReadOnly]
            public BufferLookup<StatusFloats> StatusFloatsLookup;

            public void Execute([ChunkIndexInQuery] int sortKey, Module module)
            {
                Entity targetEntity = module.Target;

                if (PlayerLookup.TryGetRefRW(targetEntity, out var playerRW))
                {
                    ref var player = ref playerRW.ValueRW;
                    var buffer = StatusFloatsLookup[targetEntity];
                    player.MaxHealth.GetValue(player.ComponentId, buffer, out var maxHealth);
                    player.Health = math.min(player.Health, maxHealth);
                }
            }
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct HealUpdateJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<ExamplePlayer> PlayerLookup;
            [ReadOnly]
            public BufferLookup<StatusFloats> StatusFloatsLookup;

            public void Execute(in Module module)
            {
                Entity targetEntity = module.Target;

                if (PlayerLookup.TryGetRefRW(targetEntity, out var playerRW))
                {
                    ref var player = ref playerRW.ValueRW;
                    var buffer = StatusFloatsLookup[targetEntity];
                    player.MaxHealth.GetValue(player.ComponentId, buffer, out var maxHealth);
                    player.Health += module.BaseValue * math.max(0, module.Stacks - module.PreviousStacks);
                    player.Health = math.min(player.Health, maxHealth);
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    // Special case where on the same frame we are dealing damage over time
    // and healing. In this example the heal comes after the damage over time.
    [UpdateAfter(typeof(StatusEffectDamageOverTimeSystem))]
    public partial struct StatusEffectHealDestroySystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<HealCleanupComponent, ModuleCleanupComponent>().WithNone<Module>().Build();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var healDestroyJob = new HealDestroyJob
            {
                PlayerLookup = SystemAPI.GetComponentLookup<ExamplePlayer>(),
                StatusFloatsLookup = SystemAPI.GetBufferLookup<StatusFloats>(true),
            };
            state.Dependency = healDestroyJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct HealDestroyJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<ExamplePlayer> PlayerLookup;
            [ReadOnly]
            public BufferLookup<StatusFloats> StatusFloatsLookup;

            public void Execute(in ModuleCleanupComponent module)
            {
                Entity targetEntity = module.Target;

                if (PlayerLookup.TryGetRefRW(targetEntity, out var playerRW))
                {
                    ref var player = ref playerRW.ValueRW;
                    var buffer = StatusFloatsLookup[targetEntity];
                    player.MaxHealth.GetValue(player.ComponentId, buffer, out var maxHealth);
                    player.Health = math.min(player.Health, maxHealth);
                }
            }
        }
    }
}
#endif