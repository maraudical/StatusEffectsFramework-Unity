using System;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static class UnsafeHashMapAsRefExtensions
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
    }
}