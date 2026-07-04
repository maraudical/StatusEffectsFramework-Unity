using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace StatusEffectsFramework.Entities
{
    public static class NativeSortExtensions
    {
        /// <summary>
        /// Finds a value in this sorted array by binary search.
        /// </summary>
        /// <remarks>If the array is not sorted, the value might not be found, even if it's present in this array.</remarks>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="array">The array to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>If found, the first index of the located value. If not found, the return value is negative.</returns>
        public static unsafe int BinarySearchFirst<T, V>(this NativeArray<T> array, V value)
            where T : unmanaged, IComparable<V>
        {
            var ptr = (T*)array.GetUnsafeReadOnlyPtr();
            var length = array.Length;
            
            var offset = 0;

            for (var l = length; l != 0; l >>= 1)
            {
                var idx = offset + (l >> 1);
                var curr = ptr[idx];
                var r = curr.CompareTo(value);
                
                if (r < 0)
                {
                    offset = idx + 1;
                    --l;
                }
            }

            return offset < length && array[offset].CompareTo(value) == 0
                ? offset
                : ~offset;
        }
    }
}