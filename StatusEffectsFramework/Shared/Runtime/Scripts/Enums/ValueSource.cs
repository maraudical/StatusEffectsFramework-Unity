namespace StatusEffectsFramework
{
    /// <summary>
    /// Where an <see cref="Effect"/> gets its value from.
    /// </summary>
    public enum ValueSource
    {
        /// <summary>
        /// The value entered on the <see cref="Effect"/>.
        /// </summary>
        ExplicitValue,
        /// <summary>
        /// The <see cref="StatusEffectData.BaseValue"/> of the <see cref="StatusEffectData"/>.
        /// </summary>
        BaseValue,
        /// <summary>
        /// A value that is set at runtime by a <see cref="DynamicEffect"/>.
        /// </summary>
        DynamicValue,
    }
}
