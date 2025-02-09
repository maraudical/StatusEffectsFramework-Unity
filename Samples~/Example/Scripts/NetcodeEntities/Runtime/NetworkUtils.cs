#if NETCODE_ENTITIES
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Utility
{
    public static class NetworkUtils
    {
        public static void DestroyLocalSimulationWorld()
        {
            foreach (var world in World.All)
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
        }

        /// <inheritdoc cref="InitializeHost(string, string, NetworkEndpoint)"/>
        public static (World ClientWorld, World ServerWorld, NetworkEndpoint Endpoint) InitializeHost(string clientWorldName, string serverWorldName, string address, ushort port)
        {
            if (!NetworkEndpoint.TryParse(address, port, out var endpoint))
                Debug.LogError($"Failure to parse network endpoint for address ({address}) or port ({port}).");

            var hostData = InitializeHost(clientWorldName, serverWorldName, endpoint);

            return (hostData.ClientWorld, hostData.ServerWorld, endpoint);
        }

        /// <summary>
        /// Initializes the host.
        /// </summary>
        public static (World ClientWorld, World ServerWorld) InitializeHost(string clientWorldName, string serverWorldName, NetworkEndpoint endpoint)
        {
            var serverWorld = InitializeServer(serverWorldName, endpoint, false);
            var clientWorld = InitializeClient(clientWorldName, endpoint);

            return (clientWorld, serverWorld);
        }

        /// <inheritdoc cref="InitializeServer(string, NetworkEndpoint, bool)"/>
        public static (World World, NetworkEndpoint Endpoint) InitializeServer(string serverWorldName, string address, ushort port, bool overrideDefaultInjectionWorld = true)
        {
            if (!NetworkEndpoint.TryParse(address, port, out var serverEndpoint))
                Debug.LogError($"Failure to parse network endpoint for address ({address}) or port ({port}).");

            return (InitializeServer(serverWorldName, serverEndpoint, overrideDefaultInjectionWorld), serverEndpoint);
        }

        /// <summary>
        /// Initializes the server.
        /// </summary>
        public static World InitializeServer(string serverWorldName, NetworkEndpoint endpoint, bool overrideDefaultInjectionWorld = true)
        {
            var serverWorld = ClientServerBootstrap.CreateServerWorld(serverWorldName);

            using var networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(endpoint);

            if (overrideDefaultInjectionWorld)
                World.DefaultGameObjectInjectionWorld = serverWorld;

            return serverWorld;
        }

        /// <inheritdoc cref="InitializeClient(string, NetworkEndpoint, bool)"/>
        public static World InitializeClient(string clientWorldName, string address, ushort port, bool overrideDefaultInjectionWorld = true)
        {
            if (!NetworkEndpoint.TryParse(address, port, out var endpoint))
                Debug.LogError($"Failure to parse network endpoint for address ({address}) or port ({port}).");

            return InitializeClient(clientWorldName, endpoint, overrideDefaultInjectionWorld);
        }

        /// <summary>
        /// Initializes the client.
        /// </summary>
        public static World InitializeClient(string clientWorldName, NetworkEndpoint endpoint, bool overrideDefaultInjectionWorld = true)
        {
            var clientWorld = ClientServerBootstrap.CreateClientWorld(clientWorldName);

            using var networkDriverQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(clientWorld.EntityManager, endpoint);

            if (overrideDefaultInjectionWorld)
                World.DefaultGameObjectInjectionWorld = clientWorld;

            return clientWorld;
        }
    }
}
#endif