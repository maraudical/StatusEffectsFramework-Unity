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
        public const string MyCustomSettingsPath = "Assets/Resources/StatusEffectSettings.asset";
        [Space]
        [NonReorderable]
        public string[] Groups = new string[32];

        [SerializeField]
        public string DefaultStatusDataPath = "ScriptableObjects/StatusEffectData";
        [SerializeField]
        public string DefaultStatusNamesPath = "ScriptableObjects/StatusNames";
        [SerializeField]
        public string DefaultComparableNamesPath = "ScriptableObjects/ComparableNames";
        [SerializeField]
        public string DefaultModulesPath = "ScriptableObjects/Modules";

        public static StatusEffectSettings GetOrCreateSettings()
        {
            var settings = Resources.Load<StatusEffectSettings>("StatusEffectSettings");
#if UNITY_EDITOR
            if (settings == null || settings.Groups == null || settings.Groups.Length != 32)
            {
                settings = CreateInstance<StatusEffectSettings>();
                settings.Groups = new string[32];
                settings.Groups[0] = "Static";
                settings.Groups[1] = "Negative";
                settings.Groups[2] = "Positive";
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources"));
                AssetDatabase.CreateAsset(settings, MyCustomSettingsPath);
            }
#endif
            return settings;
        }
#if UNITY_EDITOR
        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
#endif
    }
}