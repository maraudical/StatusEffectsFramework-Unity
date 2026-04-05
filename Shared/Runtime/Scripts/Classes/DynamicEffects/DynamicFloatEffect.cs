namespace StatusEffectFramework
{
    public abstract class DynamicFloatEffect : DynamicEffect
    {
        public virtual DynamicFloat ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect) => new DynamicFloat(this, effect);
    }
}
