using UnityEngine;

namespace StatusEffectFramework.Samples
{
    [CreateAssetMenu(fileName = "Coin Multiplier to Speed", menuName = "Status Effect Framework/Dynamic Effects/Coin Multiplier To Speed", order = 1)]
    public partial class CoinMultiplierToSpeed : DynamicFloatEffect 
    {
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
    }
}