using UnityEngine;

namespace StatusEffectsFramework
{
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
