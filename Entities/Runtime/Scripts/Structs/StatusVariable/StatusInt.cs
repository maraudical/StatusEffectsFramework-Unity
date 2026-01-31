#if ENTITIES
using Unity.Burst;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    [BurstCompile]
    public struct StatusInt
    {
        public Hash128 Id;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private int m_CachedIndex;
        
        /// <summary>
        /// Attempt to retrieve the <see cref="StatusInts"/> value for this <see cref="StatusInt"/>.
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
        /// Attempt to retrieve the <see cref="StatusInts"/> for this <see cref="StatusInt"/>.
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
        /// Attempt to retrieve the <see cref="StatusInts"/> index value for this <see cref="StatusInt"/>.
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

        public StatusInt(Hash128 id)
        {
            Id = id;
            m_CachedIndex = -1;
        }

        public static implicit operator StatusInt(UnityEngine.Hash128 value) => new StatusInt(value);
        public static implicit operator StatusInt(Hash128 value) => new StatusInt(value);
        public static implicit operator StatusInt(global::StatusEffects.StatusInt value) => new StatusInt(value != null && value.StatusName ? value.StatusName.Id : default);
    }
}
#endif