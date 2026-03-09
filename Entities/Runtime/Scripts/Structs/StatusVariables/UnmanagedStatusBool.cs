#if ENTITIES
using Unity.Burst;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectFramework.Entities
{
    [BurstCompile]
    public struct UnmanagedStatusBool
    {
        public Hash128 Id;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private int m_CachedIndex;

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusBools"/> value for this <see cref="UnmanagedStatusBool"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetValue(in Hash128 componentId, in DynamicBuffer<StatusBools> buffer, out bool value)
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
        /// Attempt to retrieve the <see cref="StatusBools"/> for this <see cref="UnmanagedStatusBool"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetElement(in Hash128 componentId, in DynamicBuffer<StatusBools> buffer, out StatusBools value)
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
        /// Attempt to retrieve the <see cref="StatusBools"/> index value for this <see cref="UnmanagedStatusBool"/>.
        /// </summary>
        [BurstCompile]
        public bool TryGetIndex(in Hash128 componentId, in DynamicBuffer<StatusBools> buffer, out int index)
        {
            StatusBools statusBool;
            int length = buffer.Length;
            index = m_CachedIndex;
            if (index >= 0 && index < buffer.Length)
            {
                statusBool = buffer[index];
                if (statusBool.ComponentId == componentId && statusBool.Id == Id)
                    return true;
            }
            
            index = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusBool = buffer[i];
                if (statusBool.ComponentId == componentId && statusBool.Id == Id)
                {
                    index = i;
                    break;
                }
            }

            m_CachedIndex = index;
            return index >= 0;
        }

        public UnmanagedStatusBool(Hash128 id)
        {
            Id = id;
            m_CachedIndex = -1;
        }

        public static implicit operator UnmanagedStatusBool(UnityEngine.Hash128 value) => new UnmanagedStatusBool(value);
        public static implicit operator UnmanagedStatusBool(Hash128 value) => new UnmanagedStatusBool(value);
        public static implicit operator UnmanagedStatusBool(StatusBool value) => new UnmanagedStatusBool(value != null && value.StatusName ? value.StatusName.Id : default);
    }
}
#endif