using System;
using PurrNet;

namespace StatusEffectsFramework.PurrNet
{
    /// <summary>
    /// The non-generic base of every PurrNet status variable, so they can be handled without knowing their types.
    /// </summary>
    [Serializable]
    public abstract class SyncStatusVariable : NetworkModule
    {
        /// <summary>
        /// Sets up the <see cref="SyncStatusVariable"/>. This must be set before trying to get any value from it.
        /// </summary>
        public abstract void SetManager(INetworkStatusManager instance);
    }
}
