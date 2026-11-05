#if Netcode_for_Entities
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    /// <summary>
    /// One hash of a <see cref="StatusRegistryPayload"/>. Netcode for Entities RPCs can't hold a Hash128, so it travels as four uints.
    /// </summary>
    public struct RegistryPayloadHash
    {
        public uint X;
        public uint Y;
        public uint Z;
        public uint W;
    }

    /// <summary>
    /// The server's <see cref="StatusRegistryPayload"/>, sent to each client before it goes in game. It holds up to 31
    /// hashes, the main registry and 30 manifests, which keeps it to one packet. More would need to be split across RPCs.
    /// </summary>
    public struct RegistryPayloadRpc : IRpcCommand
    {
        public FixedList512Bytes<RegistryPayloadHash> ManifestHashes;
    }

    /// <summary>
    /// Added to a connection once the server has sent it the payload, or the client has applied it.
    /// </summary>
    public struct RegistryPayloadReady : IComponentData { }

    /// <summary>
    /// Sends every new connection the server's registry, so the client can give status effects the same ids before
    /// <see cref="PlayerEntryClientSystem"/> puts it in game.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class RegistryPayloadServerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate(SystemAPI.QueryBuilder().WithAll<NetworkId>().WithNone<RegistryPayloadReady>().Build());
        }

        protected override void OnUpdate()
        {
            // The manifests found at startup have to be registered first, or the payload would leave them out.
            var registry = StatusRegistry.Instance;
            if (!registry || !registry.IsReady)
                return;

            var rpc = new RegistryPayloadRpc();
            foreach (Unity.Entities.Hash128 hash in registry.CreatePayload().ManifestHashes)
            {
                if (rpc.ManifestHashes.Length == rpc.ManifestHashes.Capacity)
                {
                    Debug.LogError($"The server has more status effect content than one {nameof(RegistryPayloadRpc)} holds, so clients may be told they are missing some.");
                    break;
                }

                rpc.ManifestHashes.Add(new RegistryPayloadHash { X = hash.Value.x, Y = hash.Value.y, Z = hash.Value.z, W = hash.Value.w });
            }

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, connection) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<RegistryPayloadReady>().WithEntityAccess())
            {
                var request = commandBuffer.CreateEntity();
                commandBuffer.AddComponent(request, rpc);
                commandBuffer.AddComponent(request, new SendRpcCommandRequest { TargetConnection = connection });
                commandBuffer.AddComponent<RegistryPayloadReady>(connection);
            }
            commandBuffer.Playback(EntityManager);
        }
    }

    /// <summary>
    /// Applies the server's registry, or disconnects when this client is missing content the server uses.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class RegistryPayloadClientSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate(SystemAPI.QueryBuilder().WithAll<RegistryPayloadRpc, ReceiveRpcCommandRequest>().Build());
        }

        protected override void OnUpdate()
        {
            // Manifests that are still loading would count as missing, so applying the payload waits for them.
            var registry = StatusRegistry.Instance;
            if (!registry || !registry.IsReady)
                return;

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (rpc, receive, entity) in SystemAPI.Query<RefRO<RegistryPayloadRpc>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
            {
                var hashes = new UnityEngine.Hash128[rpc.ValueRO.ManifestHashes.Length];
                for (int i = 0; i < hashes.Length; i++)
                {
                    var hash = rpc.ValueRO.ManifestHashes[i];
                    hashes[i] = new Unity.Entities.Hash128(hash.X, hash.Y, hash.Z, hash.W);
                }

                var connection = receive.ValueRO.SourceConnection;
                if (registry.TryApplyPayload(new StatusRegistryPayload(hashes), out var missing))
                {
                    commandBuffer.AddComponent<RegistryPayloadReady>(connection);
                }
                else
                {
                    Debug.LogError($"Disconnecting: this client is missing {missing} set(s) of status effect content the server uses.");
                    commandBuffer.AddComponent<NetworkStreamRequestDisconnect>(connection);
                }

                commandBuffer.DestroyEntity(entity);
            }
            commandBuffer.Playback(EntityManager);
        }
    }
}
#endif
