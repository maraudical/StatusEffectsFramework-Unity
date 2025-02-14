#if NETCODE_GAMEOBJECTS && COLLECTIONS
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.NetCode.GameObjects.Example
{
    public class ConnectionManager : MonoBehaviour
    {
        [SerializeField] private Button m_ClientButton;
        [SerializeField] private Button m_HostButton;

        private void Start()
        {
            m_ClientButton.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
            m_HostButton.onClick.AddListener(() => NetworkManager.Singleton.StartHost());

            NetworkManager.Singleton.OnClientStarted += Started;
            NetworkManager.Singleton.OnServerStarted += Started;
        }

        private void Started()
        {
            NetworkManager.Singleton.OnClientStarted -= Started;
            NetworkManager.Singleton.OnServerStarted -= Started;

            gameObject.SetActive(false);
        }
    }
}
#endif