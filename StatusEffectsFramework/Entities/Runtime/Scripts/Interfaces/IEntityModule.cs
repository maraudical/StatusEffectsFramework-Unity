using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// To allow a module to be used with Entities, the <see cref="Module"/> must implement this 
    /// interface. Otherwise it will be excluded during runtime conversion.
    /// </summary>
    public interface IEntityModule
    {
        /// <summary>
        /// Using the values in the <see cref="Module"/> and <see cref="ModuleInstance"/> this method 
        /// should be used to create the default values for the burstable module struct.
        /// </summary>
        /// <remarks>
        /// Make sure to always use the <see cref="ModuleInfo.AllocateModule{T}(T, ref ModuleInfo, ref BlobBuilder)"/> method to create the 
        /// <see cref="ModuleInfo"/> struct that will be used to store the module data. This is required 
        /// to properly allocate the module struct.
        ///
        /// <code>
        ///public void CreateModuleInfo(ModuleInstance moduleInstance, ref ModuleInfo info, ref BlobBuilder builder)
        ///{
        ///    var myModuleInstance = moduleInstance as MyModuleInstance;
        ///    var myModuleStruct = new MyModuleStruct
        ///    {
        ///        // Copy values from the module instance to the module struct here
        ///        MyValue = myModuleInstance.MyValue,
        ///    };
        ///    ModuleInfo.AllocateModule(myModuleStruct, ref info, ref builder);
        ///}
        /// </code>
        ///
        /// With Netcode for Entities, the <c>Modules&lt;T&gt;</c> elements on predicted ghosts are not replicated or
        /// included in the prediction backup. When a snapshot rolls an entity back and its status effects differ
        /// from the previous frame, its module elements are rebuilt from these default values, so any state your
        /// systems write into the module struct (such as a counter of times damaged) is reset. Keep state that has
        /// to survive a rollback in a replicated ghost component, or derive it from replicated ticks such as
        /// <c>TickAdded</c>.
        /// </remarks>
        /// <param name="moduleInstance">The <paramref name="moduleInstance"/> can be safely cast into 
        /// the attached <see cref="ModuleInstance"/> defined from the 
        /// <see cref="AttachModuleInstanceAttribute"/></param>
        public void CreateModuleInfo(ModuleInstance moduleInstance, ref ModuleInfo info, ref BlobBuilder builder);
    }
}