using System;
using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// The networking library agnostic half of a networked <see cref="StatusVariable{TState}"/>. It checks write
    /// permissions, flags the variable as dirty after local changes, and keeps values received from the network
    /// from being sent back out. Each networking library wraps this with its own sync type.
    /// </summary>
    public sealed class StatusVariableReplicator<TVar, TState>
        where TVar : StatusVariable<TState>
        where TState : struct, IStatusState<TState>
    {
        public TVar Inner { get; }
        public StatusWritePermission WritePermission { get; set; }
        /// <summary>
        /// True if the manager has been set and this instance is the server or host.
        /// </summary>
        public bool IsServer => m_Manager != null && m_Manager.IsServer;

        private readonly Action m_MarkDirty;
        private INetworkStatusManager m_Manager;
        private bool m_Applying;

        /// <param name="markDirty">Called after a permitted local change so the library syncs the new state.</param>
        public StatusVariableReplicator(TVar inner, Action markDirty, StatusWritePermission writePermission = StatusWritePermission.ServerAndOwner)
        {
            Inner = inner;
            WritePermission = writePermission;
            m_MarkDirty = markDirty;

            Inner.StateChanged += OnStateChanged;
        }

        /// <summary>
        /// Sets up the status variable. This must be set before trying to get any value from it.
        /// </summary>
        public void SetManager(INetworkStatusManager instance)
        {
            m_Manager = instance;

            Inner.SetManager(instance);
        }

        /// <summary>
        /// Whether this instance may change the base values. Logs an error when it isn't allowed.
        /// </summary>
        public bool CanWrite()
        {
            if (m_Manager == null || !m_Manager.IsSpawned || m_Manager.IsServer)
                return true;

            if (WritePermission == StatusWritePermission.ServerAndOwner && m_Manager.IsOwner)
                return true;

            Debug.LogError($"Write permissions ({WritePermission}) for this instance are not allowed on this Status Variable! If this is a Host/Server, double check that this GameObject has a NetworkObject component!");
            return false;
        }

        public void Write<TSerializer>(ref TSerializer serializer) where TSerializer : struct, IStatusSerializer
        {
            var state = Inner.GetState();
            state.Serialize(ref serializer);
        }

        public void Read<TSerializer>(ref TSerializer serializer) where TSerializer : struct, IStatusSerializer
        {
            var state = default(TState);
            state.Serialize(ref serializer);

            Apply(state);
        }

        /// <summary>
        /// Applies a state received from the network without sending it back out.
        /// </summary>
        public void Apply(in TState state)
        {
            m_Applying = true;
            try
            {
                Inner.ApplyState(state);
            }
            finally
            {
                m_Applying = false;
            }
        }

        private void OnStateChanged()
        {
            if (m_Applying || m_Manager == null || !m_Manager.IsSpawned)
                return;

            if (CanWrite())
                m_MarkDirty();
        }
    }
}
