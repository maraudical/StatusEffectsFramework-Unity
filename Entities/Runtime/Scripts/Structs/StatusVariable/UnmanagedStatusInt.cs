#if ENTITIES
using Unity.Burst;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    [BurstCompile]
    public struct UnmanagedStatusInt
    {
        public Hash128 Id;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private int m_CachedIndex;
        
        /// <summary>
        /// Attempt to retrieve the <see cref="StatusInts"/> value for this <see cref="UnmanagedStatusInt"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetValue(in Hash128 componentId, in DynamicBuffer<StatusInts> buffer, out int value)
        {
            if (TryGetIndex(componentId, buffer, out int index))
            {
                value = buffer[index].Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusInts"/> for this <see cref="UnmanagedStatusInt"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetElement(in Hash128 componentId, in DynamicBuffer<StatusInts> buffer, out StatusInts value)
        {
            if (TryGetIndex(componentId, buffer, out int index))
            {
                value = buffer[index];
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusInts"/> index value for this <see cref="UnmanagedStatusInt"/>.
        /// </summary>
        [BurstCompile]
        public bool TryGetIndex(in Hash128 componentId, in DynamicBuffer<StatusInts> buffer, out int index)
        {
            StatusInts statusInts;
            int length = buffer.Length;
            index = m_CachedIndex;
            if (index >= 0 && index < buffer.Length)
            {
                statusInts = buffer[index];
                if (statusInts.ComponentId == componentId && statusInts.Id == Id)
                    return true;
            }

            index = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusInts = buffer[i];
                if (statusInts.ComponentId == componentId && statusInts.Id == Id)
                {
                    index = i;
                    break;
                }
            }

            m_CachedIndex = index;
            return index >= 0;
        }

        public UnmanagedStatusInt(Hash128 id)
        {
            Id = id;
            m_CachedIndex = -1;
        }

        public static implicit operator UnmanagedStatusInt(UnityEngine.Hash128 value) => new UnmanagedStatusInt(value);
        public static implicit operator UnmanagedStatusInt(Hash128 value) => new UnmanagedStatusInt(value);
        public static implicit operator UnmanagedStatusInt(StatusInt value) => new UnmanagedStatusInt(value != null && value.StatusName ? value.StatusName.Id : default);
    }
}
#endif