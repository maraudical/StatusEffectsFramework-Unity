#if ENTITIES && NETCODE
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    public struct ModuleReplicationCommand : IRpcCommand 
    {
        public Entity Entity;
        public int Stacks;
        public int PreviousStacks;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndStatusEffectEntityCommandBufferSystem))]
    [BurstCompile]
    public partial struct ModuleReplicationSystem : ISystem
    {
        private EntityQuery m_ModuleReplicationQuery;

        private ComponentLookup<Module> m_ModulesLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModuleReplicationQuery = SystemAPI.QueryBuilder().WithAll<ModuleReplicationCommand, ReceiveRpcCommandRequest>().Build();

            m_ModulesLookup = SystemAPI.GetComponentLookup<Module>();

            state.RequireForUpdate(m_ModuleReplicationQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_ModulesLookup.Update(ref state);

            var commandBuffer = SystemAPI.GetSingletonRW<EndStatusEffectEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (command, rpc, entity) in SystemAPI.Query<ModuleReplicationCommand, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                commandBuffer.DestroyEntity(entity);

                if (!m_ModulesLookup.TryGetComponent(command.Entity, out Module module))
                    continue;
                
                module.IsBeingUpdated = true;
                module.IsReplicated = true;
                module.ReplicatedStacks = command.Stacks;
                module.ReplicatedPreviousStacks = command.PreviousStacks;
                commandBuffer.SetComponent(command.Entity, module);
                commandBuffer.SetComponentEnabled<ModuleUpdateTag>(command.Entity, true);
            }
        }
    }
}
#endif