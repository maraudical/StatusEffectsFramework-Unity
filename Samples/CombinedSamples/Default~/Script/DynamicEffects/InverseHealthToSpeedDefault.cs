using UnityEngine;
#if UNITY_2021_3_OR_NEWER
using System.Threading;
#else
using System.Collections;
#endif

namespace StatusEffectFramework.Samples
{
    public partial class InverseHealthToSpeed : DynamicEffectFloat
    {
        public override DynamicFloat ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect)
        {
            var dynamicFloat = new DynamicFloat(this, effect);

            if (manager.TryGetComponent(out ExamplePlayer player))
            {
#if UNITY_2021_3_OR_NEWER

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
#else
                var coroutine = manager.StartCoroutine(UpdateValue());
                statusEffect.Stopped += () => manager.StopCoroutine(coroutine);

                IEnumerator UpdateValue()
                {
                    while (true)
                    {
                        dynamicFloat.Value = Mathf.Max(0, 1f - player.Health / player.MaxHealth) * player.MaxHealth * ConversionRatio;
                        yield return null;
                    }
                }
#endif
            }

            return dynamicFloat;
        }
    }
}