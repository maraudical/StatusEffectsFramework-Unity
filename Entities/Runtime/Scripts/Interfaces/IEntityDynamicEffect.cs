using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    /// <summary>
    /// If an effect is dynamic, the associated <see cref="DynamicEffect"/> reference must implement this interface otherwise it will be excluded during runtime conversion.
    /// </summary>
    public interface IEntityDynamicEffect
    {
        /// <summary>
        /// Must return an empty <see cref="IComponentData"/>. If you would like to include values passed by the <see cref="DynamicEffect"/>, see <see cref=""/>
        /// </summary>
        public TypeIndex GetTypeIndex();
        /// <summary>
        /// This optional method  should create the default values for the burstable dynamic effect struct. Make 
        /// sure to return after passing it as a parameter to the <see cref="AllocateDynamicEffect{T}(T)"/> method.
        /// Just return <see cref="default"/> if there is no struct to allocate.
        /// </summary>
        /// /// <remarks>
        /// Make sure to always use the <see cref="AllocateDynamicEffect{T}(T)"/> method to create the 
        /// <see cref="DynamicEffectInfo"/> struct that will be used to store the dynamic effect data. This is required 
        /// to properly allocate unmanaged memory for the dynamic effect struct.
        ///
        /// <code>
        ///public DynamicEffectInfo CreateDynamicEffectInfo()
        ///{
        ///    var myDynamicEffectStruct = new MyDynamicEffectStruct
        ///    {
        ///        // Copy values from the dynamic effect to the struct here
        ///        MyValue = this.MyValue,
        ///    };
        ///    return DynamicEffectInfo.AllocateDynamicEffect(myDynamicEffectStruct);
        ///}
        /// </code>
        /// </remarks>
        public DynamicEffectInfo CreateDynamicEffectInfo();
    }
}
