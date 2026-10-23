using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// If an effect is dynamic, the associated <see cref="DynamicEffect"/> reference must implement this interface otherwise it will be excluded during runtime conversion.
    /// </summary>
    public interface IEntityDynamicEffect
    {
        /// <summary>
        /// This method should be used to create the default values for the burstable dynamic effect struct.
        /// </summary>
        /// <remarks>
        /// Make sure to always use the <see cref="DynamicEffectInfo.AllocateDynamicEffect{T}(T, ValueType, ref DynamicEffectInfo, ref BlobBuilder)"/> method to create the 
        /// <see cref="DynamicEffectInfo"/> struct that will be used to store the dynamic effect data. This is required 
        /// to properly allocate the dynamic effect struct.
        ///
        /// <code>
        ///public void CreateDynamicEffectInfo(ValueType valueType, ref DynamicEffectInfo info, ref BlobBuilder builder)
        ///{
        ///    var myDynamicEffectStruct = new MyDynamicEffectStruct
        ///    {
        ///        // Copy values from the dynamic effect to the struct here
        ///        MyValue = this.MyValue,
        ///    };
        ///    DynamicEffectInfo.AllocateDynamicEffect(myDynamicEffectStruct, valueType, ref info, ref builder);
        ///}
        /// </code>
        ///
        /// With Netcode for Entities, dynamic effect elements on predicted ghosts are not replicated or included
        /// in the prediction backup. When a snapshot rolls an entity back and its status effects differ from the
        /// previous frame, its dynamic effect elements are rebuilt with a default <c>Value</c> and these default
        /// struct values, so anything your systems wrote to them is reset. Systems that set <c>Value</c> should
        /// recompute it every tick rather than only when their inputs change, and state that has to survive a
        /// rollback belongs in a replicated ghost component.
        /// </remarks>
        public void CreateDynamicEffectInfo(ValueType valueType, ref DynamicEffectInfo info, ref BlobBuilder builder);
    }
}