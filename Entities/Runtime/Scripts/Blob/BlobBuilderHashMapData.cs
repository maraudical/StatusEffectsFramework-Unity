#if ENTITIES
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#define BLOBHASHMAP_SAFE
#endif

using System;
using Unity.Entities;
using Unity.Mathematics;

namespace StatusEffects.Entities
{
    internal ref struct BlobBuilderHashMapData<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        private BlobBuilderArray<TValue> values;
        private BlobBuilderArray<TKey> keys;
        private BlobBuilderArray<int> next;
        private BlobBuilderArray<int> buckets;
        private BlobBuilderArray<int> count;

        // we store these values in the builder because we cannot access BlobHashMapData
        // itself (it must live in blob storage)
        //private int allocatedIndexLength;
        private int bucketCapacityMask;
        internal int keyCapacity;

        internal bool TryAdd(TKey key, TValue item, bool multi)
        {
            ref int c = ref count[0];

#if BLOBHASHMAP_SAFE
            if (c >= keyCapacity)
                throw new InvalidOperationException("HashMap is full");
#endif

            int bucket = key.GetHashCode() & bucketCapacityMask;

            if (!multi && ContainsKey(bucket, key))
                return false;


            int index = c++;
            keys[index] = key;
            values[index] = item;
            next[index] = buckets[bucket];
            buckets[bucket] = index;
            return true;
        }

        internal bool ContainsKey(TKey key)
        {
            int bucket = key.GetHashCode() & bucketCapacityMask;
            return ContainsKey(bucket, key);
        }

        // Safety check for regular hashmap Add
        internal bool ContainsKey(int bucket, TKey key)
        {
            int index = buckets[bucket];
            if (index < 0)
                return false;

            while (!keys[index].Equals(key))
            {
                index = next[index];
                if (index < 0)
                    return false;
            }

            return true;
        }

        internal int Count => count[0];

        internal void Clear()
        {
            for (int i = 0; i < buckets.Length; i++)
                buckets[i] = -1;
            for (int i = 0; i < next.Length; i++)
                next[i] = -1;
        }

        internal BlobBuilderHashMapData(int capacity, int bucketCapacityRatio, ref BlobBuilder blobBuilder, ref BlobHashMapData<TKey, TValue> data)
        {
            int bucketCapacity = math.ceilpow2(capacity * bucketCapacityRatio);

            // bucketCapacityMask is neccessary for retrieval so set it on the data too
            this.bucketCapacityMask = data.bucketCapacityMask = bucketCapacity - 1;
            this.keyCapacity = capacity;

            values = blobBuilder.Allocate(ref data.values, capacity);
            keys = blobBuilder.Allocate(ref data.keys, capacity);
            next = blobBuilder.Allocate(ref data.next, capacity);
            buckets = blobBuilder.Allocate(ref data.buckets, bucketCapacity);

            // so far the only way I've found to modify the true count on the data itself (without using unsafe code)
            // is by storing it in an array we can still access in the Add method.
            // count is only used in GetKeyArray and GetValueArray to size the array to the true count instead of capacity
            // count and keyCapacity are like
            count = blobBuilder.Allocate(ref data.count, 1);

            Clear();
        }
    }
}
#endif