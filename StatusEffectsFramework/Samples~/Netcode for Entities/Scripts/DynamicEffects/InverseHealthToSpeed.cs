using Unity.Entities;
using StatusEffectsFramework.Entities;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Inverse Health to Speed", menuName = "Status Effects Framework/Dynamic Effects/Inverse Health to Speed", order = 1)]
    public class InverseHealthToSpeed : DynamicEffectFloat, IEntityDynamicEffect
    {
        public override bool PostEvaluate => false;

        public float ConversionRatio = 0.05f;

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