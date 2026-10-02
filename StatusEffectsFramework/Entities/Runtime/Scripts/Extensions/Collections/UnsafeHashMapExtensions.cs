using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace StatusEffectsFramework.Entities
{
    public static class UnsafeHashMapExtensions
    {
        /// <summary>
        /// Returns a reference to the value of <paramref name="key"/>, which is only valid when <paramref name="found"/>
        /// is true and until the hash map is next modified.
        /// </summary>
        /// <remarks>
        /// Takes the hash map by <see langword="in"/> since it only reads, so it also works on read-only variables
        /// such as <see langword="using"/> locals. The value itself is still writable through the returned reference.
        /// </remarks>
        public unsafe static ref TValue GetValueRefOrNullRef<TKey, TValue>(
            this in UnsafeHashMap<TKey, TValue> hashMap,
            in TKey key,
            out bool found)
            where TValue : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
        {
            ref readonly var data = ref hashMap.m_Data;
            int idx = data.Find(key);
            found = idx != -1;

            if (found)
                return ref UnsafeUtility.ArrayElementAsRef<TValue>(data.Ptr, idx);

            return ref UnsafeUtility.AsRef<TValue>(null);
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