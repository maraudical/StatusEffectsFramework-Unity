using System.Collections.Generic;
using UnityEngine;
using StatusEffects.Modules;
using System.Collections.ObjectModel;
using System;
#if LOCALIZED
using UnityEngine.Localization;
#endif

namespace StatusEffects
{
    [CreateAssetMenu(fileName = "New Status Effect Data", menuName = "Status Effect Framework/Status Effect Data", order = -5)]
    public class StatusEffectData : ScriptableObject
    {
        public Hash128 Id => m_Id;
        [SerializeField] private Hash128 m_Id;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_Id == default)
            {
                GenerateAndImport();
            }
        }

        private void Reset()
        {
            GenerateAndImport();
        }

        [ContextMenu("Generate New ID")]
        private void GenerateAndImport()
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrWhiteSpace(path))
                return;
            GenerateId();
            UnityEditor.AssetDatabase.ImportAsset(path);
        }
        /// <summary>
        /// This will be called automatically from a post processor.
        /// </summary>
        internal void GenerateId()
        {
            m_Id = Hash128Extensions.Id();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }

#endif
        #region Public Properties
        internal bool AutomaticallyAddToDatabase => m_AutomaticallyAddToDatabase;
        public StatusEffectGroup Group => m_Group;
        public ComparableName ComparableName => m_ComparableName;
        public float BaseValue => m_BaseValue;
        public Sprite Icon => m_Icon;
        public Color Color => m_Color;
#if LOCALIZED
        public LocalizedString StatusEffectName => m_StatusEffectName;
        public LocalizedString Acronym => m_Acronym;
        public LocalizedString Description => m_Description;
#else
        public string StatusEffectName => m_StatusEffectName;
        public string Acronym => m_Acronym;
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
        [Tooltip("Unless you are trying to do something with Addressables and loading new Status Effect Data at runtime you can leave this checked on.")]
        [SerializeField] private bool m_AutomaticallyAddToDatabase = true;
        [SerializeField] private StatusEffectGroup m_Group;
        [Tooltip("This name can be used to categorize a series of Status Effects. For example, \"Poison\" may be used on multiple different poison effects.")]
        [SerializeField] private ComparableName m_ComparableName;
        [SerializeField] private float m_BaseValue = 1f;
        [SerializeField] private Sprite m_Icon;
        [SerializeField] private Color m_Color = Color.white;
#if LOCALIZED
        [SerializeField] private  LocalizedString m_StatusEffectName;
        [SerializeField] private LocalizedString m_Acronym;
        [SerializeField] private  LocalizedString m_Description;
#else
        [SerializeField] private string m_StatusEffectName;
        [SerializeField] private string m_Acronym;
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
        [SerializeField] internal List<ModuleContainer> m_Modules = new();
#if UNITY_EDITOR
#pragma warning disable 0414
        [SerializeField] private bool m_EnableIcon = false;
        [SerializeField] private bool m_EnableColor = false;
        [SerializeField] private bool m_EnableName = false;
        [SerializeField] private bool m_EnableDescription = false;
        [SerializeField] private bool m_EnableAcronym = false;
#pragma warning restore 0414
#endif
        #endregion
    }
}
