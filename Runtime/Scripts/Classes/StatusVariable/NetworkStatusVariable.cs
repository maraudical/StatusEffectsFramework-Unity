#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
using StatusEffects;
using Unity.Netcode;
using UnityEngine;

namespace StatusEffects
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
                Instance.ValueUpdate -= InstanceUpdate;

            Instance = instance;

            Instance.ValueUpdate += InstanceUpdate;
        }

        protected abstract void InstanceUpdate(StatusEffect statusEffect);

        protected void LogWritePermissionError(NetworkStatusManager manager)
        {
            Debug.LogError($"|Client-{manager.NetworkManager.LocalClientId}|{manager.name}|{Name}| Write permissions ({WritePerm}) for this client instance is not allowed!");
        }
    }
}
#endif