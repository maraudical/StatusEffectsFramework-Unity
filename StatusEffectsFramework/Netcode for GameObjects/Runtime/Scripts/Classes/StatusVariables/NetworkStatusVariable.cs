#if NETCODE
using Unity.Netcode;
using UnityEngine;

namespace StatusEffectsFramework.NetCode
{
    public abstract class NetworkStatusVariable : NetworkVariableBase
    {
        protected IStatusManager Instance;

        public NetworkStatusVariable(NetworkVariableReadPermission readPerm, NetworkVariableWritePermission writePerm) : base(readPerm, writePerm) { }
        /// <summary>
        /// Sets up the <see cref="NetworkStatusVariable"/>. This must be set before trying to get any value from it.
        /// </summary>
        public virtual void SetManager(IStatusManager instance)
        {
            if (Instance != null)
                Instance.StatusEffectAction -= OnStatusEffect;

            Instance = instance;

            Instance.StatusEffectAction += OnStatusEffect;
        }

        protected abstract void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks);

        protected void LogWritePermissionError(NetworkStatusManager manager)
        {
            Debug.LogError($"|Client-{manager.NetworkManager.LocalClientId}|{manager.name}|{Name}| Write permissions ({WritePerm}) for this client instance is not allowed! If this is a Host/Server, double check that this GameObject has a NetworkObject component!");
        }
    }
}
#endif
