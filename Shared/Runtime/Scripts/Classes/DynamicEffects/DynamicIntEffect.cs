namespace StatusEffectFramework
{
    public abstract class DynamicIntEffect : DynamicEffect 
    {
        public abstract DynamicInt ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect);
    }
}
