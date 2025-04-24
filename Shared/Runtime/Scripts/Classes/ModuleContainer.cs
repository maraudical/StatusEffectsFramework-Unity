using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace StatusEffects.Modules
{
    [Serializable]
    public class ModuleContainer
    {
        public Module Module => m_Module;
        public ModuleInstance ModuleInstance => m_ModuleInstance;

        [SerializeField, FormerlySerializedAs("Module")]
        private Module m_Module;
        [SerializeField, FormerlySerializedAs("ModuleInstance")]
        private ModuleInstance m_ModuleInstance;
    }
}
