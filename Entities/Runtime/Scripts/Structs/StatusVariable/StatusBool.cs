#if ENTITIES
using Unity.Burst;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;

#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    [BurstCompile]
    public struct StatusBool
    {
        public Hash128 Id;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private int m_CachedIndex;

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusBools"/> value for this <see cref="StatusBool"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool GetValue(in Hash128 componentId, in DynamicBuffer<StatusBools> buffer, out bool value)
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
        /// Attempt to retrieve the <see cref="StatusBools"/> for this <see cref="StatusBool"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool Get(in Hash128 componentId, in DynamicBuffer<StatusBools> buffer, out StatusBools value)
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
        /// Attempt to retrieve the <see cref="StatusBools"/> index value for this <see cref="StatusBool"/>.
        /// </summary>
        [BurstCompile]
        public int GetIndex(in Hash128 componentId, in DynamicBuffer<StatusBools> buffer)
        {
            StatusBools statusBool;
            int length = buffer.Length;
            if (m_CachedIndex >= 0 && m_CachedIndex < buffer.Length)
            {
                statusBool = buffer[m_CachedIndex];
                if (statusBool.ComponentId == componentId && statusBool.Id == Id)
                    return m_CachedIndex;
            }
            
            m_CachedIndex = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusBool = buffer[i];
                if (statusBool.ComponentId == componentId && statusBool.Id == Id)
                {
                    m_CachedIndex = i;
                    break;
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