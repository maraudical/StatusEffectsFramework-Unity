using System.Threading;
using UnityEngine;

namespace StatusEffectFramework.Samples
{
    public partial class InverseHealthToSpeed : DynamicFloatEffect
    {
        public float ConversionRatio = 0.05f;
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
                        dynamicFloat.Value = (1f - player.Health / player.MaxHealth) * player.MaxHealth * ConversionRatio;
                        await Awaitable.NextFrameAsync(token);
                    }
                }
#else
                 
#endif
            }

            return dynamicFloat;
        }
    }
}