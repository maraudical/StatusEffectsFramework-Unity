using StatusEffectsFramework.Entities;
using StatusEffectsFramework.Entities.Samples;

namespace StatusEffectsFramework.Samples
{
    public partial class InverseHealthToSpeed : DynamicEffectFloat, IEntityDynamicEffect
    {
        public DynamicEffectInfo CreateDynamicEffectInfo()
        {
            var dynamicEffectStruct = new InverseHealthToSpeedStruct
            {
                ConversionRatio = ConversionRatio
            };
            return DynamicEffectInfo.AllocateDynamicEffect(dynamicEffectStruct, ValueType.Float);
        }
    }
}