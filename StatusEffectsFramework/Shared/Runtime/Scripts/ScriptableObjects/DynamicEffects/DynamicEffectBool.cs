namespace StatusEffectsFramework
{
    /// <summary>
    /// A <see cref="DynamicEffect"/> for a <see cref="StatusBool"/>.
    /// </summary>
    public abstract class DynamicEffectBool : DynamicEffect
    {
        /// <summary>
        /// Called when the <see cref="StatusEffect"/> is added. Returns the <see cref="DynamicBool"/> whose
        /// <see cref="DynamicBool.Value"/> should be set whenever the value changes.
        /// </summary>
        public abstract DynamicBool ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect);
    }
}
