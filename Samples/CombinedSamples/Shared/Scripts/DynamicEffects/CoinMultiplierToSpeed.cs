#if Entities
using StatusEffectsFramework.Entities;
using Unity.Entities;
#endif
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Coin Multiplier to Speed", menuName = "Status Effects Framework/Dynamic Effects/Coin Multiplier To Speed", order = 1)]
#if Entities
    public class CoinMultiplierToSpeed : DynamicEffectFloat, IEntityDynamicEffect
#else
    public class CoinMultiplierToSpeed : DynamicEffectFloat 
#endif
    {
        public override bool PostEvaluate => true;

#if Entities
        public void CreateDynamicEffectInfo(ValueType valueType, ref DynamicEffectInfo info, ref BlobBuilder builder)
        {
            DynamicEffectInfo.AllocateDynamicEffect(new CoinMultiplierToSpeedStruct(), valueType, ref info, ref builder);
        }
#endif
#if Default || UniTask
        public override DynamicFloat ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect)
        {
            var dynamicFloat = new DynamicFloat(this, effect);

            if (manager.TryGetComponent(out ExamplePlayer player))
            {
                dynamicFloat.Value = Mathf.Max(0, player.CoinMultiplier - 1);
                player.StatusCoinMultiplier.OnPreEvaluationValueChanged += UpdateValue;
                statusEffect.Stopped += () => player.StatusCoinMultiplier.OnPreEvaluationValueChanged -= UpdateValue;

                void UpdateValue(int previousValue, int currentValue) => dynamicFloat.Value = Mathf.Max(0, currentValue - 1);
            }

            return dynamicFloat;
        }
#endif
    }
}