using System.Collections.Generic;

namespace StatusEffects
{
    public interface IStatusManager
    {
        /// <summary>
        /// This <see cref="Action"/> is invoked when <see cref="StatusEffect"/>s are added or removed.
        /// </summary>
        public event System.Action<StatusEffect, StatusEffectAction, int> OnStatusEffect;
        /// <summary>
        /// This is invoked to update <see cref="StatusVariable"/>s just before the <see cref="IStatusManager.OnStatusEffect"/> event is called.
        /// </summary>
        internal event System.Action<StatusEffect> ValueUpdate;
        /// <summary>
        /// Cannot directly edit this <see cref="IReadOnlyList{T}"/>! Please call 
        /// <see cref="StatusManager.AddStatusEffect"/> or  <see cref="StatusManager.RemoveStatusEffect"/>.
        /// </summary>
        public IEnumerable<StatusEffect> Effects { get; }
    }
}
