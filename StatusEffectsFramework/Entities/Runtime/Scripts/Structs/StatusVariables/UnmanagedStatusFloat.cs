#if ENTITIES
using Unity.Burst;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    public struct UnmanagedStatusFloat
    {
#if NETCODE
        [GhostField]
#endif
        public ushort StatusName;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private int m_CachedIndex;

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusFloats"/> value for this <see cref="UnmanagedStatusFloat"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetValue(in TypeIndex typeIndex, in DynamicBuffer<StatusFloats> buffer, out float value)
        {
            if (TryGetIndex(typeIndex, buffer, out int index))
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
        public bool TryGetElement(in TypeIndex typeIndex, in DynamicBuffer<StatusFloats> buffer, out StatusFloats value)
        {
            if (TryGetIndex(typeIndex, buffer, out int index))
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
        public bool TryGetIndex(in TypeIndex typeIndex, in DynamicBuffer<StatusFloats> buffer, out int index)
        {
            StatusFloats statusFloat;
            int length = buffer.Length;
            index = m_CachedIndex;
            if (index >= 0 && index < buffer.Length)
            {
                statusFloat = buffer[index];
                if (statusFloat.TypeIndex == typeIndex && statusFloat.StatusName == StatusName)
                    return true;
            }

            index = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusFloat = buffer[i];
                if (statusFloat.TypeIndex == typeIndex && statusFloat.StatusName == StatusName)
                {
                    index = i;
                    break;
                }
            }

            m_CachedIndex = index;
            return index >= 0;
        }

        public UnmanagedStatusFloat(ushort statusName)
        {
            StatusName = statusName;
            m_CachedIndex = -1;
        }
        
        public static implicit operator UnmanagedStatusFloat(StatusFloat value) => new UnmanagedStatusFloat(value != null && value.StatusName ? value.StatusName.Id : default);
    }
}
#endif