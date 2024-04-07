#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#else
using System.Collections;
#endif
using UnityEngine;
using StatusEffects.Example;

namespace StatusEffects.Custom
{
    [CreateAssetMenu(fileName = "Heal Effect", menuName = "Status Effect Framework/Custom Effects/Heal", order = 1)]
    public class HealEffect : CustomEffect
    {
#if UNITASK
        public override async UniTask Effect<T>(T monoBehaviour, StatusEffect statusEffect, CancellationToken token)
        {
            if (!monoBehaviour.TryGetComponent(out ExampleEntity entity))
                return;

            entity.health += statusEffect.data.baseValue;
            entity.health = Mathf.Min(entity.health, entity.maxHealth);

            await UniTask.WaitUntilCanceled(token);
            // Note that you need to check if the entity is null in case the
            // cancellation was invoked from the destruction of the MonoBehaviour
            if (!entity)
                return;

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
