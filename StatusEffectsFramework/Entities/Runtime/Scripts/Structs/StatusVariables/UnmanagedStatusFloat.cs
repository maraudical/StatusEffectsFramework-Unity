using Unity.Assertions;
using Unity.Burst;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    public struct UnmanagedStatusFloat
    {
        public Hash128 UniqueKey;
        private int m_CachedIndex;

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusFloats"/> value for this <see cref="UnmanagedStatusFloat"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetValue(ulong stableTypeHash, in UnmanagedStatusRegistry registry, in DynamicBuffer<StatusFloats> buffer, out float value)
        {
            if (TryGetIndex(stableTypeHash, registry, buffer, out int index))
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
        public bool TryGetElement(ulong stableTypeHash, in UnmanagedStatusRegistry registry, in DynamicBuffer<StatusFloats> buffer, out StatusFloats value)
        {
            if (TryGetIndex(stableTypeHash, registry, buffer, out int index))
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
        public bool TryGetIndex(ulong stableTypeHash, in UnmanagedStatusRegistry registry, in DynamicBuffer<StatusFloats> buffer, out int index)
        {
            StatusFloats statusFloat;
            index = m_CachedIndex;
            if (index >= 0 && index < buffer.Length)
            {
                statusFloat = buffer[index];
                if (statusFloat.StableTypeHash == stableTypeHash && statusFloat.UniqueKey == UniqueKey)
                    return true;
            }

            index = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusFloat = buffer[i];
                if (statusFloat.StableTypeHash == stableTypeHash && statusFloat.UniqueKey == UniqueKey)
                {
                    index = i;
                    break;
                }
            }

            m_CachedIndex = index;
            return index >= 0;
        }

        public UnmanagedStatusFloat(Hash128 uniqueKey)
        {
            UniqueKey = uniqueKey;
            m_CachedIndex = -1;
        }

        public static implicit operator UnmanagedStatusFloat(StatusFloat statusFloat)
        {
            Assert.IsNotNull(statusFloat, $"{nameof(StatusFloat)} cannot be null when casting to an {nameof(UnmanagedStatusFloat)}.");
            Assert.IsNotNull(statusFloat.StatusName, $"{nameof(StatusFloat.StatusName)} cannot be null when casting to an {nameof(UnmanagedStatusFloat)}.");

            return new UnmanagedStatusFloat(statusFloat.StatusName.GetUniqueKeyHash());
        }
    }
}