using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace StatusEffectsFramework
{
    [Serializable]
    public class ModuleContainer
    {
        public Module Module => m_Module;
        public ModuleInstance ModuleInstance => m_ModuleInstance;

        [SerializeField, FormerlySerializedAs("Module")]
        internal Module m_Module;
        [SerializeField, FormerlySerializedAs("ModuleInstance")]
        internal ModuleInstance m_ModuleInstance;
    }
}
