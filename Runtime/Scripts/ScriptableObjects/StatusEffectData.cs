using System.Collections.Generic;
using UnityEngine;
using StatusEffects.Modules;
#if LOCALIZED
using UnityEngine.Localization;
#endif

namespace StatusEffects
{
    [CreateAssetMenu(fileName = "New Status Effect Data", menuName = "Status Effect Framework/Status Effect Data", order = 1)]
    public class StatusEffectData : ScriptableObject
    {
        public StatusEffectGroup group;
        [Tooltip("This name can be used to categorize a series of Status Effects. For example, \"Poison\" may be used on multiple different poison effects.")]
        public ComparableName comparableName;
        public float baseValue = 1f;
        public Sprite icon;
#if LOCALIZED
        public LocalizedString statusEffectName;
        public LocalizedString description;
#else
        public string statusEffectName;
        [TextArea]
        public string description;
#endif
        [Tooltip("If you want effects to be applied multiple times on the same MonoBehaviour enable this.")]
        public bool allowEffectStacking;
        [Tooltip("Depending on the Non-Stacking Behaviour, the value and/or the duration may be adjusted when the effect already exists. See the documentation for more information.")]
        public NonStackingBehaviour nonStackingBehaviour;
        [Min(-1)]
        public int maxStack = -1;
        [Tooltip("The effect list will apply value adjustments to the given Status.")]
        public List<Effect> effects = new();
        [Tooltip("Conditionals will be checked when a Status Effect is added. This way you can automatically add or remove other effects with ease.")]
        public List<Condition> conditions = new();
        [Tooltip("Modules will apply additional functionality to a Status Effect. This is not required.")]
        public List<ModuleContainer> modules = new();
#if UNITY_EDITOR
#pragma warning disable 0414
        [SerializeField] private bool enableIcon = true;
        [SerializeField] private bool enableName = true;
        [SerializeField] private bool enableDescription = true;
#pragma warning restore 0414
#endif
    }
}
