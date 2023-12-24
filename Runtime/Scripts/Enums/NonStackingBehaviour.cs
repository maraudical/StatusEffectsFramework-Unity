namespace StatusEffects
{
    public enum NonStackingBehaviour
    {
        /// <summary>
        /// Match Highest Value will take the value of the effect and 
        /// recalculate the duration to the equivalent final value over the time. 
        /// This is the RECOMMENDED option for non-stacking behaviour.
        /// </summary>
        MatchHighestValue,
        /// <summary>
        /// Regardless of the durations, use the effect with the highest value.
        /// </summary>
        TakeHighestValue,
        /// <summary>
        /// Regardless of the value, use the effect with the highest duration.
        /// </summary>
        TakeHighestDuration,
        /// <summary>
        /// Use the newest effect.
        /// </summary>
        TakeNewest,
        /// <summary>
        /// Use the oldest effect.
        /// </summary>
        TakeOldest,
    }
}
