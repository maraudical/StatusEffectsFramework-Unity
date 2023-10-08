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
        [Space]
        [NonReorderable]
        [Tooltip("These are predefined statuses availiable for your effects to effect.")]
        public string[] statuses;
        [Space]
        [NonReorderable]
        public string[] groups;

        public static StatusEffectSettings GetOrCreateSettings()
        {
            var settings = Resources.Load<StatusEffectSettings>("StatusEffectSettings");
#if UNITY_EDITOR
            if (settings == null)
            {
                settings = CreateInstance<StatusEffectSettings>();
                settings.statuses = new string[] { "Max Health", "Speed", "Damage" };
                settings.groups = new string[32];
                settings.groups[0] = "Static";
                settings.groups[1] = "Negative";
                settings.groups[2] = "Positive";
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
