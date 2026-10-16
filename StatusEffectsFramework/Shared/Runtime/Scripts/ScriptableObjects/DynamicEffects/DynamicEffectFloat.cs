namespace StatusEffectsFramework
{
    /// <summary>
    /// A <see cref="DynamicEffect"/> for a <see cref="StatusFloat"/>.
    /// </summary>
    public abstract class DynamicEffectFloat : DynamicEffect
    {
        /// <summary>
        /// Called when the <see cref="StatusEffect"/> is added. Returns the <see cref="DynamicFloat"/> whose
        /// <see cref="DynamicFloat.Value"/> should be set whenever the value changes.
        /// </summary>
        public abstract DynamicFloat ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect);
    }
}