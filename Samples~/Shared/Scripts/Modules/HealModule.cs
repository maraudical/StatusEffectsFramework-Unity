using UnityEngine;

namespace StatusEffectFramework.Samples
{
    [CreateAssetMenu(fileName = "Heal Module", menuName = "Status Effect Framework/Modules/Heal", order = 1)]
    public partial class HealModule : Module
    {  
        private void OnStackUpdate(IExamplePlayer player, StatusEffect statusEffect, int previous, int stack)
        {
            player.Health += statusEffect.Data.BaseValue * Mathf.Max(0, stack - previous);
            player.Health = Mathf.Min(player.Health, player.MaxHealth);
        }
    }
}