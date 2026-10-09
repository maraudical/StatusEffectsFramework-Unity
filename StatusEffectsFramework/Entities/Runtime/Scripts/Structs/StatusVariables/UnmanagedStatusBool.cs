using Unity.Assertions;
using Unity.Burst;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    public struct UnmanagedStatusBool
    {
        public Hash128 UniqueKey;
        private int m_CachedIndex;

        /// <summary>
        /// Attempt to retrieve the <see cref="StatusBools"/> value for this <see cref="UnmanagedStatusBool"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetValue(ulong stableTypeHash, in UnmanagedStatusRegistry registry, in DynamicBuffer<StatusBools> buffer, out bool value)
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
        /// Attempt to retrieve the <see cref="StatusBools"/> for this <see cref="UnmanagedStatusBool"/>.
        /// </summary>
        /// <returns>True if a matching index was found.</returns>
        [BurstCompile]
        public bool TryGetElement(ulong stableTypeHash, in UnmanagedStatusRegistry registry, in DynamicBuffer<StatusBools> buffer, out StatusBools value)
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
        /// Attempt to retrieve the <see cref="StatusBools"/> index value for this <see cref="UnmanagedStatusBool"/>.
        /// </summary>
        [BurstCompile]
        public bool TryGetIndex(ulong stableTypeHash, in UnmanagedStatusRegistry registry, in DynamicBuffer<StatusBools> buffer, out int index)
        {
            StatusBools statusBool;
            index = m_CachedIndex;
            if (index >= 0 && index < buffer.Length)
            {
                statusBool = buffer[index];
                if (statusBool.StableTypeHash == stableTypeHash && statusBool.UniqueKey == UniqueKey)
                    return true;
            }
            
            index = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusBool = buffer[i];
                if (statusBool.StableTypeHash == stableTypeHash && statusBool.UniqueKey == UniqueKey)
                {
                    index = i;
                    break;
                }
            }

            m_CachedIndex = index;
            return index >= 0;
        }

        public UnmanagedStatusBool(Hash128 uniqueKey)
        {
            UniqueKey = uniqueKey;
            m_CachedIndex = -1;
        }

        public static implicit operator UnmanagedStatusBool(StatusBool statusBool)
        {
            Assert.IsNotNull(statusBool, $"{nameof(StatusBool)} cannot be null when casting to an {nameof(UnmanagedStatusBool)}.");
            Assert.IsNotNull(statusBool.StatusName, $"{nameof(StatusBool.StatusName)} cannot be null when casting to an {nameof(UnmanagedStatusBool)}.");

            return new UnmanagedStatusBool(statusBool.StatusName.GetUniqueKeyHash());
        }
    }
}