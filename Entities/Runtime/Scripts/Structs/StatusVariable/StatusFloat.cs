#if ENTITIES
using Unity.Burst;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    [BurstCompile]
    public struct StatusFloat
    {
        public Hash128 Id;
        /// <summary>
        /// Only use the cached index if there have not been any structural changes 
        /// to the <see cref="StatusFloats"/> buffer since the last time this index 
        /// was set. You must manually set the value.
        /// </summary>
        public int CachedIndex => m_CachedIndex;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private int m_CachedIndex;

        /// <summary>
        /// Attempt to retrieve the <see cref="StatuFloats"/> value for this <see cref="StatusFloat"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool GetValue(Hash128 componentId, DynamicBuffer<StatusFloats> buffer, out float value, bool structuralChange = false)
        {
            int index = GetIndex(componentId, buffer, structuralChange);

            if (index >= 0)
            {
                value = buffer[index].Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusFloats"/> for this <see cref="StatusFloat"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool Get(Hash128 componentId, DynamicBuffer<StatusFloats> buffer, out StatusFloats value, bool structuralChange = false)
        {
            int index = GetIndex(componentId, buffer, structuralChange);

            if (index >= 0)
            {
                value = buffer[index];
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusFloats"/> index value for this <see cref="StatusFloat"/>.
        /// </summary>
        [BurstCompile]
        public int GetIndex(Hash128 componentId, DynamicBuffer<StatusFloats> buffer, bool structuralChange = false)
        {
            if (structuralChange || m_CachedIndex < 0)
            {
                m_CachedIndex = -1;

                for (int i = 0; i < buffer.Length; i++)
                {
                    var statusFloat = buffer[i];
                    if (statusFloat.ComponentId == componentId && statusFloat.Id == Id)
                    {
                        m_CachedIndex = i;
                        break;
                    }
                }
            }

            return m_CachedIndex;
        }

        public StatusFloat(Hash128 id)
        {
            Id = id;
            m_CachedIndex = -1;
        }

        public static implicit operator StatusFloat(UnityEngine.Hash128 value) => new StatusFloat(value);
        public static implicit operator StatusFloat(Hash128 value) => new StatusFloat(value);
        public static implicit operator StatusFloat(global::StatusEffects.StatusFloat value) => new StatusFloat(value != null && value.StatusName ? value.StatusName.Id : default);
    }
}
#endif