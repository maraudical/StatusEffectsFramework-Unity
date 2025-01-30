#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#elif UNITY_2023_1_OR_NEWER
using StatusEffects.Extensions;
using System.Threading;
#else
using System.Collections;
#endif
using UnityEngine;
using StatusEffects.Example;
using StatusEffects.NetCode.Entities;
using Unity.Entities;

namespace StatusEffects.Modules
{
    [CreateAssetMenu(fileName = "Heal Module", menuName = "Status Effect Framework/Modules/Heal", order = 1)]
    public class HealModule : Module
#if ENTITIES && ADDRESSABLES
        , IEntityModule
    {
        public void ModifyCommandBuffer(ref EntityCommandBuffer commandBuffer, in Entity entity, ModuleInstance moduleInstance)
        {
            commandBuffer.AddComponent(entity, new HealEntityModule());
        }

        public struct HealEntityModule : IComponentData { }
#else
    {
#endif
#if UNITASK
        public override async UniTask EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
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
#elif UNITY_2023_1_OR_NEWER
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

            yield return null;
        }

        public override void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (manager.TryGetComponent(out IExamplePlayer player))
                // Clamp health after status effect ends
                player.Health = Mathf.Min(player.Health, player.MaxHealth);                         
        }
#endif

        private void OnStackUpdate(IExamplePlayer player, StatusEffect statusEffect, int previous, int stack)
        {
            player.Health += statusEffect.Data.BaseValue * Mathf.Max(0, stack - previous);
            player.Health = Mathf.Min(player.Health, player.MaxHealth);
        }
    }
}
