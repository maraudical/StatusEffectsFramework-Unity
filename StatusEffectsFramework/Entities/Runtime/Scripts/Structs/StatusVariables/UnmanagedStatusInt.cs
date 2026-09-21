#if ENTITIES
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    public struct UnmanagedStatusInt
    {
#if NETCODE
        [GhostField]
#endif
        public Hash128 UniqueKey;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private ushort m_Id;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        private ushort m_Version;
#if NETCODE
        [GhostField(SendData = false)]
#endif
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
            if (m_Version != registry.Version)
            {
                if (Hint.Unlikely(!registry.TryGetId(UniqueKey, out m_Id)))
                {
                    index = -1;
                    UnityEngine.Debug.LogWarning($"{nameof(UnmanagedStatusInt)} with unique key \"{UniqueKey}\" does not exist in the registry.");
                    return false;
                }

                m_Version = registry.Version;
            }

            StatusInts statusInts;
            int length = buffer.Length;
            index = m_CachedIndex;
            if (index >= 0 && index < buffer.Length)
            {
                statusInts = buffer[index];
                if (statusInts.StableTypeHash == stableTypeHash && statusInts.Id == m_Id)
                    return true;
            }

            index = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusInts = buffer[i];
                if (statusInts.StableTypeHash == stableTypeHash && statusInts.Id == m_Id)
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
            m_Id = default;
            m_Version = default;
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
#endif