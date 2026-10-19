using StatusEffectsFramework.Entities;
using Unity.Entities;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Coin Multiplier to Speed", menuName = "Status Effects Framework/Dynamic Effects/Coin Multiplier To Speed", order = 1)]
    public class CoinMultiplierToSpeed : DynamicEffectFloat, IEntityDynamicEffect
    {
        public override bool PostEvaluate => true;

        public void CreateDynamicEffectInfo(ValueType valueType, ref DynamicEffectInfo info, ref BlobBuilder builder)
        {
            DynamicEffectInfo.AllocateDynamicEffect(new CoinMultiplierToSpeedStruct(), valueType, ref info, ref builder);
        }
    }
}