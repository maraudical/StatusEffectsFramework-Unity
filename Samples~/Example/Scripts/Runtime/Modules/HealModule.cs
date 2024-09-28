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

namespace StatusEffects.Modules
{
    [CreateAssetMenu(fileName = "Heal Module", menuName = "Status Effect Framework/Modules/Heal", order = 1)]
    public class HealModule : Module
    {
#if UNITASK
        public override async UniTask EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            if (!manager.TryGetComponent(out IExampleEntity entity))
                return;
            // Add health according to status effect
            entity.Health += statusEffect.Data.BaseValue * statusEffect.Stack;
            entity.Health = Mathf.Min(entity.Health, entity.MaxHealth);

            statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(entity, statusEffect, previous, stack);

            await UniTask.WaitUntilCanceled(token);
            // Note that you need to check if the entity is null in case the
            // cancellation was invoked from the destruction of the MonoBehaviour
            if (entity == null)
                return;
            // Clamp health after status effect ends
            entity.Health = Mathf.Min(entity.Health, entity.MaxHealth);
        }
#elif UNITY_2023_1_OR_NEWER
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            if (!manager.TryGetComponent(out IExampleEntity entity))
                return;
            // Add health according to status effect
            entity.Health += statusEffect.Data.BaseValue;
            entity.Health = Mathf.Min(entity.Health, entity.MaxHealth);

            statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(entity, statusEffect, previous, stack);
            
            await AwaitableExtensions.WaitUntilCanceled(token);
            // Note that you need to check if the entity is null in case the
            // cancellation was invoked from the destruction of the MonoBehaviour
            if (entity == null)
                return;
            // Clamp health after status effect ends
            entity.Health = Mathf.Min(entity.Health, entity.MaxHealth);
        }
#else
        public override IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (manager.TryGetComponent(out IExampleEntity entity))
            {
                // Add health according to status effect
                entity.Health += statusEffect.Data.BaseValue;
                entity.Health = Mathf.Min(entity.Health, entity.MaxHealth);

                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(entity, statusEffect, previous, stack);
            }

            yield return null;
        }

        public override void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (manager.TryGetComponent(out IExampleEntity entity))
                // Clamp health after status effect ends
                entity.Health = Mathf.Min(entity.Health, entity.MaxHealth);                         
        }
#endif

        private void OnStackUpdate(IExampleEntity entity, StatusEffect statusEffect, int previous, int stack)
        {
            entity.Health += statusEffect.Data.BaseValue * Mathf.Max(0, stack - previous);
            entity.Health = Mathf.Min(entity.Health, entity.MaxHealth);
        }
    }
}
