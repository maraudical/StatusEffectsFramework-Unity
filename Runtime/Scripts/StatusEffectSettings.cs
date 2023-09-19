using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StatusEffects
{
    // Create a new type of Settings Asset.
    public class StatusEffectSettings : ScriptableObject
    {
        public const string k_MyCustomSettingsPath = "Assets/Plugins/Status Effects Framework/Resources/Settings/StatusEffectSettings.asset";

        [Tooltip("If you want effects to be applied multiple times on the same MonoBehaviour enable this.")]
        public bool allowEffectStacking;
#if LOCALIZATION_SUPPORT
        public bool disableUnityLocalizationSupport;
#endif
        [Space]
        public string[] groups;
        [Space]
        [Tooltip("These are predefined statuses availiable for your effects to effect.")]
        public string[] statuses;

        public static StatusEffectSettings GetOrCreateSettings()
        {
            var settings = Resources.Load<StatusEffectSettings>("Settings/StatusEffectSettings");
#if UNITY_EDITOR
            if (settings == null)
            {
                settings = CreateInstance<StatusEffectSettings>();
                settings.allowEffectStacking = false;
#if LOCALIZATION_SUPPORT
                settings.disableUnityLocalizationSupport = false;
#endif
                settings.groups = new string[] { "Static", "Negative", "Positive" };
                settings.statuses = new string[] { "Max Health", "Speed", "Damage" };
                AssetDatabase.CreateAsset(settings, k_MyCustomSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
#endif
    }
}
