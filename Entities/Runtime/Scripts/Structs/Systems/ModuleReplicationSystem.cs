#if ENTITIES && NETCODE
using Unity.Burst;
using Unity.Collections;
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
    [BurstCompile]
    public partial struct ModuleReplicationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<ModuleReplicationCommand, ReceiveRpcCommandRequest>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var moduleLookup = SystemAPI.GetComponentLookup<Module>();

            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (command, rpc, entity) in SystemAPI.Query<ModuleReplicationCommand, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                commandBuffer.DestroyEntity(entity);

                if (!moduleLookup.TryGetComponent(command.Entity, out Module module))
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