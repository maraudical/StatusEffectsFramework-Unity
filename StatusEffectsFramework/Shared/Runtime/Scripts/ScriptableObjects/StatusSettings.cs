using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StatusEffectsFramework
{
    // Create a new type of Settings Asset.
    public class StatusSettings : ScriptableObject
    {
        public const string SettingsName = "StatusSettings";
        public const string SettingsPath = "Assets/Settings/Resources/" + SettingsName + ".asset";

        [Space]
        [NonReorderable]
        public string[] Groups = new string[32];

        public string DefaultUniqueKeyNamespace = "base";
        public string DefaultStatusDataPath = "ScriptableObjects/StatusEffectData";
        public string DefaultStatusNamesPath = "ScriptableObjects/StatusNames";
        public string DefaultComparableNamesPath = "ScriptableObjects/ComparableNames";
        public string DefaultModulesPath = "ScriptableObjects/Modules";
        public string DefaultDynamicEffectsPath = "ScriptableObjects/DynamicEffects";

        public static StatusSettings Instance { get => s_Instance != null ? s_Instance : Get(); }
        private static StatusSettings s_Instance;
        private static StatusSettings Get()
        {
            s_Instance = Resources.Load<StatusSettings>(SettingsName);
#if UNITY_EDITOR
            if (s_Instance == null || s_Instance.Groups == null || s_Instance.Groups.Length != 32)
            {
                s_Instance = CreateInstance<StatusSettings>();
                s_Instance.Groups = new string[32];
                s_Instance.Groups[0] = "Static";
                s_Instance.Groups[1] = "Negative";
                s_Instance.Groups[2] = "Positive";
                AssetDatabase.CreateAsset(s_Instance, SettingsPath);
                AssetDatabase.SaveAssets();
            }
#endif
            return s_Instance;
        }
        
#if UNITY_EDITOR
        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(Instance);
        }
#endif
    }
}