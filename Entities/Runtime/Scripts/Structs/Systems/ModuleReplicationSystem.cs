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
        private ComponentLookup<Module> m_ModuleLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModuleLookup = state.GetComponentLookup<Module>();

            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<ModuleReplicationCommand, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_ModuleLookup.Update(ref state);

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (command, rpc, entity) in SystemAPI.Query<ModuleReplicationCommand, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                commandBuffer.DestroyEntity(entity);

                if (!m_ModuleLookup.EntityExists(command.Entity))
                    continue;

                Module module = m_ModuleLookup[command.Entity];
                module.IsBeingUpdated = true;
                module.IsReplicated = true;
                module.ReplicatedStacks = command.Stacks;
                module.ReplicatedPreviousStacks = command.PreviousStacks;
                commandBuffer.SetComponent(command.Entity, module);
                commandBuffer.SetComponentEnabled<ModuleUpdateTag>(command.Entity, true);
            }
            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}
#endif