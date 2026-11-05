#if Netcode_for_GameObjects
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffectsFramework.Samples
{
    public class ConnectionManager : MonoBehaviour
    {
        private const string k_RegistryPayloadMessage = "StatusEffectsFramework.RegistryPayload";

        [SerializeField] private Button m_ClientButton;
        [SerializeField] private Button m_HostButton;

        private void Start()
        {
            m_ClientButton.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
            m_HostButton.onClick.AddListener(() => NetworkManager.Singleton.StartHost());

            NetworkManager.Singleton.OnClientStarted += Started;
            NetworkManager.Singleton.OnServerStarted += Started;

            NetworkManager.Singleton.OnServerStarted += ListenForClients;
            NetworkManager.Singleton.OnClientStarted += ListenForRegistryPayload;
        }

        private void Started()
        {
            NetworkManager.Singleton.OnClientStarted -= Started;
            NetworkManager.Singleton.OnServerStarted -= Started;

            gameObject.SetActive(false);
        }

        // The server sends each client its registry, so both give status effects the same ids. A game should finish this
        // before anything with status effects spawns, for example by spawning the player once the client has applied it.
        private void ListenForClients()
        {
            NetworkManager.Singleton.OnServerStarted -= ListenForClients;
            NetworkManager.Singleton.OnClientConnectedCallback += SendRegistryPayload;
        }

        private void SendRegistryPayload(ulong clientId)
        {
            // A host's own client already shares the server's registry.
            if (clientId == NetworkManager.ServerClientId)
                return;

            var bytes = StatusRegistry.Instance.CreatePayload().ToBytes();
            using var writer = new FastBufferWriter(bytes.Length + sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(bytes);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(k_RegistryPayloadMessage, clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private void ListenForRegistryPayload()
        {
            NetworkManager.Singleton.OnClientStarted -= ListenForRegistryPayload;

            if (!NetworkManager.Singleton.IsServer)
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(k_RegistryPayloadMessage, ApplyRegistryPayload);
        }

        private void ApplyRegistryPayload(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte[] bytes);

            if (!StatusRegistryPayload.TryFromBytes(bytes, out var payload))
            {
                Debug.LogError("Disconnecting: the server's status effect registry couldn't be read.");
                NetworkManager.Singleton.Shutdown();
                return;
            }

            if (!StatusRegistry.Instance.TryApplyPayload(payload, out var missing))
            {
                Debug.LogError($"Disconnecting: this client is missing {missing} set(s) of status effect content the server uses.");
                NetworkManager.Singleton.Shutdown();
            }
        }
    }
}
#endif
