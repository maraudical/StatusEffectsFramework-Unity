#if Entities
using Unity.Entities;
using StatusEffectsFramework.Entities;
#endif
#if UniTask
using Cysharp.Threading.Tasks;
using System.Threading;
#elif Default
using System.Threading;
#endif
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Inverse Health to Speed", menuName = "Status Effects Framework/Dynamic Effects/Inverse Health to Speed", order = 1)]
#if Entities
    public class InverseHealthToSpeed : DynamicEffectFloat, IEntityDynamicEffect
#else
    public class InverseHealthToSpeed : DynamicEffectFloat
#endif
    {
        public override bool PostEvaluate => false;

        public float ConversionRatio = 0.05f;

#if Entities
        public void CreateDynamicEffectInfo(ValueType valueType, ref DynamicEffectInfo info, ref BlobBuilder builder)
        {
            var dynamicEffectStruct = new InverseHealthToSpeedStruct
            {
                ConversionRatio = ConversionRatio
            };
            DynamicEffectInfo.AllocateDynamicEffect(dynamicEffectStruct, valueType, ref info, ref builder);
        }
#endif
#if UniTask
        public override DynamicFloat ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect)
        {
            var dynamicFloat = new DynamicFloat(this, effect);

            if (manager.TryGetComponent(out ExamplePlayer player))
            {
                var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(manager.destroyCancellationToken);
                UpdateValue(cancellationTokenSource.Token).Forget();
                statusEffect.Stopped += cancellationTokenSource.Cancel;

                async UniTaskVoid UpdateValue(CancellationToken token)
                {
                    while (true)
                    {
                        dynamicFloat.Value = Mathf.Max(0, 1f - player.Health / player.MaxHealth) * player.MaxHealth * ConversionRatio;
                        await UniTask.NextFrame(token);
                    }
                }
            }

            return dynamicFloat;
        }
#elif Default
        public override DynamicFloat ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect)
        {
            var dynamicFloat = new DynamicFloat(this, effect);

            if (manager.TryGetComponent(out ExamplePlayer player))
            {
                var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(manager.destroyCancellationToken);
                _ = UpdateValue(cancellationTokenSource.Token);
                statusEffect.Stopped += cancellationTokenSource.Cancel;

                async Awaitable UpdateValue(CancellationToken token)
                {
                    while (true)
                    {
                        dynamicFloat.Value = Mathf.Max(0, 1f - player.Health / player.MaxHealth) * player.MaxHealth * ConversionRatio;
                        await Awaitable.NextFrameAsync(token);
                    }
                }
            }

            return dynamicFloat;
        }
#endif
    }
}