using StatusEffectsFramework.Entities;
using StatusEffectsFramework.Entities.Samples;
using Unity.Entities;

namespace StatusEffectsFramework.Samples
{
    public partial class InverseHealthToSpeed : DynamicEffectFloat, IEntityDynamicEffect
    {
        public void CreateDynamicEffectInfo(ValueType valueType, ref DynamicEffectInfo info, ref BlobBuilder builder)
        {
            var dynamicEffectStruct = new InverseHealthToSpeedStruct
            {
                ConversionRatio = ConversionRatio
            };
            DynamicEffectInfo.AllocateDynamicEffect(dynamicEffectStruct, valueType, ref info, ref builder);
        }
    }
}