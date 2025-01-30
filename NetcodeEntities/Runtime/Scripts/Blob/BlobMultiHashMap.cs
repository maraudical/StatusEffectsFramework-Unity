#if ENTITIES && ADDRESSABLES
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#define BLOBHASHMAP_SAFE
#endif

using System;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    /// <summary>
    /// A read only multihashmap that can be used as inside blob asset
    /// </summary>
    [MayOnlyLiveInBlobStorage]
    public struct BlobMultiHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal BlobHashMapData<TKey, TValue> data;

        /// <summary>
        /// Retrieve iterator for the first value for the key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">Output value.</param>
        /// <param name="it">Iterator.</param>
        /// <returns>Returns true if the container contains the key.</returns>
        public bool TryGetFirstValue(TKey key, out TValue item, out BlobMultiHashMapIterator<TKey> it) =>
            data.TryGetFirstValue(key, out item, out it);

        /// <summary>
        /// Retrieve iterator to the next value for the key.
        /// </summary>
        /// <param name="item">Output value.</param>
        /// <param name="it">Iterator.</param>
        /// <returns>Returns true if next value for the key is found.</returns>
        public bool TryGetNextValue(out TValue item, ref BlobMultiHashMapIterator<TKey> it) =>
            data.TryGetNextValue(out item, ref it);

        /// <summary>
        /// The current number of items in the container
        /// </summary>
        public int Count => data.count[0];

        /// <summary>
        /// Determines whether an key is in the container.
        /// </summary>
        /// <param name="key">The key to locate in the container.</param>
        /// <returns>Returns true if the container contains the key.</returns>
        public bool ContainsKey(TKey key) => data.TryGetFirstValue(key, out _, out _);

        /// <summary>
        /// Returns array populated with keys.
        /// </summary>
        /// <remarks>Number of returned keys will match number of values in the container. If key contains multiple values it will appear number of times
        /// how many values are associated to the same key.</remarks>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Array of keys.</returns>
        public NativeArray<TKey> GetKeyArray(Allocator allocator) => data.GetKeys(allocator);


        /// <summary>
        /// Returns array populated with values.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>Array of values.</returns>
        public NativeArray<TValue> GetValueArray(Allocator allocator) => data.GetValues(allocator);
    }

    public ref struct BlobBuilderMultiHashMap<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal BlobBuilderHashMapData<TKey, TValue> data;

        internal BlobBuilderMultiHashMap(int capacity, int bucketCapacityRatio, ref BlobBuilder blobBuilder, ref BlobHashMapData<TKey, TValue> data)
        {
#if BLOBHASHMAP_SAFE
            if (capacity <= 0)
                throw new ArgumentException("Must be greater than zero", nameof(capacity));
            if (bucketCapacityRatio <= 0)
                throw new ArgumentException("Must be greater than zero", nameof(bucketCapacityRatio));
#endif

            this.data = new BlobBuilderHashMapData<TKey, TValue>(capacity, bucketCapacityRatio, ref blobBuilder, ref data);
        }

        public void Add(TKey key, TValue item) => data.TryAdd(key, item, true);
        public int Capacity => data.keyCapacity;
        public int Count => data.Count;
    }

    public struct BlobMultiHashMapIterator<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        internal TKey key;
        internal int nextIndex;
    }
}
#endif