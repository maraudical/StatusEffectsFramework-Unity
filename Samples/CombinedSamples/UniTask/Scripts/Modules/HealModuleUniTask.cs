using Cysharp.Threading.Tasks;
using StatusEffectsFramework.Entities;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    public partial class HealModule : Module
    {
        public override async UniTaskVoid EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            if (!manager.TryGetComponent(out IExamplePlayer player))
                return;
            // Add health according to status effect
            player.Health += statusEffect.Data.BaseValue * statusEffect.Stacks;
            player.Health = Mathf.Min(player.Health, player.MaxHealth);

            statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(player, statusEffect, previous, stack);

            await UniTask.WaitUntilCanceled(token);
            // Note that you need to check if the entity is null in case the
            // cancellation was invoked from the destruction of the MonoBehaviour
            if (player == null)
                return;
            // Clamp health after status effect ends
            player.Health = Mathf.Min(player.Health, player.MaxHealth);
        }
    }
}