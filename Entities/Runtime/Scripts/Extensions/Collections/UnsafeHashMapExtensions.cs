using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace StatusEffectFramework.Entities
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
            
            using var keys = data.GetKeyArray(Allocator.Temp);
            using var values = data.GetValueArray<TValue>(Allocator.Temp);

            if (found)
                return ref UnsafeUtility.ArrayElementAsRef<TValue>(data.Ptr, idx);

            return ref UnsafeUtility.AsRef<TValue>(data.Ptr);
        }
    }
}