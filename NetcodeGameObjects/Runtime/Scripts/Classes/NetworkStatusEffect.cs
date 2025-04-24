#if NETCODE
using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace StatusEffects.NetCode.GameObjects
{
    /// <summary>
    /// A class that captures Status Effects into values that are 
    /// serializable/deserializable by Netcode for GameObjects.
    /// </summary>
    public struct NetworkStatusEffect : INetworkSerializable, IEquatable<NetworkStatusEffect>
    {
        public FixedString64Bytes Id;
        public StatusEffectTiming Timing;
        public float Duration;
        public int Stacks;
        public FixedString64Bytes InstanceId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Id);
            serializer.SerializeValue(ref Timing);
            serializer.SerializeValue(ref Duration);
            serializer.SerializeValue(ref Stacks);
            serializer.SerializeValue(ref InstanceId);
        }

        public override int GetHashCode()
        {
            return InstanceId.GetHashCode();
        }

        public bool Equals(NetworkStatusEffect other)
        {
            return InstanceId == other.InstanceId;
        }

        public NetworkStatusEffect(Hash128 id, StatusEffectTiming timing, float duration, int stacks, Hash128 instanceId)
        {
            Id = id.ToString();
            Timing = timing;
            Duration = duration;
            Stacks = stacks;
            InstanceId = instanceId.ToString();
        }
    }
}
#endif