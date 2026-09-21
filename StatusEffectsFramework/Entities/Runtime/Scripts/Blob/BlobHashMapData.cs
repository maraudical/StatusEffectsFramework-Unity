#if ENTITIES
using System;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
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

        internal BlobMultiHashMapIterator<TKey> GetValuesForKey(TKey key)
        {
            int bucket = key.GetHashCode() & bucketCapacityMask;
            var it = new BlobMultiHashMapIterator<TKey> { key = key, nextIndex = buckets[bucket] };
            return it;
        }

        internal bool TryGetFirstValue(TKey key, out TValue item, out BlobMultiHashMapIterator<TKey> it)
        {
            it = GetValuesForKey(key);
            return TryGetNextValue(out item, ref it);
        }

        internal ref TValue GetFirstValueRef(TKey key, out BlobMultiHashMapIterator<TKey> it)
        {
            it = GetValuesForKey(key);
            return ref GetNextValueRef(ref it);
        }

        internal ref TValue GetFirstValueRef(TKey key)
        {
            var index = key.GetHashCode() & bucketCapacityMask;
            return ref values[index];
        }

        internal bool TryGetNextValue(out TValue item, ref BlobMultiHashMapIterator<TKey> it)
        {
            int index = it.nextIndex;

            if (!IsValidIndex(it))
            {
                it.nextIndex = -1;
                item = default;
                return false;
            }

            it.nextIndex = next[index];
            item = values[index];
            return true;
        }

        internal ref TValue GetNextValueRef(ref BlobMultiHashMapIterator<TKey> it)
        {
            int index = it.nextIndex;

            if (!IsValidIndex(it))
            {
                it.nextIndex = -1;
                throw new IndexOutOfRangeException($"The next index of \"{index}\" for key \"{it.key}\" is invalid.");
            }

            it.nextIndex = next[index];
            return ref values[index];
        }

        internal bool IsValidIndex(in BlobMultiHashMapIterator<TKey> it)
        {
            return IsValidIndex(it.key, it.nextIndex);
        }

        internal bool IsValidIndex(TKey key, int index)
        {
            if (index < 0 /*|| index >= keyCapacity*/)
            {
                return false;
            }

            while (!keys[index].Equals(key))
            {
                index = next[index];
                if (index < 0 /*|| index >= keyCapacity*/)
                {
                    return false;
                }
            }

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