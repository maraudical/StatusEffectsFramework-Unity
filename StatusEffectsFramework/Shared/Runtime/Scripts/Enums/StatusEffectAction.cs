namespace StatusEffectsFramework
{
    /// <summary>
    /// What happened to a <see cref="StatusEffect"/>, as reported by <see cref="IStatusManager.StatusEffectAction"/>.
    /// </summary>
    public enum StatusEffectAction
    {
        /// <summary>
        /// The <see cref="StatusEffect"/> was added. The previous stack count is 0.
        /// </summary>
        AddedStatusEffect,
        /// <summary>
        /// The <see cref="StatusEffect"/> was removed. The current stack count is 0.
        /// </summary>
        RemovedStatusEffect,
        /// <summary>
        /// The stack count of an existing <see cref="StatusEffect"/> increased.
        /// </summary>
        AddedStacks,
        /// <summary>
        /// The stack count of an existing <see cref="StatusEffect"/> decreased.
        /// </summary>
        RemovedStacks
    }
}