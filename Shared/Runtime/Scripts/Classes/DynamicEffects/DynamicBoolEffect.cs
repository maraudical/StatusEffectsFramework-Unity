namespace StatusEffectFramework
{
    public abstract class DynamicBoolEffect : DynamicEffect
    {
        public abstract DynamicBool ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect);
    }
}
