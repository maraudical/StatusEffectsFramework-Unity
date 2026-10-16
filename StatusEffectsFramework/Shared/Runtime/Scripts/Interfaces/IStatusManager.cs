using System.Collections.Generic;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Manages the currently active <see cref="StatusEffect"/>s of a component.
    /// </summary>
    public interface IStatusManager
    {
        /// <summary>
        /// This event is invoked when <see cref="StatusEffect"/>s are added or removed.
        /// </summary>
        /// <remarks>Returns the <see cref="StatusEffect"/> reference, the <see cref="StatusEffectsFramework.StatusEffectAction"/>, 
        /// and then the previous and then current <see cref="int"/> values for stack count.</remarks>
        public event System.Action<StatusEffect, StatusEffectAction, int, int> StatusEffectAction;
        /// <summary>
        /// The active <see cref="StatusEffect"/>s. This is read only, use
        /// <see cref="AddStatusEffect(StatusEffectData, int)"/> or <see cref="RemoveStatusEffect(StatusEffect)"/> to change it.
        /// </summary>
        public IEnumerable<StatusEffect> StatusEffects { get; }
        /// <summary>
        /// Gets a <see cref="StatusEffect"/> by its <see cref="StatusEffect.Id"/>. Returns true if it was found.
        /// </summary>
        public bool TryGetStatusEffect(uint id, out StatusEffect statusEffect);
        /// <summary>
        /// Returns the <see cref="StatusEffect"/>s that match the given parameters. Optional
        /// <paramref name="matchAllGroups"/> toggle to check if all group flags in the given
        /// <paramref name="group"/> need to match in order to fetch.
        /// </summary>
#nullable enable
        public IEnumerable<StatusEffect> GetStatusEffects(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null, bool matchAllGroups = true);
#nullable disable
        /// <summary>
        /// Returns the first <see cref="StatusEffect"/> that matches the given parameters. If none are 
        /// found returns null. Optional <paramref name="matchAllGroups"/> toggle to check if all group 
        /// flags in the given <paramref name="group"/> need to match in order to fetch.
        /// </summary>
#nullable enable
        public StatusEffect GetFirstStatusEffect(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null, bool matchAllGroups = true);
#nullable disable
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to this status manager. Returns null if 
        /// no <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, int stacks = 1);
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to this status manager. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect in seconds. Returns null if no <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, int stacks = 1);
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to this status manager. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect where each invocation of the <see cref="StatusEvent"/> 
        /// will reduce the duration by the given interval. Returns null if no 
        /// <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, StatusEvent statusEvent, int stacks = 1);
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to this status manager. 
        /// The StatusEffect will be removed when the given 
        /// <see cref="System.Func{TResult}"/> returns true. Returns null if no 
        /// <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, System.Func<bool> predicate, int stacks = 1);
        /// <summary>
        /// Removes a <see cref="StatusEffect"/> from this status manager.
        /// </summary>
        public void RemoveStatusEffect(StatusEffect statusEffect);
        /// <summary>
        /// Removes all <see cref="StatusEffect"/> from this status manager. If a 
        /// <paramref name="stacks"/> count is given it will remove only the specified amount.
        /// </summary>
#nullable enable
        public void RemoveStatusEffect(StatusEffectData statusEffectData, int? stacks = null);
#nullable disable
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from this status manager that 
        /// have the same <see cref="ComparableName"/>. If a <paramref name="stacks"/> count 
        /// is given it will remove only the specified amount.
        /// </summary>
        public void RemoveStatusEffect(ComparableName name, int? stacks = null);
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from this status manager that 
        /// are part of the given <see cref="StatusEffectGroup"/> group. Optional 
        /// <paramref name="stacks"/> count and <paramref name="matchAllGroups"/> 
        /// toggle to check if all group flags in the given <paramref name="group"/> 
        /// need to match in order to remove.
        /// </summary>
        public void RemoveStatusEffect(StatusEffectGroup group, int? stacks = null, bool matchAllGroups = true);
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from this status manager.
        /// </summary>
        public void RemoveAllStatusEffects();
    }
}
