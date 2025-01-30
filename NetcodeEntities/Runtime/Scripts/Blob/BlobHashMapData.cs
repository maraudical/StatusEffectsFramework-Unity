#if ENTITIES && ADDRESSABLES
using System;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    /*
     * Basically the same implementation as Unity's NativeHashMap, except it uses BlobArray
     * and only provides read functionality
     */

    internal struct BlobHashMapData<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal BlobArray<TValue> values;
        internal BlobArray<TKey> keys;
        internal BlobArray<int> next;
        internal BlobArray<int> buckets;
        internal BlobArray<int> count; // only contains a single element containing the true count (set by builder)

        internal int bucketCapacityMask; // == buckets.Length - 1

        internal bool TryGetFirstValue(TKey key, out TValue item, out BlobMultiHashMapIterator<TKey> it)
        {
            it.key = key;

            // ReSharper disable once Unity.BurstAccessingManagedMethod
            int bucket = key.GetHashCode() & bucketCapacityMask;
            it.nextIndex = buckets[bucket];
            return TryGetNextValue(out item, ref it);
        }

        internal bool TryGetNextValue(out TValue item, ref BlobMultiHashMapIterator<TKey> it)
        {
            int index = it.nextIndex;
            it.nextIndex = -1;
            item = default;

            if (index < 0 /*|| index >= keyCapacity*/)
            {
                return false;
            }

            while (!keys[index].Equals(it.key))
            {
                index = next[index];
                if (index < 0 /*|| index >= keyCapacity*/)
                {
                    return false;
                }
            }

            it.nextIndex = next[index];
            item = values[index];
            return true;
        }

        /*
         * Note that the following methods only work correctly because there is no Remove functionality on the builders
         * If there were then there could be gaps in the key and value arrays
         * This can be optimized to just a memcpy but that would require using unsafe code
         */

        internal NativeArray<TKey> GetKeys(Allocator allocator)
        {
            int length = count[0];
            var arr = new NativeArray<TKey>(length, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < length; i++)
                arr[i] = keys[i];
            return arr;
        }

        internal NativeArray<TValue> GetValues(Allocator allocator)
        {
            int length = count[0];
            var arr = new NativeArray<TValue>(length, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < length; i++)
                arr[i] = values[i];
            return arr;
        }
    }
}
#endif