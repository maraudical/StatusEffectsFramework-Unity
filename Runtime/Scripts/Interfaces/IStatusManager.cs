using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using UnityEngine;

namespace StatusEffects
{
    public interface IStatusManager
    {
        /// <summary>
        /// This <see cref="Action"/> is invoked when <see cref="StatusEffect"/>s are added or removed.
        /// </summary>
        /// <remarks>Returns the <see cref="StatusEffect"/> reference, the <see cref="StatusEffectAction"/>, 
        /// and then the previous and then current <see cref="int"/> values for stack count.</remarks>
        public event System.Action<StatusEffect, StatusEffectAction, int, int> OnStatusEffect;
        /// <summary>
        /// This is invoked to update <see cref="StatusVariable"/>s just before the <see cref="IStatusManager.OnStatusEffect"/> event is called.
        /// </summary>
        internal event System.Action<StatusEffect> ValueUpdate;
        /// <summary>
        /// Cannot directly edit this <see cref="IReadOnlyList{T}"/>! Please call 
        /// <see cref="StatusManager.AddStatusEffect"/> or  <see cref="StatusManager.RemoveStatusEffect"/>.
        /// </summary>
        public IEnumerable<StatusEffect> Effects { get; }
        /// <summary>
        /// Gets a <see cref="StatusEffect"/>s by its <see cref="StatusEffect.GetInstanceID"/>.
        /// </summary>
        public bool GetStatusEffect(Hash128 instanceId, out StatusEffect statusEffect);
        /// <summary>
        /// Returns the listed <see cref="StatusEffect"/>s in a <see cref="List{}"/> for the <see cref="StatusManager"/>.
        /// </summary>
#nullable enable
        public IEnumerable<StatusEffect> GetStatusEffects(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null);
#nullable disable
        /// <summary>
        /// Returns the first <see cref="StatusEffect"/>s that matches the given parameters. If none are found returns null.
        /// </summary>
#nullable enable
        public StatusEffect GetFirstStatusEffect(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null);
#nullable disable
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to this <see cref="StatusManager"/>. Returns null if no <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, int stacks = 1);
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to the <see cref="StatusManager"/>. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect in seconds. Returns null if no <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, int stacks = 1);
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to the <see cref="StatusManager"/>. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect where each invocation of the <see cref="UnityEvent"/> 
        /// will reduce the duration by the given interval. Returns null if no 
        /// <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, UnityEvent unityEvent, float interval = 1, int stacks = 1);
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="StatusManager"/>. 
        /// The StatusEffect will be removed when the given 
        /// <see cref="System.Func{bool}"/> is true. Returns null if no 
        /// <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, System.Func<bool> predicate, int stacks = 1);
        /// <summary>
        /// Removes a <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public void RemoveStatusEffect(StatusEffect statusEffect);
        /// <summary>
        /// Removes all <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>. If a stack count is given it will remove only the specified amount.
        /// </summary>
#nullable enable
        public void RemoveStatusEffect(StatusEffectData statusEffectData, int? stacks = null);
#nullable disable
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// have the same <see cref="ComparableName"/>. If a stacks count is given it will remove only the specified amount.
        /// </summary>
        public void RemoveStatusEffect(ComparableName name, int? stacks = null);
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// are part of the given <see cref="StatusEffectGroup"/> group. If a stacks count is given it will remove only the specified amount.
        /// </summary>
        public void RemoveStatusEffect(StatusEffectGroup group, int? stacks = null);
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public void RemoveAllStatusEffects();
    }
}
