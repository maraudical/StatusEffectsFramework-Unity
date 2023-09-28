using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StatusEffects
{
    // Create a new type of Settings Asset.
    public class StatusEffectSettings : ScriptableObject
    {
        public const string k_MyCustomSettingsPath = "Assets/Resources/StatusEffectSettings.asset";
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
            var settings = Resources.Load<StatusEffectSettings>("StatusEffectSettings");
#if UNITY_EDITOR
            if (settings == null)
            {
                settings = CreateInstance<StatusEffectSettings>();
#if LOCALIZATION_SUPPORT
                settings.disableUnityLocalizationSupport = false;
#endif
                settings.groups = new string[] { "Static", "Negative", "Positive" };
                settings.statuses = new string[] { "Max Health", "Speed", "Damage" };
                Directory.CreateDirectory($"{Application.dataPath}/Resources");
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
