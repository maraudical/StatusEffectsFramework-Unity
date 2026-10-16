using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Holds the data a <see cref="Module"/> uses on a <see cref="StatusEffectData"/>. The type to use is set on the
    /// <see cref="Module"/> with <see cref="AttachModuleInstanceAttribute"/>.
    /// </summary>
    public abstract class ModuleInstance : ScriptableObject { }
}
