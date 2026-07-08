#if ENTITIES
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    public struct DynamicEffectInfo
    {
        public TypeIndex TypeIndex { get; internal set; }
        internal IntPtr Ptr;
        internal int Size;

        /// <summary>
        /// Creates a new <see cref="DynamicEffectInfo"/> for the specified dynamic effect struct, allocating a copy of it 
        /// to unmanaged memory.
        /// </summary>
        public static unsafe DynamicEffectInfo AllocateDynamicEffect<T>(T dynamicEffectStruct, ValueType valueType) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>();
            void* ptr = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
            UnsafeUtility.CopyStructureToPtr(ref dynamicEffectStruct, ptr);
            Type type = valueType switch
            {
                ValueType.Int => typeof(DynamicInts<T>),
                ValueType.Bool => typeof(DynamicBools<T>),
                _ => typeof(DynamicFloats<T>),
            };
            var dynamicEffectInfo = new DynamicEffectInfo
            {
                TypeIndex = TypeManager.GetTypeIndex(type),
                Ptr = (IntPtr)ptr,
                Size = size,
            };
            return dynamicEffectInfo;
        }
    }
}
#endif