using System.Threading;
using UnityEngine;

namespace StatusEffectFramework.Samples
{
    public partial class InverseHealthToSpeed : DynamicFloatEffect
    {
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
    }
}