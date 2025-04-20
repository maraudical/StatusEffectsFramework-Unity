using System;
using UnityEngine;
using UnityEngine.Serialization;
#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
using UnityEngine.AddressableAssets;
#endif

namespace StatusEffects
{
    [Serializable]
    public class Condition
    {
        public ConditionalConfigurable SearchableConfigurable => m_SearchableConfigurable;
        public StatusEffectData SearchableData => m_SearchableData;
        public ComparableName SearchableComparableName => m_SearchableComparableName;
        public StatusEffectGroup SearchableGroup => m_SearchableGroup;
        public bool Exists => m_Exists;
        public bool Add => m_Add;
        public bool Scaled => m_Scaled;
        public bool UseStacks => m_UseStacks;
        public int Stacks => m_Stacks;
        public ConditionalConfigurable ActionConfigurable => m_ActionConfigurable;
        public StatusEffectData ActionData => m_ActionData;
        public ComparableName ActionComparableName => m_ActionComparableName;
        public StatusEffectGroup ActionGroup => m_ActionGroup;
        public ConditionalTiming Timing => m_Timing;
        public float Duration => m_Duration;

        [SerializeField, FormerlySerializedAs("SearchableConfigurable")]
        private ConditionalConfigurable m_SearchableConfigurable;
        [SerializeField, FormerlySerializedAs("SearchableData")]
        private StatusEffectData m_SearchableData;
        [SerializeField, FormerlySerializedAs("SearchableComparableName")]
        private ComparableName m_SearchableComparableName;
        [SerializeField, FormerlySerializedAs("SearchableGroup")]
        private StatusEffectGroup m_SearchableGroup;
        [SerializeField, FormerlySerializedAs("Exists")]
        private bool m_Exists = true;
        [SerializeField, FormerlySerializedAs("Add")]
        private bool m_Add = true;
        [SerializeField, FormerlySerializedAs("Scaled")]
        private bool m_Scaled = true;
        [SerializeField, FormerlySerializedAs("UseStacks")]
        private bool m_UseStacks;
        [SerializeField, FormerlySerializedAs("Stacks")]
        [Min(1)] private int m_Stacks = 1;
        [SerializeField, FormerlySerializedAs("ActionConfigurable")]
        private ConditionalConfigurable m_ActionConfigurable;
        [SerializeField, FormerlySerializedAs("ActionData")]
        private StatusEffectData m_ActionData;
        [SerializeField, FormerlySerializedAs("ActionComparableName")]
        private ComparableName m_ActionComparableName;
        [SerializeField, FormerlySerializedAs("ActionGroup")]
        private StatusEffectGroup m_ActionGroup;
        [SerializeField, FormerlySerializedAs("Timing")]
        private ConditionalTiming m_Timing;
        [SerializeField, FormerlySerializedAs("Duration")]
        [Min(0)] private float m_Duration;
    }
}
