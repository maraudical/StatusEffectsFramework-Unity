using System;
using System.Collections.Generic;
using UnityEngine;
#if LOCALIZATION_SUPPORT && !disableUnityLocalizationSupport
using UnityEngine.Localization;
#endif

namespace StatusEffects
{
    [CreateAssetMenu(fileName = "New Status Effect Data", menuName = "Status Effect Data", order = 1)]
    public class StatusEffectData : ScriptableObject
    {
        [GroupString] public string group;
        public new string name;
#if LOCALIZATION_SUPPORT && !disableUnityLocalizationSupport
        public LocalizedString localizedName;
        public LocalizedString localizedDescription;
#else
        [TextArea] public string description;
#endif
        [Space]
        public List<Effect> effects;
        [Space]
        public CustomEffect customEffect;
    }

    [Serializable]
    public class Effect
    {
        [StatusString] public string statusName;
        public ValueType valueType;
        public ValueModifier valueModifier;
        public float floatValue;
        public int intValue;
        public bool boolValue;
        [Min (0)] public int priority;
    }
}
