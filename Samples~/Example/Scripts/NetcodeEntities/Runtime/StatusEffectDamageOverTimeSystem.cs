#if ENTITIES && ADDRESSABLES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using static StatusEffects.Modules.DamageOverTimeModule;

namespace StatusEffects.NetCode.Entities.Example
{
#if NETCODE_ENTITIES
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
#endif
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct StatusEffectDamageOverTimeSystem : ISystem
    {
        private ComponentLookup<ExamplePlayer> m_PlayerLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_PlayerLookup = state.GetComponentLookup<ExamplePlayer>();

            state.RequireForUpdate<DamageOverTimeEntityModule>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_PlayerLookup.Update(ref state);

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var commandBufferParallel = commandBuffer.AsParallelWriter();
            
            new DamageOverTimeJob
            {
                CommandBuffer = commandBufferParallel,
                PlayerLookup = m_PlayerLookup,
                TimeDelta = SystemAPI.Time.DeltaTime
            }.Schedule();

            state.Dependency.Complete();

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct DamageOverTimeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly]
            public ComponentLookup<ExamplePlayer> PlayerLookup;
            public float TimeDelta;

            public void Execute([EntityIndexInQuery] int sortKey, ref DamageOverTimeEntityModule damageOverTime, in Module module)
            {
                Entity entity = module.Parent;
                
                if (PlayerLookup.TryGetComponent(entity, out ExamplePlayer player))
                {
                    damageOverTime.CurrentSeconds -= TimeDelta;
                    if (damageOverTime.CurrentSeconds <= 0)
                    {
                        damageOverTime.CurrentSeconds = damageOverTime.InvervalSeconds;
                        player.Health -= module.BaseValue * module.Stacks;
                        CommandBuffer.SetComponent(sortKey, entity, player);
                    }
                }
            }
        }
    }
}
#endif