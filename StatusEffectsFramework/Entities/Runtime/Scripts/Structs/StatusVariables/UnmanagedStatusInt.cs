using Unity.Assertions;
using Unity.Burst;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    public struct UnmanagedStatusInt
    {
        public Hash128 UniqueKey;
        private int m_CachedIndex;

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusInts"/> value for this <see cref="UnmanagedStatusInt"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetValue(ulong stableTypeHash, in UnmanagedStatusRegistry registry, in DynamicBuffer<StatusInts> buffer, out int value)
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
        /// Attempt to retrieve the <see cref="StatusInts"/> for this <see cref="UnmanagedStatusInt"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetElement(ulong stableTypeHash, in UnmanagedStatusRegistry registry, in DynamicBuffer<StatusInts> buffer, out StatusInts value)
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
        /// Attempt to retrieve the <see cref="StatusInts"/> index value for this <see cref="UnmanagedStatusInt"/>.
        /// </summary>
        [BurstCompile]
        public bool TryGetIndex(ulong stableTypeHash, in UnmanagedStatusRegistry registry, in DynamicBuffer<StatusInts> buffer, out int index)
        {
            StatusInts statusInts;
            index = m_CachedIndex;
            if (index >= 0 && index < buffer.Length)
            {
                statusInts = buffer[index];
                if (statusInts.StableTypeHash == stableTypeHash && statusInts.UniqueKey == UniqueKey)
                    return true;
            }

            index = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusInts = buffer[i];
                if (statusInts.StableTypeHash == stableTypeHash && statusInts.UniqueKey == UniqueKey)
                {
                    index = i;
                    break;
                }
            }

            m_CachedIndex = index;
            return index >= 0;
        }

        public UnmanagedStatusInt(Hash128 uniqueKey)
        {
            UniqueKey = uniqueKey;
            m_CachedIndex = -1;
        }

        public static implicit operator UnmanagedStatusInt(StatusInt statusInt)
        {
            Assert.IsNotNull(statusInt, $"{nameof(StatusInt)} cannot be null when casting to an {nameof(UnmanagedStatusInt)}.");
            Assert.IsNotNull(statusInt.StatusName, $"{nameof(StatusInt.StatusName)} cannot be null when casting to an {nameof(UnmanagedStatusInt)}.");

            return new UnmanagedStatusInt(statusInt.StatusName.GetUniqueKeyHash());
        }
}
}