#if ENTITIES && ADDRESSABLES
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#define BLOBHASHMAP_SAFE
#endif

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    /// <summary>
    /// A read only hashmap that can be used inside a blob asset
    /// </summary>
    [MayOnlyLiveInBlobStorage]
    public struct BlobHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal BlobHashMapData<TKey, TValue> data;

        /// <summary>
        /// Retrieve a value by key
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                    return value;

#if BLOBHASHMAP_SAFE
                throw new KeyNotFoundException($"Key: {key} is not present in the BlobHashMap.");
#else
                 return default;
#endif
            }
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="item">If key is found item parameter will contain value</param>
        /// <returns>Returns true if key is found, otherwise returns false.</returns>
        public bool TryGetValue(TKey key, out TValue item) => data.TryGetFirstValue(key, out item, out _);

        /// <summary>
        /// Determines whether an key is in the container.
        /// </summary>
        /// <param name="key">The key to locate in the container.</param>
        /// <returns>Returns true if the container contains the key.</returns>
        public bool ContainsKey(TKey key) => TryGetValue(key, out _);

        /// <summary>
        /// The current number of items in the container
        /// </summary>
        public int Count => data.count[0];

        /// <summary>
        /// Returns an array containing all of the keys in this container
        /// </summary>
        public NativeArray<TKey> GetKeyArray(Allocator allocator) => data.GetKeys(allocator);

        /// <summary>
        /// Returns an array containing all of the values in this container
        /// </summary>
        public NativeArray<TValue> GetValueArray(Allocator allocator) => data.GetValues(allocator);
    }

    public ref struct BlobBuilderHashMap<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal BlobBuilderHashMapData<TKey, TValue> data;

        internal BlobBuilderHashMap(int capacity, int bucketCapacityRatio, ref BlobBuilder blobBuilder, ref BlobHashMapData<TKey, TValue> data)
        {
#if BLOBHASHMAP_SAFE
            if (capacity <= 0)
                throw new ArgumentException("Must be greater than zero", nameof(capacity));
            if (bucketCapacityRatio <= 0)
                throw new ArgumentException("Must be greater than zero", nameof(bucketCapacityRatio));
#endif

            this.data = new BlobBuilderHashMapData<TKey, TValue>(capacity, bucketCapacityRatio, ref blobBuilder, ref data);
        }

        public void Add(TKey key, TValue item)
        {

#if BLOBHASHMAP_SAFE
            if (!data.TryAdd(key, item, false))
                throw new ArgumentException($"An item with key {key} already exists", nameof(key));
#else
            TryAdd(key, item);
#endif
        }

        public bool TryAdd(TKey key, TValue value) => data.TryAdd(key, value, false);
        public bool ContainsKey(TKey key) => data.ContainsKey(key);
        public int Capacity => data.keyCapacity;
        public int Count => data.Count;
    }
}
#endif