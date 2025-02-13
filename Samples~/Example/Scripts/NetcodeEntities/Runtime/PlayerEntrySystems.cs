#if NETCODE_ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Gameplay.Player
{
    public struct PlayerEntryRequest : IRpcCommand { }

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct PlayerEntryClientSystem : ISystem
    {
        private EntityQuery m_PendingNetworkIdQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<NetworkId>().WithNone<NetworkStreamInGame>();
            m_PendingNetworkIdQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(m_PendingNetworkIdQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var pendingNetworkIds = m_PendingNetworkIdQuery.ToEntityArray(Allocator.Temp);

            foreach (var pendingNetworkId in pendingNetworkIds)
            {
                commandBuffer.AddComponent<NetworkStreamInGame>(pendingNetworkId);
                var request = commandBuffer.CreateEntity();
                commandBuffer.AddComponent(request, new PlayerEntryRequest());
                commandBuffer.AddComponent(request, new SendRpcCommandRequest { TargetConnection = pendingNetworkId });
            }
            commandBuffer.Playback(state.EntityManager);
        }
    }


    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct PlayerEntryServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<PlayerEntryRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, rpc, entity) in SystemAPI.Query<PlayerEntryRequest, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                commandBuffer.DestroyEntity(entity);
                commandBuffer.AddComponent<NetworkStreamInGame>(rpc.SourceConnection);
            }
            commandBuffer.Playback(state.EntityManager);
        }
    }
}
#endif