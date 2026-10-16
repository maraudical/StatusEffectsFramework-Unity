using System;
using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Pairs a <see cref="Module"/> with the <see cref="ModuleInstance"/> data it uses on a <see cref="StatusEffectData"/>.
    /// </summary>
    [Serializable]
    public class ModuleContainer
    {
        public Module Module => m_Module;
        public ModuleInstance ModuleInstance => m_ModuleInstance;

        [Tooltip("The module that adds custom behaviour to the status effect.")]
        [SerializeField]
        internal Module m_Module;
        [Tooltip("The data the module uses for this status effect. The type is defined by the module.")]
        [SerializeField]
        internal ModuleInstance m_ModuleInstance;
    }
}
