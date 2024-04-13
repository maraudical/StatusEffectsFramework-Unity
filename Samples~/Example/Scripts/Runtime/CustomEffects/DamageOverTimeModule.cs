#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#else
using System.Collections;
#endif
using UnityEngine;
using StatusEffects.Example;
using StatusEffects.Modules;
using System;

namespace StatusEffects.Custom
{
    [CreateAssetMenu(fileName = "Damage Over Time Module", menuName = "Status Effect Framework/Modules/Damage Over Time", order = 1)]
    [AttachModuleInstance(typeof(DamageOverTimeInstance))]
    public class DamageOverTimeModule : Module
    {
#if UNITASK
        public override async UniTask EnableModule<T>(T monoBehaviour, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            if (monoBehaviour.TryGetComponent(out ExampleEntity entity))
                while (!token.IsCancellationRequested)
                {
                    // Reduce health based on the Statu Effect base value
                    entity.health -= statusEffect.data.baseValue;
                    // Wait for the interval before applying the damage again
                    await UniTask.WaitForSeconds(damageOverTimeInstance.intervalSeconds);
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
