using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    [BurstCompile]
    public struct ModuleInfo
    {
        public TypeIndex TypeIndex { get; internal set; }
        internal IntPtr Ptr;
        internal int Size;

        /// <summary>
        /// Creates a new <see cref="ModuleInfo"/> for the specified module struct, allocating a copy of it 
        /// to unmanaged memory and associating it with the module's type.
        /// </summary>
        [BurstDiscard]
        public static unsafe ModuleInfo AllocateModule<T>(T moduleStruct) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>();
            void* ptr = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
            UnsafeUtility.CopyStructureToPtr(ref moduleStruct, ptr);
            var moduleInfo = new ModuleInfo
            {
                TypeIndex = TypeManager.GetTypeIndex(typeof(Modules<T>)),
                Ptr = (IntPtr)ptr,
                Size = size,
            };
            return moduleInfo;
        }

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
