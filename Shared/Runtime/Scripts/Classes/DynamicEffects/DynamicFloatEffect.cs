namespace StatusEffectFramework
{
    public abstract class DynamicFloatEffect : DynamicEffect
    {
        public abstract DynamicFloat ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect);
    }
}