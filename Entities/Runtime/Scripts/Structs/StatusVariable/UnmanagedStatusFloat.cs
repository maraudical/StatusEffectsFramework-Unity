#if ENTITIES
using Unity.Burst;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    [BurstCompile]
    public struct UnmanagedStatusFloat
    {
        public Hash128 Id;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private int m_CachedIndex;

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusFloats"/> value for this <see cref="UnmanagedStatusFloat"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetValue(in Hash128 componentId, in DynamicBuffer<StatusFloats> buffer, out float value)
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
        /// Attempt to retrieve the <see cref="StatusFloats"/> for this <see cref="UnmanagedStatusFloat"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetElement(in Hash128 componentId, in DynamicBuffer<StatusFloats> buffer, out StatusFloats value)
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
        /// Attempt to retrieve the <see cref="StatusFloats"/> index value for this <see cref="UnmanagedStatusFloat"/>.
        /// </summary>
        [BurstCompile]
        public bool TryGetIndex(in Hash128 componentId, in DynamicBuffer<StatusFloats> buffer, out int index)
        {
            StatusFloats statusFloat;
            int length = buffer.Length;
            index = m_CachedIndex;
            if (index >= 0 && index < buffer.Length)
            {
                statusFloat = buffer[index];
                if (statusFloat.ComponentId == componentId && statusFloat.Id == Id)
                    return true;
            }

            index = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusFloat = buffer[i];
                if (statusFloat.ComponentId == componentId && statusFloat.Id == Id)
                {
                    index = i;
                    break;
                }
            }

            m_CachedIndex = index;
            return index >= 0;
        }

        public UnmanagedStatusFloat(Hash128 id)
        {
            Id = id;
            m_CachedIndex = -1;
        }

        public static implicit operator UnmanagedStatusFloat(UnityEngine.Hash128 value) => new UnmanagedStatusFloat(value);
        public static implicit operator UnmanagedStatusFloat(Hash128 value) => new UnmanagedStatusFloat(value);
        public static implicit operator UnmanagedStatusFloat(StatusFloat value) => new UnmanagedStatusFloat(value != null && value.StatusName ? value.StatusName.Id : default);
    }
}
#endif