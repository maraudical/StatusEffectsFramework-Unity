using System;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    [BurstCompile]
    public struct ModuleInfo
    {
        public TypeIndex TypeIndex;
        public IntPtr Ptr;
        public int Size;

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public T GetValue<T>() where T : unmanaged
        {
            if (Hint.Unlikely(Size != UnsafeUtility.SizeOf<T>()))
                throw new ArgumentException($"Invalid type {typeof(T)}! The type parameter for the {nameof(GetValue)} method should be the same type parameter used in {TypeIndex.ToFixedString()}.");

            unsafe
            {
                UnsafeUtility.CopyPtrToStructure(Ptr.ToPointer(), out T value);
                return value;
            }
        }
    }
}
