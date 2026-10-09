using System;
using Mirror;

namespace StatusEffectsFramework.Mirror
{
    /// <summary>
    /// The non-generic base of every Mirror status variable, so they can be handled without knowing their types.
    /// </summary>
    [Serializable]
    public abstract class SyncStatusVariable : SyncObject
    {
        /// <summary>
        /// Sets up the <see cref="SyncStatusVariable"/>. This must be set before trying to get any value from it.
        /// </summary>
        public abstract void SetManager(INetworkStatusManager instance);
    }
}
