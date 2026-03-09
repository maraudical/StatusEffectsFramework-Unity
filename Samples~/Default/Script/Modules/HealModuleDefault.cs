#if UNITY_2023_1_OR_NEWER
using System.Threading;
#else
using System.Collections;
#endif
using UnityEngine;

namespace StatusEffectFramework.Samples
{
    public partial class HealModule : Module
    {
#if UNITY_2023_1_OR_NEWER
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            if (!manager.TryGetComponent(out IExamplePlayer player))
                return;
            // Add health according to status effect
            player.Health += statusEffect.Data.BaseValue;
            player.Health = Mathf.Min(player.Health, player.MaxHealth);

            statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(player, statusEffect, previous, stack);

            await AwaitableExtensions.WaitUntilCanceled(token);
            // Note that you need to check if the player is null in case the
            // cancellation was invoked from the destruction of the MonoBehaviour
            if (player == null)
                return;
            // Clamp health after status effect ends
            player.Health = Mathf.Min(player.Health, player.MaxHealth);
        }
#else
        public override IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (manager.TryGetComponent(out IExamplePlayer player))
            {
                // Add health according to status effect
                player.Health += statusEffect.Data.BaseValue;
                player.Health = Mathf.Min(player.Health, player.MaxHealth);

                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(player, statusEffect, previous, stack);
            }

            yield break;
        }

        public override void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (manager.TryGetComponent(out IExamplePlayer player))
                // Clamp health after status effect ends
                player.Health = Mathf.Min(player.Health, player.MaxHealth);
        }
#endif
    }
}