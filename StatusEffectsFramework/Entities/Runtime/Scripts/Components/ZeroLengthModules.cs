using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Module types whose buffer has become empty and should be removed from the entity if it is
    /// still empty when checked later.
    /// </summary>
    /// <remarks>
    /// Removing a module buffer is deferred instead of done as soon as it empties, since it can be
    /// refilled before then (for example by another event in a later tick or after a rollback):
    /// <list type="number">
    /// <item><c>ModulesJob</c> appends a type here, through its late command buffer, once its tracked
    /// length reaches zero. If the type later gets elements again, it removes any entries for it that
    /// have already been played back.</item>
    /// <item><c>ModulesFirstPredictionTickJob</c> appends the leftover types it clears after a rollback.</item>
    /// <item><c>ZeroLengthModulesJob</c>, scheduled by the <see cref="ModulesSystem"/>, removes each
    /// listed type whose buffer is still empty, clears this buffer and disables it.</item>
    /// </list>
    /// The component is only enabled while it has entries, so <c>ZeroLengthModulesJob</c> skips every
    /// other entity.
    /// </remarks>
    internal struct ZeroLengthModules : IBufferElementData, IEnableableComponent
    {
        public TypeIndex TypeIndex;
    }
}