namespace StatusEffectsFramework
{
    public abstract class DynamicEffectInt : DynamicEffect 
    {
        public abstract DynamicInt ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect);
    }
}
