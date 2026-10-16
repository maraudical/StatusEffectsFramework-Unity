namespace StatusEffectsFramework
{
    /// <summary>
    /// How a <see cref="StatusEffect"/> ends.
    /// </summary>
    public enum StatusEffectTiming
    {
        /// <summary>
        /// Ends after a number of seconds.
        /// </summary>
        Duration,
        /// <summary>
        /// Lasts until it is removed.
        /// </summary>
        Infinite,
        /// <summary>
        /// Counts down each time a <see cref="StatusEvent"/> is invoked, and ends when nothing is left.
        /// </summary>
        Event,
        /// <summary>
        /// Ends when a predicate returns true.
        /// </summary>
        Predicate
    }
}
