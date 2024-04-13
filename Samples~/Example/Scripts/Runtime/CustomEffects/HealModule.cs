#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#else
using System.Collections;
#endif
using UnityEngine;
using StatusEffects.Example;
using StatusEffects.Modules;

namespace StatusEffects.Custom
{
    [CreateAssetMenu(fileName = "Heal Module", menuName = "Status Effect Framework/Modules/Heal", order = 1)]
    public class HealModule : Module
    {
#if UNITASK
        public override async UniTask EnableModule<T>(T monoBehaviour, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            if (!monoBehaviour.TryGetComponent(out ExampleEntity entity))
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
#else
        public override IEnumerator Effect<T>(T monoBehaviour, StatusEffect statusEffect)
        {
            if (monoBehaviour.TryGetComponent(out ExampleEntity entity))
                entity.health += statusEffect.data.baseValue;

            yield return null;
        }

        public override void EffectEnd<T>(T monoBehaviour, StatusEffect statusEffect)
        {
            if (monoBehaviour.TryGetComponent(out ExampleEntity entity))
                entity.health = Mathf.Min(entity.health, entity.maxHealth);                         
        }
#endif
    }
}
