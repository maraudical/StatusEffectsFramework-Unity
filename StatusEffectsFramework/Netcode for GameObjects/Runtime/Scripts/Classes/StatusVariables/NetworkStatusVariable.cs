using System;
using Unity.Netcode;

namespace StatusEffectsFramework.Netcode
{
    /// <summary>
    /// The non-generic base of every network status variable, so they can be handled without knowing their types.
    /// </summary>
    [Serializable]
    public abstract class NetworkStatusVariable : NetworkVariableBase
    {
        protected NetworkStatusVariable(NetworkVariableReadPermission readPerm, NetworkVariableWritePermission writePerm) : base(readPerm, writePerm) { }

        /// <summary>
        /// Sets up the <see cref="NetworkStatusVariable"/>. This must be set before trying to get any value from it.
        /// </summary>
        public abstract void SetManager(INetworkStatusManager instance);
    }
}
