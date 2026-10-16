using System.Collections.Generic;
#if ADDRESSABLES
using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Collects the registrants that are kept out of the main <see cref="StatusRegistry"/>, such as content that is
    /// shipped separately in an Addressables group.
    /// </summary>
    public class StatusRegistryDependency : ScriptableObject
    {
        /// <summary>
        /// The <see cref="StatusEffectData"/>s that use this dependency.
        /// </summary>
        public IReadOnlyList<StatusEffectData> StatusEffectDatas => m_StatusEffectDatas;
        /// <summary>
        /// The <see cref="StatusName"/>s that use this dependency.
        /// </summary>
        public IReadOnlyList<StatusName> StatusNames => m_StatusNames;
        /// <summary>
        /// The <see cref="ComparableName"/>s that use this dependency.
        /// </summary>
        public IReadOnlyList<ComparableName> ComparableNames => m_ComparableNames;
        /// <summary>
        /// The <see cref="StatusEvent"/>s that use this dependency.
        /// </summary>
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