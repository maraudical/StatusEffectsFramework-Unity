#if ENTITIES
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    public struct ModuleInfo
    {
        public TypeIndex TypeIndex { get; internal set; }
        internal IntPtr Ptr;
        internal int Size;

        /// <summary>
        /// Creates a new <see cref="ModuleInfo"/> for the specified module struct, allocating a copy of it 
        /// to unmanaged memory and associating it with the module's type.
        /// </summary>
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
    }
}
#endif