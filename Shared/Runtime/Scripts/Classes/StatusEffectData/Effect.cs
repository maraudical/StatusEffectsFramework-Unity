using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace StatusEffectFramework
{
    [Serializable]
    public class Effect
    {
        public StatusName StatusName => m_StatusName;
        public ValueModifier ValueModifier => m_ValueModifier;
        public ValueType ValueType => m_ValueType;
        public int Priority => m_Priority;
        public float FloatValue => m_FloatValue;
        public int IntValue => m_IntValue;
        public bool BoolValue => m_BoolValue;

        [SerializeField, FormerlySerializedAs("StatusName")]
        private StatusName m_StatusName;
        [SerializeField, FormerlySerializedAs("ValueModifier")]
        private ValueModifier m_ValueModifier;
        [SerializeField]
        private ValueType m_ValueType;
        [SerializeField, FormerlySerializedAs("Priority")]
        [Min(0)] private int m_Priority;
        [SerializeField, FormerlySerializedAs("FloatValue")]
        private float m_FloatValue;
        [SerializeField, FormerlySerializedAs("IntValue")]
        private int m_IntValue;
        [SerializeField, FormerlySerializedAs("BoolValue")]
        private bool m_BoolValue;
    }
}
