using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Inverse Health to Speed", menuName = "Status Effect Framework/Dynamic Effects/Inverse Health to Speed", order = 1)]
    public partial class InverseHealthToSpeed : DynamicEffectFloat 
    {
        public override bool PostEvaluate => true;

        public float ConversionRatio = 0.05f;
    }
}