#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
using System;
using Unity.Collections;
using Unity.Netcode;

namespace StatusEffects.NetCode.GameObjects
{
    /// <summary>
    /// A class that captures Status Effects into values that are 
    /// serializable/deserializable by Netcode for GameObjects.
    /// </summary>
    public struct NetworkStatusEffect : INetworkSerializable, IEquatable<NetworkStatusEffect>
    {
        public FixedString128Bytes AssetGuid;
        public StatusEffectTiming Timing;
        public float Duration;
        public int Stack;
        public int InstanceId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref AssetGuid);
            serializer.SerializeValue(ref Timing);
            serializer.SerializeValue(ref Duration);
            serializer.SerializeValue(ref Stack);
            serializer.SerializeValue(ref InstanceId);
        }

        public override int GetHashCode()
        {
            return InstanceId;
        }

        public bool Equals(NetworkStatusEffect other)
        {
            return InstanceId == other.InstanceId;
        }

        public NetworkStatusEffect(FixedString128Bytes assetGuid, StatusEffectTiming timing, float duration, int stack, int instanceId)
        {
            AssetGuid = assetGuid;
            Timing = timing;
            Duration = duration;
            Stack = stack;
            InstanceId = instanceId;
        }
    }
}
#endif