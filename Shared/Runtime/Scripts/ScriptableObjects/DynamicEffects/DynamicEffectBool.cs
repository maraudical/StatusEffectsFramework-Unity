namespace StatusEffectFramework
{
    public abstract class DynamicEffectBool : DynamicEffect
    {
        public abstract DynamicBool ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect);
    }
}
