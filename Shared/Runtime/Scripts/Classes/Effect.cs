using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace StatusEffects
{
    [Serializable]
    public class Effect
    {
        public StatusName StatusName => m_StatusName;
        public ValueModifier ValueModifier => m_ValueModifier;
        public int Priority => m_Priority;
        public bool UseBaseValue => m_UseBaseValue;
        public float FloatValue => m_FloatValue;
        public int IntValue => m_IntValue;
        public bool BoolValue => m_BoolValue;

        [SerializeField, FormerlySerializedAs("StatusName")]
        private StatusName m_StatusName;
        [SerializeField, FormerlySerializedAs("ValueModifier")]
        private ValueModifier m_ValueModifier;
        [SerializeField, FormerlySerializedAs("Priority")]
        [Min(0)] private int m_Priority;
        [SerializeField, FormerlySerializedAs("UseBaseValue")]
        private bool m_UseBaseValue;
        [SerializeField, FormerlySerializedAs("FloatValue")]
        private float m_FloatValue;
        [SerializeField, FormerlySerializedAs("IntValue")]
        private int m_IntValue;
        [SerializeField, FormerlySerializedAs("BoolValue")]
        private bool m_BoolValue;
    }
}
