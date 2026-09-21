#if UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;
#elif UNITY_2023_1_OR_NEWER
using System.Threading;
using System.Threading.Tasks;
#else
using System.Collections;
#endif
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
