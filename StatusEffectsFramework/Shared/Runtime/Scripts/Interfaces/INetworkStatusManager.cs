namespace StatusEffectsFramework
{
    /// <summary>
    /// An <see cref="IStatusManager"/> that is synced over a network. Implemented by the manager of each
    /// networking library so status variables can check who is allowed to write to them.
    /// </summary>
    public interface INetworkStatusManager : IStatusManager
    {
        /// <summary>
        /// True once the manager is spawned on the network.
        /// </summary>
        bool IsSpawned { get; }
        /// <summary>
        /// True if this instance is the server or host.
        /// </summary>
        bool IsServer { get; }
        /// <summary>
        /// True if this instance owns the manager.
        /// </summary>
        bool IsOwner { get; }
    }
}
