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
            if (!manager.TryGetComponent(out ExampleEntity entity))
                return;
            // Add health according to status effect
            entity.health += statusEffect.data.baseValue;
            entity.health = Mathf.Min(entity.health, entity.maxHealth);

            await UniTask.WaitUntilCanceled(token);
            // Note that you need to check if the entity is null in case the
            // cancellation was invoked from the destruction of the MonoBehaviour
            if (!entity)
                return;
            // Clamp health after status effect ends
            entity.health = Mathf.Min(entity.health, entity.maxHealth);
        }
#elif UNITY_2023_1_OR_NEWER
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            if (!manager.TryGetComponent(out ExampleEntity entity))
                return;
            // Add health according to status effect
            entity.health += statusEffect.data.baseValue;
            entity.health = Mathf.Min(entity.health, entity.maxHealth);
            
            await AwaitableExtensions.WaitUntilCanceled(token);
            // Note that you need to check if the entity is null in case the
            // cancellation was invoked from the destruction of the MonoBehaviour
            if (!entity)
                return;
            // Clamp health after status effect ends
            entity.health = Mathf.Min(entity.health, entity.maxHealth);
        }
#else
        public override IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (manager.TryGetComponent(out ExampleEntity entity))
            {
                // Add health according to status effect
                entity.health += statusEffect.data.baseValue;
                entity.health = Mathf.Min(entity.health, entity.maxHealth);
            }

            yield return null;
        }

        public override void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (manager.TryGetComponent(out ExampleEntity entity))
                // Clamp health after status effect ends
                entity.health = Mathf.Min(entity.health, entity.maxHealth);                         
        }
#endif
    }
}
