#if ENTITIES && ADDRESSABLES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static StatusEffects.Modules.HealModule;

namespace StatusEffects.NetCode.Entities.Example
{
#if NETCODE_ENTITIES
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
#endif
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    // Special case where on the same frame we are dealing damage over time
    // and healing. In this example the heal comes after the damage over time.
    [UpdateAfter(typeof(StatusEffectDamageOverTimeSystem))]
    public partial struct StatusEffectHealSystem : ISystem
    {
        private ComponentLookup<ExamplePlayer> m_PlayerLookup;
        private BufferLookup<StatusFloats> m_StatusFloatLookup;

        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_PlayerLookup = state.GetComponentLookup<ExamplePlayer>();
            m_StatusFloatLookup = state.GetBufferLookup<StatusFloats>();

            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<HealEntityModule, ModuleUpdateTag>();
            m_EntityQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (m_EntityQuery.IsEmpty)
                return;
            
            m_PlayerLookup.Update(ref state);
            m_StatusFloatLookup.Update(ref state);

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var commandBufferParallel = commandBuffer.AsParallelWriter();
            
            new HealJob
            {
                CommandBuffer = commandBufferParallel,
                PlayerLookup = m_PlayerLookup,
                StatusFloatLookup = m_StatusFloatLookup
            }.Schedule();

            state.Dependency.Complete();

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct HealJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly]
            public ComponentLookup<ExamplePlayer> PlayerLookup;
            [ReadOnly]
            public BufferLookup<StatusFloats> StatusFloatLookup;

            public void Execute([EntityIndexInQuery] int sortKey, Entity entity, in HealEntityModule healModule, in Module module, in ModuleUpdateTag update)
            {
                Entity parentEntity = module.Parent;
                
                if (PlayerLookup.TryGetComponent(parentEntity, out ExamplePlayer player))
                {
                    if (module.IsBeingDestroyed)
                    {
                        player.Health = math.min(player.Health, GetStatusFloat(parentEntity, player.ComponentId, player.MaxHealth));
                    }
                    else
                    {
                        player.Health += module.BaseValue * math.max(0, module.Stacks - module.PreviousStacks);
                        player.Health = math.min(player.Health, GetStatusFloat(parentEntity, player.ComponentId, player.MaxHealth));
                    }
                    CommandBuffer.SetComponent(sortKey, parentEntity, player);
                }
            }

            private float GetStatusFloat(in Entity entity, in FixedString64Bytes componentId, in FixedString64Bytes id)
            {
                var buffer = StatusFloatLookup[entity];
                for (int i = 0; i < buffer.Length; i++)
                {
                    var statusFloat = buffer[i];
                    if (statusFloat.ComponentId == componentId && statusFloat.Id == id)
                    {
                        return statusFloat.Value;
                    }
                }
                return 0;
            }
        }
    }
}
#endif