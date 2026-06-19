using System;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace StatusEffectFramework.Entities
{
    [BurstCompile]
    public struct DynamicEffectInfo
    {
        internal IntPtr Ptr;
        internal int Size;

        /// <summary>
        /// Creates a new <see cref="DynamicEffectInfo"/> for the specified dynamic effect struct, allocating a copy of it 
        /// to unmanaged memory.
        /// </summary>
        public static unsafe DynamicEffectInfo AllocateDynamicEffect<T>(T dynamicEffectStruct) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>();
            void* ptr = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
            UnsafeUtility.CopyStructureToPtr(ref dynamicEffectStruct, ptr);
            var dynamicEffectInfo = new DynamicEffectInfo
            {
                Ptr = (IntPtr)ptr,
                Size = size,
            };
            return dynamicEffectInfo;
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public T GetValue<T>() where T : unmanaged
        {
            if (Hint.Unlikely(Ptr == IntPtr.Zero))
                throw new ArgumentException($"Dynamic effect struct has not been allocated! Make sure to implement the IEntityDynamicEffect virtual method <b>\"CreateDynamicEffectInfo\"</b> before attempting to retrive it.");

            if (Hint.Unlikely(Size != UnsafeUtility.SizeOf<T>()))
                throw new ArgumentException($"Invalid type {typeof(T)} used to retrieve dynamic effect value struct!");

            unsafe
            {
                UnsafeUtility.CopyPtrToStructure(Ptr.ToPointer(), out T value);
                return value;
            }
        }
    }
}
