using StatusEffectFramework.Entities;
using StatusEffectsFramework.Entities.Samples;
using Unity.Entities;

namespace StatusEffectFramework.Samples
{
    public partial class InverseHealthToSpeed : DynamicFloatEffect, IEntityDynamicEffect
    {
        public TypeIndex GetTypeIndex() => TypeManager.GetTypeIndex<InverseHealthToSpeedComponent>();

        public DynamicEffectInfo CreateDynamicEffectInfo()
        {
            var dynamicEffectStruct = new InverseHealthToSpeedStruct
            {
                ConversionRatio = ConversionRatio
            };
            return (this as IEntityDynamicEffect).AllocateDynamicEffect(dynamicEffectStruct);
        }
    }
}