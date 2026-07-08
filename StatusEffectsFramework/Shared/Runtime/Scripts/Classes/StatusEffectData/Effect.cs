using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace StatusEffectsFramework
{
    [Serializable]
    public class Effect
    {
        public StatusName StatusName => m_StatusName;
        public ValueModifier ValueModifier => m_ValueModifier;
        public ValueSource ValueSource => m_ValueSource;
        public int Priority => m_Priority;
        public float FloatValue => m_FloatValue;
        public int IntValue => m_IntValue;
        public bool BoolValue => m_BoolValue;
        public DynamicEffectFloat DynamicFloatEffect => m_DynamicFloatEffect;
        public DynamicEffectInt DynamicIntEffect => m_DynamicIntEffect;
        public DynamicEffectBool DynamicBoolEffect => m_DynamicBoolEffect;

        [SerializeField, FormerlySerializedAs("StatusName")]
        private StatusName m_StatusName;
        [SerializeField, FormerlySerializedAs("ValueModifier")]
        private ValueModifier m_ValueModifier;
        [SerializeField]
        private ValueSource m_ValueSource;
        [SerializeField, FormerlySerializedAs("Priority")]
        [Min(0)] private int m_Priority;
        [SerializeField, FormerlySerializedAs("FloatValue")]
        private float m_FloatValue;
        [SerializeField, FormerlySerializedAs("IntValue")]
        private int m_IntValue;
        [SerializeField, FormerlySerializedAs("BoolValue")]
        private bool m_BoolValue;
        [SerializeField] private DynamicEffectFloat m_DynamicFloatEffect;
        [SerializeField] private DynamicEffectInt m_DynamicIntEffect;
        [SerializeField] private DynamicEffectBool m_DynamicBoolEffect;
    }
}
