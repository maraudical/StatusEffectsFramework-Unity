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
    public struct UnmanagedStatusFloat
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
            if (m_Version != registry.Version)
            {
                if (Hint.Unlikely(!registry.TryGetId(UniqueKey, out m_Id)))
                {
                    index = -1;
                    UnityEngine.Debug.LogWarning($"{nameof(UnmanagedStatusFloat)} with unique key \"{UniqueKey}\" does not exist in the registry.");
                    return false;
                }

                m_Version = registry.Version;
            }

            StatusFloats statusFloat;
            int length = buffer.Length;
            index = m_CachedIndex;
            if (index >= 0 && index < buffer.Length)
            {
                statusFloat = buffer[index];
                if (statusFloat.StableTypeHash == stableTypeHash && statusFloat.Id == m_Id)
                    return true;
            }

            index = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusFloat = buffer[i];
                if (statusFloat.StableTypeHash == stableTypeHash && statusFloat.Id == m_Id)
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
            m_Id = default;
            m_Version = default;
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
#endif