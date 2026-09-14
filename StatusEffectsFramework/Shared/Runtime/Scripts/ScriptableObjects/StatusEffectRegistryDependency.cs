using System.Collections.Generic;
#if ADDRESSABLES
using UnityEngine;

namespace StatusEffectsFramework
{
    public class StatusEffectRegistryDependency : ScriptableObject
    {
        public IReadOnlyList<StatusEffectData> StatusEffectDatas => m_StatusEffectDatas;
        public IReadOnlyList<StatusName> StatusNames => m_StatusNames;
        public IReadOnlyList<ComparableName> ComparableNames => m_ComparableNames;
        public IReadOnlyList<StatusEvent> StatusEvents => m_StatusEvents;

        [SerializeField, HideInInspector] 
        private List<StatusEffectData> m_StatusEffectDatas;
        [SerializeField, HideInInspector]
        private List<StatusName> m_StatusNames;
        [SerializeField, HideInInspector]
        private List<ComparableName> m_ComparableNames;
        [SerializeField, HideInInspector]
        private List<StatusEvent> m_StatusEvents;
    }
}
#endif