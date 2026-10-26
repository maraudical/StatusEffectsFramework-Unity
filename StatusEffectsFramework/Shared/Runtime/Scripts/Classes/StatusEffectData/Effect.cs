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
        /// <summary>
        /// The type of value the <see cref="StatusName"/> holds, which decides whether the float, int or bool value
        /// is used. It is stored so it is known without resolving the key, and the <see cref="StatusRegistry"/> doesn't
        /// register a <see cref="StatusEffectData"/> whose effects don't match their registered status names.
        /// </summary>
        /// <remarks>
        /// In the editor it comes from the assigned asset when there is one, and it is baked before a build.
        /// </remarks>
        public ValueType ValueType
        {
            get
            {
#if UNITY_EDITOR
                var statusName = m_StatusName.EditorAsset;
                if (statusName)
                    return statusName.ValueType;
#endif
                return m_ValueType;
            }
        }
        public ValueModifier ValueModifier => m_ValueModifier;
        public ValueSource ValueSource => m_ValueSource;
        public int Priority => m_Priority;
        public float FloatValue => m_FloatValue;
        public int IntValue => m_IntValue;
        public bool BoolValue => m_BoolValue;
        public DynamicEffectFloat DynamicFloat => m_DynamicFloat;
        public DynamicEffectInt DynamicInt => m_DynamicInt;
        public DynamicEffectBool DynamicBool => m_DynamicBool;

        [SerializeField]
        private RegistrantKey<StatusName> m_StatusName;
        [Tooltip("The type of value the status name holds. It follows the assigned status name, so it only needs to be chosen when the key is typed.")]
        [SerializeField]
        private ValueType m_ValueType;
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
        [SerializeField] private DynamicEffectFloat m_DynamicFloat;
        [Tooltip("Sets the value at runtime. Only used when the Value Source is Dynamic Value.")]
        [SerializeField] private DynamicEffectInt m_DynamicInt;
        [Tooltip("Sets the value at runtime. Only used when the Value Source is Dynamic Value.")]
        [SerializeField] private DynamicEffectBool m_DynamicBool;
    }
}
