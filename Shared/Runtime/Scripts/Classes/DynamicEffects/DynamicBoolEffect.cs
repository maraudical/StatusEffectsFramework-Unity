namespace StatusEffectFramework
{
    public abstract class DynamicBoolEffect : DynamicEffect
    {
        public virtual DynamicBool ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect) => new DynamicBool(this, effect);
    }
}
