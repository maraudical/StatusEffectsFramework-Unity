namespace StatusEffectsFramework
{
    /// <summary>
    /// How long a <see cref="StatusEffect"/> added by a <see cref="Condition"/> lasts.
    /// </summary>
    public enum ConditionalTiming
    {
        /// <summary>
        /// Lasts for the duration set on the <see cref="Condition"/>.
        /// </summary>
        Duration,
        /// <summary>
        /// Lasts until it is removed.
        /// </summary>
        Infinite,
        /// <summary>
        /// Ends the same way as the <see cref="StatusEffect"/> that triggered the <see cref="Condition"/>.
        /// </summary>
        Inherited
    }
}
