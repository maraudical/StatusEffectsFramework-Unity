namespace StatusEffectsFramework
{
    public abstract class DynamicEffectFloat : DynamicEffect
    {
        public abstract DynamicFloat ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect);
    }
}