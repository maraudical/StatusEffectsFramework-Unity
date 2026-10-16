namespace StatusEffectsFramework
{
    /// <summary>
    /// How the value of an <see cref="Effect"/> is combined with the other effects on the same status.
    /// The result is <c>(base + additive) * multiplicative + postAdditive</c>, limited by the minimum and maximum,
    /// unless an overwrite replaces it.
    /// </summary>
    public enum ValueModifier
    {
        /// <summary>
        /// Added to the base value before multiplying.
        /// </summary>
        Additive = 0,
        /// <summary>
        /// Added to a multiplier that starts at 1, so two effects of 0.5 give a multiplier of 2, not 1.25.
        /// </summary>
        Multiplicative = 1 << 1,
        /// <summary>
        /// Added after multiplying.
        /// </summary>
        PostAdditive = 1 << 2,
        /// <summary>
        /// Limits the value to at most this. The highest priority wins, and on a tie the lowest value.
        /// </summary>
        Maximum = 1 << 3,
        /// <summary>
        /// Limits the value to at least this. The highest priority wins, and on a tie the highest value.
        /// </summary>
        Minimum = 1 << 4,
        /// <summary>
        /// Replaces the value. The highest priority wins, and on a tie the oldest <see cref="StatusEffect"/>.
        /// Only a minimum or maximum with at least the same priority can still limit it.
        /// </summary>
        Overwrite = 1 << 5,
    }
}
