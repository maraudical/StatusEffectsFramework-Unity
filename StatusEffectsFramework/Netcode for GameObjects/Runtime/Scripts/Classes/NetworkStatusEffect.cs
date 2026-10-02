#if NETCODE
using System;
using Unity.Netcode;

namespace StatusEffectsFramework.NetCode
{
    /// <summary>
    /// Captures a <see cref="StatusEffect"/> as values that are serializable by Netcode for GameObjects.
    /// </summary>
    public struct NetworkStatusEffect : INetworkSerializable, IEquatable<NetworkStatusEffect>
    {
        /// <summary>
        /// The id of the <see cref="StatusEffectData"/> in the <see cref="StatusRegistry"/>.
        /// </summary>
        public ushort Id;
        public StatusEffectTiming Timing;
        /// <summary>
        /// The <see cref="StatusEffect.Duration"/>. For <see cref="StatusEffectTiming.Duration"/> this is the
        /// total duration, measured from <see cref="ServerTimeAdded"/>.
        /// </summary>
        public float Duration;
        public int Stacks;
        public uint InstanceId;
        /// <summary>
        /// The server time the status effect was added at, so clients can work out how much time has passed.
        /// </summary>
        public double ServerTimeAdded;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Id);
            serializer.SerializeValue(ref Timing);
            serializer.SerializeValue(ref Duration);
            serializer.SerializeValue(ref Stacks);
            serializer.SerializeValue(ref InstanceId);
            serializer.SerializeValue(ref ServerTimeAdded);
        }

        public override int GetHashCode() => InstanceId.GetHashCode();

        public override bool Equals(object obj) => obj is NetworkStatusEffect other && Equals(other);

        /// <remarks>
        /// Compares every field, not just <see cref="InstanceId"/>. A <see cref="NetworkList{T}"/> ignores writes
        /// of an equal value, so comparing by instance id alone would drop changes to the stacks or duration.
        /// </remarks>
        public bool Equals(NetworkStatusEffect other)
        {
            return InstanceId == other.InstanceId
                && Id == other.Id
                && Timing == other.Timing
                && Duration == other.Duration
                && Stacks == other.Stacks
                && ServerTimeAdded == other.ServerTimeAdded;
        }

        public NetworkStatusEffect(ushort id, StatusEffectTiming timing, float duration, int stacks, uint instanceId, double serverTimeAdded)
        {
            Id = id;
            Timing = timing;
            Duration = duration;
            Stacks = stacks;
            InstanceId = instanceId;
            ServerTimeAdded = serverTimeAdded;
        }
    }
}
#endif
