using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Coin Multiplier to Speed", menuName = "Status Effects Framework/Dynamic Effects/Coin Multiplier To Speed", order = 1)]
    public class CoinMultiplierToSpeed : DynamicEffectFloat 
    {
        public override bool PostEvaluate => true;

    }
}