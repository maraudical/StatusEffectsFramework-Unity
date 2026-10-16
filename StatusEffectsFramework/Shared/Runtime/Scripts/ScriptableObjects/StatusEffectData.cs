using System.Collections.Generic;
using UnityEngine;
#if LOCALIZED
using UnityEngine.Localization;
#endif

namespace StatusEffectsFramework
{
    /// <summary>
    /// A data container for everything related to the readonly values in a <see cref="StatusEffect"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "New Status Effect Data", menuName = "Status Effects Framework/Status Effect Data", order = -5)]
    public class StatusEffectData : Registrant
    {
        #region Public Properties
        public StatusEffectGroup Group => m_Group;
        public RegistrantKey<ComparableName> ComparableName => m_ComparableName;
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
        public IReadOnlyList<Effect> Effects => m_Effects;
        public IReadOnlyList<Condition> Conditions => m_Conditions;
        public IReadOnlyList<ModuleContainer> Modules => m_Modules;
        #endregion

        #region Private Fields
        [Tooltip("The groups this status effect is in. The groups are named in the Status Settings. Groups can be used to find or remove status effects, and in conditions.")]
        [SerializeField] private StatusEffectGroup m_Group;
        [Tooltip("This optional name can be used to categorize a series of Status Effects. For example, \"Poison\" may be used for multiple different poison effects. Leave empty if this effect is unique.")]
        [SerializeField] private RegistrantKey<ComparableName> m_ComparableName;
        [Tooltip("A value for this status effect. Effects can use it by setting their Value Source to Base Value. It is also what the Non-Stacking Behaviour compares.")]
        [SerializeField] private float m_BaseValue = 1f;
        [Tooltip("The icon to show for this status effect in your UI.")]
        [SerializeField] private Sprite m_Icon;
        [Tooltip("The color to show for this status effect in your UI.")]
        [SerializeField] private Color m_Color = Color.white;
#if LOCALIZED
        [SerializeField] private LocalizedString m_StatusEffectName;
        [SerializeField] private LocalizedString m_Acronym;
        [SerializeField] private LocalizedString m_Description;
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
        [Tooltip("The most stacks that can be active at once for this status effect. -1 is unlimited. Only used when Allow Effect Stacking is enabled.")]
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
