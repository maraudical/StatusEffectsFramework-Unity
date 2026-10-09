using System;
using Unity.Netcode;
using UnityEngine;

namespace StatusEffectsFramework.Netcode
{
    /// <summary>
    /// The network side of a status variable. The evaluation logic lives in the regular
    /// <see cref="StatusVariable{TState}"/> that each network status variable wraps, so this only
    /// handles write permissions, dirty flags and serializing the base values.
    /// </summary>
    [Serializable]
    public abstract class NetworkStatusVariable<TVar, TState> : NetworkStatusVariable
        where TVar : StatusVariable<TState>
        where TState : struct, IStatusState<TState>
    {
        [SerializeField] protected TVar m_Inner;

        private StatusVariableReplicator<TVar, TState> m_Replicator;

        private StatusVariableReplicator<TVar, TState> Replicator => m_Replicator ??= new StatusVariableReplicator<TVar, TState>(
            m_Inner,
            () => SetDirty(true),
            WritePerm == NetworkVariableWritePermission.Owner ? StatusWritePermission.ServerAndOwner : StatusWritePermission.Server);

        protected NetworkStatusVariable(TVar inner, StatusWritePermission writePermission)
            : base(NetworkVariableReadPermission.Everyone, ToNetcode(writePermission))
        {
            m_Inner = inner;
        }

        public override void SetManager(INetworkStatusManager instance) => Replicator.SetManager(instance);

        /// <summary>
        /// Whether this instance may change the base values. Logs an error when it isn't allowed.
        /// </summary>
        protected bool CanWrite() => Replicator.CanWrite();

        public override void WriteField(FastBufferWriter writer)
        {
            var serializer = new NetworkStatusWriter(writer);
            Replicator.Write(ref serializer);
        }

        public override void ReadField(FastBufferReader reader)
        {
            var serializer = new NetworkStatusReader(reader);
            Replicator.Read(ref serializer);
        }

        public override void WriteDelta(FastBufferWriter writer) => WriteField(writer);

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            ReadField(reader);

            // When the server receives a value from an owner it needs to forward it to everyone else.
            if (keepDirtyDelta)
                SetDirty(true);
        }

        private static NetworkVariableWritePermission ToNetcode(StatusWritePermission permission)
        {
            return permission == StatusWritePermission.ServerAndOwner ? NetworkVariableWritePermission.Owner : NetworkVariableWritePermission.Server;
        }
    }
}
