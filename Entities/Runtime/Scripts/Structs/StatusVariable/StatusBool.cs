#if ENTITIES
using Unity.Burst;
using Unity.Entities;

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
        public int CachedIndex;

        /// <summary>
        /// Attempt to retrieve the buffer index for this <see cref="StatusBool"/> and component id.
        /// </summary>
        /// <returns>The matching index. If none is found then -1.</returns>
        [BurstCompile]
        public readonly int GetBufferIndex(Hash128 componentId, DynamicBuffer<StatusBools> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                var statusFloat = buffer[i];
                if (statusFloat.ComponentId == componentId && statusFloat.Id == Id)
                {
                    return i;
                }
            }

            return -1;
        }

        public StatusBool(Hash128 id)
        {
            Id = id;
            CachedIndex = -1;
        }

        public static implicit operator StatusBool(Hash128 value) => new StatusBool(value);
        public static implicit operator StatusBool(global::StatusEffects.StatusBool value) => new StatusBool(value != null && value.StatusName ? value.StatusName.Id : default);
    }
}
#endif