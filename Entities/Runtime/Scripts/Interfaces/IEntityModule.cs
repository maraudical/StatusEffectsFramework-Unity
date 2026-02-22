#if ENTITIES
using StatusEffects.Modules;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffects.Entities
{
    public interface IEntityModule
    {
        /// <summary>
        /// This method should return a system of type <see cref="ISystem"/> or <see cref="SystemBase"/> 
        /// that will process the module's logic. The <see cref="DisableAutoCreationAttribute"/> should 
        /// be added to that system as module system will be manually created and destroyed when needed.
        /// </summary>
        public Type ModuleSystemType();
        /// <summary>
        /// This method should return the generic buffer <see cref="Modules{T}"/> for your struct.
        /// </summary>
        public Type ModuleType();
        /// <summary>
        /// Using the values in the <see cref="Module"/> and <see cref="ModuleInstance"/> this method 
        /// should create the default values for the burstable module struct. Make sure to return after 
        /// passing it as a parameter to the <see cref="AllocateModule{T}(T)"/> method"/>
        /// </summary>
        /// /// <remarks>
        /// Make sure to always use the <see cref="AllocateModule{T}(T)"/> method to create the 
        /// <see cref="ModuleInfo"/> struct that will be used to store the module data. This is required 
        /// to properly allocate unmanaged memory for the module struct and associate it with the correct 
        /// type information.
        ///
        /// <code>
        ///public ModuleInfo CreateModuleInfo(ModuleInstance moduleInstance)
        ///{
        ///    var myModuleInstance = moduleInstance as MyModuleInstance;
        ///    var myModuleStruct = new MyModuleStruct
        ///    {
        ///        // Copy values from the module instance to the module struct here
        ///        MyValue = testModuleInstance.MyValue,
        ///    };
        ///    return (this as IEntityModule).AllocateModule(testModuleStruct);
        ///}
        /// </code>
        /// </remarks>
        /// <param name="moduleInstance">The <paramref name="moduleInstance"/> can be safely cast into 
        /// the attached <see cref="ModuleInstance"/> defined from the 
        /// <see cref="AttachModuleInstanceAttribute"/></param>
        public ModuleInfo CreateModuleInfo(ModuleInstance moduleInstance);
        /// <summary>
        /// Creates a new <see cref="ModuleInfo"/> for the specified module struct, allocating a copy of it 
        /// to unmanaged memory and associating it with the module's type.
        /// </summary>
        public sealed ModuleInfo AllocateModule<T>(T moduleStruct) where T : struct
        {
            unsafe{
                void* ptr = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
                UnsafeUtility.CopyStructureToPtr(ref moduleStruct, ptr);
                var moduleInfo = new ModuleInfo
                {
                    TypeIndex = TypeManager.GetTypeIndex(ModuleType()),
                    Ptr = (IntPtr)ptr,
                };
                return moduleInfo;
            }
        }
    }
}
#endif