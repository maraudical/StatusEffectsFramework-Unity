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
        public bool GetValue(in Hash128 componentId, in DynamicBuffer<StatusInts> buffer, out int value)
        {
            int index = GetIndex(componentId, buffer);

            if (index >= 0)
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
        public bool Get(in Hash128 componentId, in DynamicBuffer<StatusInts> buffer, out StatusInts value)
        {
            int index = GetIndex(componentId, buffer);

            if (index >= 0)
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
        public int GetIndex(in Hash128 componentId, in DynamicBuffer<StatusInts> buffer)
        {
            StatusInts statusInt;
            int length = buffer.Length;
            if (m_CachedIndex >= 0 && m_CachedIndex < buffer.Length)
            {
                statusInt = buffer[m_CachedIndex];
                if (statusInt.ComponentId == componentId && statusInt.Id == Id)
                    return m_CachedIndex;
            }

            m_CachedIndex = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusInt = buffer[i];
                if (statusInt.ComponentId == componentId && statusInt.Id == Id)
                {
                    m_CachedIndex = i;
                    break;
                }
            }

            return m_CachedIndex;
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