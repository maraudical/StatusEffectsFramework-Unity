using System;
using Mirror;
using UnityEngine;

namespace StatusEffectsFramework.Mirror
{
    /// <summary>
    /// The network side of a status variable. The evaluation logic lives in the regular
    /// <see cref="StatusVariable{TState}"/> that each Mirror status variable wraps, so this only
    /// handles write permissions, dirty flags and serializing the base values.
    /// </summary>
    /// <remarks>
    /// Mirror syncs a <see cref="SyncObject"/> through the <see cref="NetworkBehaviour"/> that declares it, so
    /// that behaviour must have its Sync Direction set to Client To Server for owners to be able to write when
    /// the write permission is <see cref="StatusWritePermission.ServerAndOwner"/>.
    /// </remarks>
    [Serializable]
    public abstract class SyncStatusVariable<TVar, TState> : SyncStatusVariable
        where TVar : StatusVariable<TState>
        where TState : struct, IStatusState<TState>
    {
        [SerializeField] protected TVar m_Inner;

        private readonly StatusWritePermission m_WritePermission;
        private StatusVariableReplicator<TVar, TState> m_Replicator;

        private StatusVariableReplicator<TVar, TState> Replicator => m_Replicator ??= new StatusVariableReplicator<TVar, TState>(
            m_Inner,
            () => OnDirty?.Invoke(),
            m_WritePermission);

        protected SyncStatusVariable(TVar inner, StatusWritePermission writePermission)
        {
            m_Inner = inner;
            m_WritePermission = writePermission;
        }

        public override void SetManager(INetworkStatusManager instance) => Replicator.SetManager(instance);

        /// <summary>
        /// Whether this instance may change the base values. Logs an error when it isn't allowed.
        /// </summary>
        protected bool CanWrite() => Replicator.CanWrite();

        public override void OnSerializeAll(NetworkWriter writer)
        {
            var serializer = new NetworkStatusWriter(writer);
            Replicator.Write(ref serializer);
        }

        public override void OnSerializeDelta(NetworkWriter writer) => OnSerializeAll(writer);

        public override void OnDeserializeAll(NetworkReader reader)
        {
            var serializer = new NetworkStatusReader(reader);
            Replicator.Read(ref serializer);
        }

        public override void OnDeserializeDelta(NetworkReader reader)
        {
            OnDeserializeAll(reader);

            // When the server receives a value from an owner it needs to forward it to everyone else.
            if (Replicator.IsServer)
                OnDirty?.Invoke();
        }

        public override void ClearChanges() { }

        public override void Reset() { }
    }
}
