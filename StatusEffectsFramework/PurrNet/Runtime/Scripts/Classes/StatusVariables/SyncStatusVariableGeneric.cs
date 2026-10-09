using System;
using UnityEngine;

namespace StatusEffectsFramework.PurrNet
{
    /// <summary>
    /// The network side of a status variable. The evaluation logic lives in the regular
    /// <see cref="StatusVariable{TState}"/> that each PurrNet status variable wraps, so this only
    /// handles write permissions and sending the base values.
    /// </summary>
    /// <remarks>
    /// PurrNet's RPCs can't be generic, so each concrete type declares the RPCs that send its own state.
    /// The server sends its state to everyone (buffered, so late joiners get the latest), and an owner's
    /// change is sent to the server, which checks the write permission and then sends it to everyone.
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
            OnLocalChange,
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

        /// <summary>
        /// Sends the given state from the server to everyone.
        /// </summary>
        protected abstract void SendToObservers(TState state);

        /// <summary>
        /// Sends the given state from an owner to the server.
        /// </summary>
        protected abstract void SendToServer(TState state);

        public override void OnSpawn()
        {
            base.OnSpawn();

            // Clients joining later get the latest state through the buffered RPC.
            if (isServer)
                SendToObservers(m_Inner.GetState());
        }

        /// <summary>
        /// Called by the RPC that carries a state from the server.
        /// </summary>
        protected void ReceiveFromServer(TState state)
        {
            if (!isServer)
                Replicator.Apply(state);
        }

        /// <summary>
        /// Called by the RPC that carries a state from an owner. Applies it and sends it to everyone.
        /// </summary>
        protected void ReceiveFromOwner(TState state)
        {
            if (m_WritePermission != StatusWritePermission.ServerAndOwner)
            {
                Debug.LogWarning("A client sent a value for a Status Variable whose write permission only allows the server.");
                return;
            }

            Replicator.Apply(state);
            SendToObservers(state);
        }

        private void OnLocalChange()
        {
            var state = m_Inner.GetState();

            if (isServer)
                SendToObservers(state);
            else
                SendToServer(state);
        }
    }
}
