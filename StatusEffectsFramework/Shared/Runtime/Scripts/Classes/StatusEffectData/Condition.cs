using System;
using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// A rule of a <see cref="StatusEffectData"/> that is checked when its <see cref="StatusEffect"/> is added. If a status effect
    /// (the searchable) exists or doesn't exist, another (the action) is added or removed.
    /// </summary>
    [Serializable]
    public class Condition
    {
        public ConditionalConfigurable SearchableConfigurable => m_SearchableConfigurable;
        public RegistrantKey<StatusEffectData> SearchableData => m_SearchableData;
        public RegistrantKey<ComparableName> SearchableComparableName => m_SearchableComparableName;
        public StatusEffectGroup SearchableGroup => m_SearchableGroup;
        public bool IsPresent => m_IsPresent;
        public bool IsAdded => m_IsAdded;
        public bool IsScaled => m_IsScaled;
        public bool UseStacks => m_UseStacks;
        public int Stacks => m_Stacks;
        public ConditionalConfigurable ActionConfigurable => m_ActionConfigurable;
        public RegistrantKey<StatusEffectData> ActionData => m_ActionData;
        public RegistrantKey<ComparableName> ActionComparableName => m_ActionComparableName;
        public StatusEffectGroup ActionGroup => m_ActionGroup;
        public ConditionalTiming Timing => m_Timing;
        public float Duration => m_Duration;

        [SerializeField]
        private ConditionalConfigurable m_SearchableConfigurable;
        [SerializeField]
        private RegistrantKey<StatusEffectData> m_SearchableData;
        [SerializeField]
        private RegistrantKey<ComparableName> m_SearchableComparableName;
        [SerializeField]
        private StatusEffectGroup m_SearchableGroup;
        [SerializeField]
        private bool m_IsPresent = true;
        [SerializeField]
        private bool m_IsAdded = true;
        [Tooltip("When enabled, the stacks of this condition are multiplied by the stacks of the status effect that is being added.")]
        [SerializeField]
        private bool m_IsScaled = true;
        [SerializeField]
        private bool m_UseStacks;
        [SerializeField]
        [Min(1)] private int m_Stacks = 1;
        [SerializeField]
        private ConditionalConfigurable m_ActionConfigurable;
        [SerializeField]
        private RegistrantKey<StatusEffectData> m_ActionData;
        [SerializeField]
        private RegistrantKey<ComparableName> m_ActionComparableName;
        [SerializeField]
        private StatusEffectGroup m_ActionGroup;
        [SerializeField]
        private ConditionalTiming m_Timing;
        [SerializeField]
        [Min(0)] private float m_Duration;
    }
}
