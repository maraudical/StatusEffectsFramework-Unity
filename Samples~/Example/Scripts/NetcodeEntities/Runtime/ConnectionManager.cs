#if NETCODE_ENTITIES
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utility;

namespace StatusEffects.NetCode.GameObjects.Example
{
    public class ConnectionManager : MonoBehaviour
    {
        [SerializeField] private string m_IpAddress = "127.0.0.1";
        [SerializeField] private ushort m_Port = 7777;
        [SerializeField] private string m_ServerWorld = "Server World";
        [SerializeField] private string m_ClientWorld = "Client World";
        [SerializeField] private Button m_ClientButton;
        [SerializeField] private Button m_HostButton;

        private void Start()
        {
            // Try to find any running client or server to see if one still needs to be made.
            foreach (var world in World.All)
                if (world.Flags is WorldFlags.GameServer or WorldFlags.GameClient or WorldFlags.GameThinClient)
                {
                    gameObject.SetActive(false);
                    return;
                }

            m_ClientButton.onClick.AddListener(() => 
            {
                NetworkUtils.DestroyLocalSimulationWorld();
                NetworkUtils.InitializeClient(m_ClientWorld, m_IpAddress, m_Port);
                // Ideally the worlds would have been made before entering the game.
                // That way we wouldn't have to reload the scene.
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); 
            });
            m_HostButton.onClick.AddListener(() => 
            {
                NetworkUtils.DestroyLocalSimulationWorld();
                NetworkUtils.InitializeHost(m_ClientWorld, m_ServerWorld, m_IpAddress, m_Port);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }
    }
}
#endif