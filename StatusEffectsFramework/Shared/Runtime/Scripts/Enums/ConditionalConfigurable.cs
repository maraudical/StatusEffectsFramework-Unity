namespace StatusEffectsFramework
{
    /// <summary>
    /// What a <see cref="Condition"/> searches for, or acts on.
    /// </summary>
    public enum ConditionalConfigurable
    {
        /// <summary>
        /// A specific <see cref="StatusEffectData"/>.
        /// </summary>
        Data,
        /// <summary>
        /// Every <see cref="StatusEffect"/> with a <see cref="ComparableName"/>.
        /// </summary>
        Name,
        /// <summary>
        /// <see cref="StatusEffect"/>s that are in all of the selected groups.
        /// </summary>
        AllGroups,
        /// <summary>
        /// <see cref="StatusEffect"/>s that are in at least one of the selected groups.
        /// </summary>
        AnyGroups,
    }
}