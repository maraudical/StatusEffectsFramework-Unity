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
    public struct UnmanagedStatusBool
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
            if (m_Version != registry.Version)
            {
                m_Version = registry.Version;
                m_CachedIndex = -1;

                if (Hint.Unlikely(!registry.TryGetId(UniqueKey, out ushort id)))
                {
                    m_Id = StatusRegistry.NullId;
                    UnityEngine.Debug.LogWarning($"{nameof(UnmanagedStatusBool)} with unique key \"{UniqueKey}\" does not exist in the registry.");
                }
                else
                    m_Id = id;
            }

            if (Hint.Unlikely(m_Id == StatusRegistry.NullId))
            {
                index = -1;
                return false;
            }

            StatusBools statusBool;
            int length = buffer.Length;
            index = m_CachedIndex;
            if (index >= 0 && index < buffer.Length)
            {
                statusBool = buffer[index];
                if (statusBool.StableTypeHash == stableTypeHash && statusBool.Id == m_Id)
                    return true;
            }
            
            index = -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                statusBool = buffer[i];
                if (statusBool.StableTypeHash == stableTypeHash && statusBool.Id == m_Id)
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
            m_Id = StatusRegistry.NullId;
            m_Version = default;
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