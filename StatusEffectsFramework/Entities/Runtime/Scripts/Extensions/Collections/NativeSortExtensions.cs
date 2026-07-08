using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace StatusEffectsFramework.Entities
{
    public static class NativeSortExtensions
    {
        /// <summary>
        /// Finds the first value in this sorted array by binary search.
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
            
            return BinarySearchFirst(ptr, length, value);
        }

        /// <summary>
        /// Finds the first value in this sorted array by binary search.
        /// </summary>
        /// <remarks>If the array is not sorted, the value might not be found, even if it's present in this array.</remarks>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="ptr">The array ptr to search.</param>
        /// <param name="length">The length of the array.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>If found, the first index of the located value. If not found, the return value is negative.</returns>
        public static unsafe int BinarySearchFirst<T, V>(T* ptr, int length, V value)
            where T : unmanaged, IComparable<V>
        {
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

            return offset < length && ptr[offset].CompareTo(value) == 0
                ? offset
                : ~offset;
        }

        /// <summary>
        /// Finds the last value in this sorted array by binary search.
        /// </summary>
        /// <remarks>If the array is not sorted, the value might not be found, even if it's present in this array.</remarks>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="array">The array to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>If found, the last index of the located value. If not found, the return value is negative.</returns>
        public static unsafe int BinarySearchLast<T, V>(this NativeArray<T> array, V value)
            where T : unmanaged, IComparable<V>
        {
            var ptr = (T*)array.GetUnsafeReadOnlyPtr();
            var length = array.Length;

            return BinarySearchLast(ptr, length, value);
        }

        /// <summary>
        /// Finds the last value in this sorted array by binary search.
        /// </summary>
        /// <remarks>If the array is not sorted, the value might not be found, even if it's present in this array.</remarks>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="ptr">The array ptr to search.</param>
        /// <param name="length">The length of the array.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>If found, the last index of the located value. If not found, the return value is negative.</returns>
        public static unsafe int BinarySearchLast<T, V>(T* ptr, int length, V value)
            where T : unmanaged, IComparable<V>
        {
            var offset = 0;

            for (var l = length; l != 0; l >>= 1)
            {
                var idx = offset + (l >> 1);
                var curr = ptr[idx];
                var r = curr.CompareTo(value);

                if (r <= 0)
                {
                    offset = idx + 1;
                    --l;
                }
            }

            var last = offset - 1;
            
            return last >= 0 && ptr[last].CompareTo(value) == 0
                ? last
                : ~offset;
        }
    }
}