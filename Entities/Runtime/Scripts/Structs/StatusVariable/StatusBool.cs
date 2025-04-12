#if ENTITIES
using Unity.Burst;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    [BurstCompile]
    public struct StatusBool
    {
        public Hash128 Id;
        /// <summary>
        /// Only use the cached index if there have not been any structural changes 
        /// to the <see cref="StatusBools"/> buffer since the last time this index 
        /// was set. You must manually set the value.
        /// </summary>
        public int CachedIndex => m_CachedIndex;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private int m_CachedIndex;

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusBools"/> value for this <see cref="StatusBool"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool GetValue(Hash128 componentId, DynamicBuffer<StatusBools> buffer, out bool value, bool structuralChange = false)
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
        /// Attempt to retrieve the <see cref="StatusBools"/> for this <see cref="StatusBool"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool Get(Hash128 componentId, DynamicBuffer<StatusBools> buffer, out StatusBools value, bool structuralChange = false)
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
        /// Attempt to retrieve the <see cref="StatusBools"/> index value for this <see cref="StatusBool"/>.
        /// </summary>
        [BurstCompile]
        public int GetIndex(Hash128 componentId, DynamicBuffer<StatusBools> buffer, bool structuralChange = false)
        {
            if (structuralChange || m_CachedIndex < 0)
            {
                m_CachedIndex = -1;

                for (int i = 0; i < buffer.Length; i++)
                {
                    var statusBool = buffer[i];
                    if (statusBool.ComponentId == componentId && statusBool.Id == Id)
                    {
                        m_CachedIndex = i;
                        break;
                    }
                }
            }

            return m_CachedIndex;
        }

        public StatusBool(Hash128 id)
        {
            Id = id;
            m_CachedIndex = -1;
        }

        public static implicit operator StatusBool(UnityEngine.Hash128 value) => new StatusBool(value);
        public static implicit operator StatusBool(Hash128 value) => new StatusBool(value);
        public static implicit operator StatusBool(global::StatusEffects.StatusBool value) => new StatusBool(value != null && value.StatusName ? value.StatusName.Id : default);
    }
}
#endif