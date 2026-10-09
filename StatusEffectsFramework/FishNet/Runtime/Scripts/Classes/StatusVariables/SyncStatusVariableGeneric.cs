using System;
using FishNet.Serializing;
using UnityEngine;

namespace StatusEffectsFramework.FishNet
{
    /// <summary>
    /// The network side of a status variable. The evaluation logic lives in the regular
    /// <see cref="StatusVariable{TState}"/> that each FishNet status variable wraps, so this only
    /// handles write permissions, dirty flags and serializing the base values.
    /// </summary>
    /// <remarks>
    /// A FishNet sync type is only written by the server, so when the write permission is
    /// <see cref="StatusWritePermission.ServerAndOwner"/> an owner's change is sent to the server through the
    /// <see cref="NetworkStatusManager"/>, which checks the owner and then syncs it to everyone.
    /// </remarks>
    [Serializable]
    public abstract class SyncStatusVariable<TVar, TState> : SyncStatusVariable
        where TVar : StatusVariable<TState>
        where TState : struct, IStatusState<TState>
    {
        [SerializeField] protected TVar m_Inner;

        private readonly StatusWritePermission m_WritePermission;
        private StatusVariableReplicator<TVar, TState> m_Replicator;
        private NetworkStatusManager m_Manager;
        private int m_ManagerIndex = -1;

        private StatusVariableReplicator<TVar, TState> Replicator => m_Replicator ??= new StatusVariableReplicator<TVar, TState>(
            m_Inner,
            OnLocalChange,
            m_WritePermission);

        protected SyncStatusVariable(TVar inner, StatusWritePermission writePermission)
        {
            m_Inner = inner;
            m_WritePermission = writePermission;
        }

        /// <summary>
        /// True if an owner is allowed to submit changes to the server.
        /// </summary>
        internal bool OwnerCanWrite => m_WritePermission == StatusWritePermission.ServerAndOwner;

        public override void SetManager(NetworkStatusManager instance)
        {
            m_Manager = instance;
            m_ManagerIndex = instance.RegisterVariable(this);

            Replicator.SetManager(instance);
        }

        /// <summary>
        /// Whether this instance may change the base values. Logs an error when it isn't allowed.
        /// </summary>
        protected bool CanWrite() => Replicator.CanWrite();

        /// <summary>
        /// Sends the current state from an owner to the server.
        /// </summary>
        protected abstract void SubmitToServer(NetworkStatusManager manager, int index, in TState state);

        /// <summary>
        /// Applies a state an owner sent to the server, then syncs it to everyone.
        /// </summary>
        internal void ReceiveFromOwner(in TState state)
        {
            Replicator.Apply(state);
            Dirty();
        }

        private void OnLocalChange()
        {
            if (((INetworkStatusManager)m_Manager).IsServer)
                Dirty();
            else
                SubmitToServer(m_Manager, m_ManagerIndex, m_Inner.GetState());
        }

        public override void WriteDelta(PooledWriter writer, bool resetSyncTick = true)
        {
            base.WriteDelta(writer, resetSyncTick);

            var serializer = new NetworkStatusWriter(writer);
            Replicator.Write(ref serializer);
        }

        public override void WriteFull(PooledWriter writer) => WriteDelta(writer, false);

        protected internal override void Read(PooledReader reader, bool asServer)
        {
            var serializer = new NetworkStatusReader(reader);
            var state = default(TState);
            state.Serialize(ref serializer);

            // The server is the source of truth, so only clients apply what they read.
            if (!asServer)
                Replicator.Apply(state);
        }

        protected internal override void ResetState() { }
    }
}
