using System.Collections.Generic;
using UnityEngine;
using StatusEffects.Modules;
using System.Collections.ObjectModel;
#if LOCALIZED
using UnityEngine.Localization;
#endif

namespace StatusEffects
{
    [CreateAssetMenu(fileName = "New Status Effect Data", menuName = "Status Effect Framework/Status Effect Data", order = 1)]
    public class StatusEffectData : ScriptableObject
    {
#if ENTITIES && ADDRESSABLES
        public string Id => m_Id;
        [SerializeField] private string m_Id;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(m_Id))
                GenerateId();
        }

        private void Reset()
        {
            GenerateId();
        }
        /// <summary>
        /// This will be called automatically from a post processor.
        /// </summary>
        public void GenerateId()
        {
#if UNITY_EDITOR
            m_Id = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

#endif
        #region Public Properties
        public StatusEffectGroup Group => m_Group;
        public ComparableName ComparableName => m_ComparableName;
        public float BaseValue => m_BaseValue;
        public Sprite Icon => m_Icon;
#if LOCALIZED
        public LocalizedString StatusEffectName => m_StatusEffectName;
        public LocalizedString Description => m_Description;
#else
        public string StatusEffectName => m_StatusEffectName;
        public string Description => m_Description;
#endif
        public bool AllowEffectStacking => m_AllowEffectStacking;
        public NonStackingBehaviour NonStackingBehaviour => m_NonStackingBehaviour;
        public int MaxStacks => m_MaxStacks;
        public ReadOnlyCollection<Effect> Effects => m_Effects.AsReadOnly();
        public ReadOnlyCollection<Condition> Conditions => m_Conditions.AsReadOnly();
        public ReadOnlyCollection<ModuleContainer> Modules => m_Modules.AsReadOnly();
        #endregion

        #region Private Fields
        [SerializeField] private StatusEffectGroup m_Group;
        [Tooltip("This name can be used to categorize a series of Status Effects. For example, \"Poison\" may be used on multiple different poison effects.")]
        [SerializeField] private ComparableName m_ComparableName;
        [SerializeField] private float m_BaseValue = 1f;
        [SerializeField] private Sprite m_Icon;
#if LOCALIZED
        [SerializeField] private  LocalizedString m_StatusEffectName;
        [SerializeField] private  LocalizedString m_Description;
#else
        [SerializeField] private string m_StatusEffectName;
        [TextArea]
        [SerializeField] private string m_Description;
#endif
        [Tooltip("If you want effects to be applied multiple times on the same MonoBehaviour enable this.")]
        [SerializeField] private bool m_AllowEffectStacking;
        [Tooltip("Depending on the Non-Stacking Behaviour, the value and/or the duration may be adjusted when the effect already exists. See the documentation for more information.")]
        [SerializeField] private NonStackingBehaviour m_NonStackingBehaviour;
        [Min(-1)]
        [SerializeField] private int m_MaxStacks = -1;
        [Tooltip("The effect list will apply value adjustments to the given Status.")]
        [SerializeField] private List<Effect> m_Effects = new();
        [Tooltip("Conditionals will be checked when a Status Effect is added. This way you can automatically add or remove other effects with ease.")]
        [SerializeField] private List<Condition> m_Conditions = new();
        [Tooltip("Modules will apply additional functionality to a Status Effect. This is not required.")]
        [SerializeField] private List<ModuleContainer> m_Modules = new();
#if UNITY_EDITOR
#pragma warning disable 0414
        [SerializeField] private bool m_EnableIcon = true;
        [SerializeField] private bool m_EnableName = true;
        [SerializeField] private bool m_EnableDescription = true;
#pragma warning restore 0414
#endif
#endregion
    }
}
