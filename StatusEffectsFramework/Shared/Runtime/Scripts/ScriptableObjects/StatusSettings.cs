using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StatusEffectsFramework
{
    /// <summary>
    /// The project wide settings of the framework, such as the names of the groups.
    /// </summary>
    public class StatusSettings : ScriptableObject
    {
        public const string SettingsName = "StatusSettings";
        public const string SettingsPath = "Assets/Settings/Resources/" + SettingsName + ".asset";

        [Tooltip("The names of the groups a status effect can be in. A status effect stores its groups as flags, so there are 32.")]
        [Space]
        [NonReorderable]
        public string[] Groups = new string[32];

        [Tooltip("The namespace new keys start with, for example \"base\" gives \"base:poison\".")]
        public string DefaultKeyNamespace = "base";
        [Tooltip("The folder, relative to Assets, that is opened when creating new Status Effect Data.")]
        public string DefaultStatusDataPath = "ScriptableObjects/StatusEffectData";
        [Tooltip("The folder, relative to Assets, that is opened when creating new Status Names.")]
        public string DefaultStatusNamesPath = "ScriptableObjects/StatusNames";
        [Tooltip("The folder, relative to Assets, that is opened when creating new Comparable Names.")]
        public string DefaultComparableNamesPath = "ScriptableObjects/ComparableNames";
        [Tooltip("The folder, relative to Assets, that is opened when creating new Modules.")]
        public string DefaultModulesPath = "ScriptableObjects/Modules";
        [Tooltip("The folder, relative to Assets, that is opened when creating new Dynamic Effects.")]
        public string DefaultDynamicEffectsPath = "ScriptableObjects/DynamicEffects";

        /// <summary>
        /// The settings asset, which is loaded from Resources. In the editor it is created when missing.
        /// </summary>
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