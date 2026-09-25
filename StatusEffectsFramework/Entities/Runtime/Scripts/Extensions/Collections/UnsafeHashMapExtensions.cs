using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace StatusEffectsFramework.Entities
{
    public static class UnsafeHashMapExtensions
    {
        public unsafe static ref TValue TryGetValueByRef<TKey, TValue>(
            this ref UnsafeHashMap<TKey, TValue> hashMap,
            in TKey key,
            out bool found)
            where TValue : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
        {
            ref var data = ref hashMap.m_Data;
            int idx = data.Find(key);
            found = idx != -1;

            if (found)
                return ref UnsafeUtility.ArrayElementAsRef<TValue>(data.Ptr, idx);

            return ref UnsafeUtility.AsRef<TValue>(data.Ptr);
        }

        /// <summary>
        /// Adds a new key and returns a reference to its value so it can be written in place. The value is
        /// uninitialized so it must be assigned. The reference is only valid until the hash map is next modified.
        /// </summary>
        /// <remarks>
        /// Throws if the key already exists when collection checks are enabled. Otherwise the reference to the
        /// existing value is returned.
        /// </remarks>
        public unsafe static ref TValue AddByRef<TKey, TValue>(
            this ref UnsafeHashMap<TKey, TValue> hashMap,
            in TKey key)
            where TValue : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
        {
            ref var data = ref hashMap.m_Data;
            int idx = data.TryAdd(key);

            if (idx == -1)
            {
                ThrowKeyAlreadyAdded();
                idx = data.Find(key);
            }

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(data.Ptr, idx);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowKeyAlreadyAdded()
        {
            throw new ArgumentException("An item with the same key has already been added.");
        }
    }
}