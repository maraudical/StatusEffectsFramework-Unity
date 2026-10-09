using System;
using FishNet.Object.Synchronizing;
using FishNet.Object.Synchronizing.Internal;

namespace StatusEffectsFramework.FishNet
{
    /// <summary>
    /// The non-generic base of every FishNet status variable, so they can be handled without knowing their types.
    /// </summary>
    [Serializable]
    public abstract class SyncStatusVariable : SyncBase, ICustomSync
    {
        protected SyncStatusVariable() : base(new SyncTypeSettings()) { }

        /// <summary>
        /// Sets up the <see cref="SyncStatusVariable"/>. This must be set before trying to get any value from it.
        /// </summary>
        public abstract void SetManager(NetworkStatusManager instance);

        // No extra types need serializers generated, since the state is written by hand.
        public object GetSerializedType() => null;
    }
}
