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
        /// <summary>
        /// Only use the cached index if there have not been any structural changes 
        /// to the <see cref="StatusInts"/> buffer since the last time this index 
        /// was set.
        /// </summary>
        public int CachedIndex => m_CachedIndex;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private int m_CachedIndex;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private int m_CachedLength;

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusInts"/> value for this <see cref="StatusInt"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool GetValue(Hash128 componentId, DynamicBuffer<StatusInts> buffer, out int value, bool structuralChange = false)
        {
            GetIndex(componentId, buffer, structuralChange);

            if (m_CachedIndex >= 0)
            {
                value = buffer[m_CachedIndex].Value;
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
        public bool Get(Hash128 componentId, DynamicBuffer<StatusInts> buffer, out StatusInts value, bool structuralChange = false)
        {
            GetIndex(componentId, buffer, structuralChange);

            if (m_CachedIndex >= 0)
            {
                value = buffer[m_CachedIndex];
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusInts"/> index value for this <see cref="StatusInt"/>.
        /// </summary>
        [BurstCompile]
        public int GetIndex(Hash128 componentId, DynamicBuffer<StatusInts> buffer, bool structuralChange = false)
        {
            if (structuralChange || m_CachedIndex < 0 || m_CachedLength != buffer.Length)
            {
                m_CachedIndex = -1;

                for (int i = 0; i < buffer.Length; i++)
                {
                    var statusInt = buffer[i];
                    if (statusInt.ComponentId == componentId && statusInt.Id == Id)
                    {
                        m_CachedIndex = i;
                        m_CachedLength = buffer.Length;
                        break;
                    }
                }
            }
            
            return m_CachedIndex;
        }

        public StatusInt(Hash128 id)
        {
            Id = id;
            m_CachedIndex = -1;
            m_CachedLength = default;
        }

        public static implicit operator StatusInt(UnityEngine.Hash128 value) => new StatusInt(value);
        public static implicit operator StatusInt(Hash128 value) => new StatusInt(value);
        public static implicit operator StatusInt(global::StatusEffects.StatusInt value) => new StatusInt(value != null && value.StatusName ? value.StatusName.Id : default);
    }
}
#endif