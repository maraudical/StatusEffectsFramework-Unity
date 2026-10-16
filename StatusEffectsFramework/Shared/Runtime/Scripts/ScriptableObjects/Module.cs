using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Adds custom behaviour to a <see cref="StatusEffect"/>. Add it to the modules of a <see cref="StatusEffectData"/> and
    /// override <see cref="EnableModule"/> and <see cref="DisableModule"/>.
    /// </summary>
    public abstract class Module : ScriptableObject
    {
        /// <summary>
        /// Use this callback to do something when the effect begins.
        /// </summary>
        public virtual void EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance) { }
        /// <summary>
        /// Use this callback to do something when the effect ends.
        /// </summary>
        public virtual void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance) { }
    }
}
