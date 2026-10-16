using System;
using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// A change that a <see cref="StatusEffectData"/> makes to a status while its <see cref="StatusEffect"/> is active. The value is
    /// multiplied by the stacks and applied to every status variable with the same <see cref="StatusName"/>.
    /// </summary>
    [Serializable]
    public class Effect
    {
        public RegistrantKey<StatusName> StatusName => m_StatusName;
        public ValueModifier ValueModifier => m_ValueModifier;
        public ValueSource ValueSource => m_ValueSource;
        public int Priority => m_Priority;
        public float FloatValue => m_FloatValue;
        public int IntValue => m_IntValue;
        public bool BoolValue => m_BoolValue;
        public DynamicEffectFloat DynamicFloatEffect => m_DynamicFloatEffect;
        public DynamicEffectInt DynamicIntEffect => m_DynamicIntEffect;
        public DynamicEffectBool DynamicBoolEffect => m_DynamicBoolEffect;

        [SerializeField]
        private RegistrantKey<StatusName> m_StatusName;
        [Tooltip("How the value is combined with the other effects on the same status. See the documentation for more information.")]
        [SerializeField]
        private ValueModifier m_ValueModifier;
        [Tooltip("Where the value comes from: a value entered here, the Base Value of the status effect, or a Dynamic Effect that sets it at runtime.")]
        [SerializeField]
        private ValueSource m_ValueSource;
        [Tooltip("Only used by the Minimum, Maximum and Overwrite modifiers. When several effects use the same one on a status, the highest priority wins.")]
        [SerializeField]
        [Min(0)] private int m_Priority;
        [SerializeField]
        private float m_FloatValue;
        [SerializeField]
        private int m_IntValue;
        [SerializeField]
        private bool m_BoolValue;
        [Tooltip("Sets the value at runtime. Only used when the Value Source is Dynamic Value.")]
        [SerializeField] private DynamicEffectFloat m_DynamicFloatEffect;
        [Tooltip("Sets the value at runtime. Only used when the Value Source is Dynamic Value.")]
        [SerializeField] private DynamicEffectInt m_DynamicIntEffect;
        [Tooltip("Sets the value at runtime. Only used when the Value Source is Dynamic Value.")]
        [SerializeField] private DynamicEffectBool m_DynamicBoolEffect;
    }
}
