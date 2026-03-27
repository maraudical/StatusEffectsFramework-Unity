using System;
using System.Threading;

namespace StatusEffectFramework
{
    public abstract class DynamicFloatEffect : DynamicEffect
    {
        public virtual DynamicFloat ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect, CancellationToken token) => new DynamicFloat(this, effect);
    }
}
