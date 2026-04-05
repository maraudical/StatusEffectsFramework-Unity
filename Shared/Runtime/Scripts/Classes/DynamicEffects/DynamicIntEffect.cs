namespace StatusEffectFramework
{
    public abstract class DynamicIntEffect : DynamicEffect 
    {
        public virtual DynamicInt ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect) => new DynamicInt(this, effect);
    }
}
