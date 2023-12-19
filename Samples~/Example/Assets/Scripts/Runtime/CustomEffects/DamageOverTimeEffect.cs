#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#else
using System.Collections;
#endif
using UnityEngine;

namespace StatusEffects.Custom
{
    [CreateAssetMenu(fileName = "Damage Over Time Effect", menuName = "Status Effect Framework/Custom Effects/Damage Over Time", order = 1)]
    public class DamageOverTimeEffect : CustomEffect
    {
        [SerializeField] private float intervalSeconds = 1f;

#if UNITASK
        public override async UniTask Effect<T>(T monoBehaviour, StatusEffect statusEffect, CancellationToken token)
        {
            if (monoBehaviour.TryGetComponent(out ExampleEntity entity))
                while (!token.IsCancellationRequested)
                {
                    entity.health -= statusEffect.data.baseValue;
                    await UniTask.WaitForSeconds(intervalSeconds);
                }
        }
#else
        public override IEnumerator Effect<T>(T monoBehaviour, StatusEffect statusEffect)
        {
            if (monoBehaviour.TryGetComponent(out ExampleEntity entity))
                for (; ; )
                {
                    entity.health -= statusEffect.data.baseValue;
                    yield return new WaitForSeconds(intervalSeconds);
                }
        }

        public override void EffectEnd<T>(T monoBehaviour, StatusEffect statusEffect) { }
#endif
    }
}
