namespace StatusEffectsFramework
{
    /// <summary>
    /// A <see cref="DynamicEffect"/> for a <see cref="StatusInt"/>.
    /// </summary>
    public abstract class DynamicEffectInt : DynamicEffect 
    {
        /// <summary>
        /// Called when the <see cref="StatusEffect"/> is added. Returns the <see cref="DynamicInt"/> whose
        /// <see cref="DynamicInt.Value"/> should be set whenever the value changes.
        /// </summary>
        public abstract DynamicInt ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect);
    }
}
