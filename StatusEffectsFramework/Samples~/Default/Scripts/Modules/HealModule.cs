using Cysharp.Threading.Tasks;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Heal Module", menuName = "Status Effects Framework/Modules/Heal", order = 1)]
    public class HealModule : Module
    {
        public override void EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (!manager.TryGetComponent(out IExamplePlayer player))
                return;

            // Add health according to status effect
            player.Health += statusEffect.Data.BaseValue * statusEffect.Stacks;
            player.Health = Mathf.Min(player.Health, player.MaxHealth);

            statusEffect.StackUpdate += (previous, stack) => OnStackUpdate(player, statusEffect, previous, stack);
            // Clamp health after status effect ends
            statusEffect.Stopped += () => player.Health = Mathf.Min(player.Health, player.MaxHealth);

            void OnStackUpdate(IExamplePlayer player, StatusEffect statusEffect, int previous, int stack)
            {
                player.Health += statusEffect.Data.BaseValue * Mathf.Max(0, stack - previous);
                player.Health = Mathf.Min(player.Health, player.MaxHealth);
            }
        }
    }
}