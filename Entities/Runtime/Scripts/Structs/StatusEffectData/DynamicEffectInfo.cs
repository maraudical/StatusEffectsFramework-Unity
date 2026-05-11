using System;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace StatusEffectFramework.Entities
{
    [BurstCompile]
    public struct DynamicEffectInfo
    {
        internal IntPtr Ptr;
        internal int Size;

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
