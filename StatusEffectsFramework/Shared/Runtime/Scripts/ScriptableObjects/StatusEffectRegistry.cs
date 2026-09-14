using UnityEngine;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
#endif

namespace StatusEffectsFramework
{
    /// <summary>
    /// A ScriptableObject that serves as a registry for various types of status-related data. 
    /// It maintains a dictionary of keys and ids, allowing for efficient retrieval and 
    /// management of status effects in the game. The database can be dynamically updated 
    /// during runtime.
    /// </summary>
    public class StatusEffectRegistry : ScriptableObject
#if UNITY_EDITOR
        , IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Preprocess();
        }

        [InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.EnteredPlayMode)
                Get().Preprocess();
        }

        private void Preprocess()
        {
            m_StatusEffectDatas = new();
            m_StatusNames = new();
            m_ComparableNames = new();
            m_StatusEvents = new();

            FindAssets(m_StatusEffectDatas);
            FindAssets(m_StatusNames);
            FindAssets(m_ComparableNames);
            FindAssets(m_StatusEvents);
            Debug.Log($"StatusEffectRegistry: Preprocessed {m_StatusEffectDatas.Count} StatusEffectDatas, {m_StatusNames.Count} StatusNames, {m_ComparableNames.Count} ComparableNames, and {m_StatusEvents.Count} StatusEvents.");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        public void FindAssets<T>(List<T> assets) where T : Registrant
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(T)}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null
#if ADDRESSABLES
                    && asset.OptionalRegistryDependency == null
#endif
                    )
                    assets.Add(asset);
            }
        }
#else
    {
#endif
        public const string RegistryName = "StatusEffectRegistry";
        public const string RegistryPath = "Assets/Settings/Resources/" + RegistryName + ".asset";

        public event Action RegistryRebuilt;

        public IReadOnlyDictionary<ushort, StatusEffectData> IdToStatusEffectData => m_IdToStatusEffectData;
        public IReadOnlyDictionary<string, ushort> KeyToIdForStatusEffectData => m_KeyToIdForStatusEffectData;

        public IReadOnlyDictionary<ushort, StatusName> IdToStatusName => m_IdToStatusName;
        public IReadOnlyDictionary<string, ushort> KeyToIdForStatusName => m_KeyToIdForStatusName;

        public IReadOnlyDictionary<ushort, ComparableName> IdToComparableName => m_IdToComparableName;
        public IReadOnlyDictionary<string, ushort> KeyToIdForComparableName => m_KeyToIdForComparableName;

        public IReadOnlyDictionary<ushort, StatusEvent> IdToStatusEvent => m_IdToStatusEvent;
        public IReadOnlyDictionary<string, ushort> KeyToIdForStatusEvent => m_KeyToIdForStatusEvent;

        private Dictionary<ushort, StatusEffectData> m_IdToStatusEffectData;
        private Dictionary<string, ushort> m_KeyToIdForStatusEffectData;

        private Dictionary<ushort, StatusName> m_IdToStatusName;
        private Dictionary<string, ushort> m_KeyToIdForStatusName;

        private Dictionary<ushort, ComparableName> m_IdToComparableName;
        private Dictionary<string, ushort> m_KeyToIdForComparableName;

        private Dictionary<ushort, StatusEvent> m_IdToStatusEvent;
        private Dictionary<string, ushort> m_KeyToIdForStatusEvent;

        public IReadOnlyList<StatusEffectData> StatusEffectDatas => m_StatusEffectDatas;
        public IReadOnlyList<StatusName> StatusNames => m_StatusNames;
        public IReadOnlyList<ComparableName> ComparableNames => m_ComparableNames;
        public IReadOnlyList<StatusEvent> StatusEvents => m_StatusEvents;

        [SerializeField, HideInInspector]
        private List<StatusEffectData> m_StatusEffectDatas;
        [SerializeField, HideInInspector]
        private List<StatusName> m_StatusNames;
        [SerializeField, HideInInspector]
        private List<ComparableName> m_ComparableNames;
        [SerializeField, HideInInspector]
        private List<StatusEvent> m_StatusEvents;

#if ADDRESSABLES
        private HashSet<StatusEffectRegistryDependency> m_Dependencies;

#endif
        public static StatusEffectRegistry Get()
        {
            var registry = Resources.Load<StatusEffectRegistry>(RegistryName);

#if UNITY_EDITOR
            if (registry == null)
            {
                registry = CreateInstance<StatusEffectRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
                AssetDatabase.SaveAssets();
            }

#endif
            return registry;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            var registry = Get();
            if (registry == null)
            {
                Debug.LogError($"{nameof(StatusEffectRegistry)} could not be found at path {RegistryPath}. Please ensure that the registry exists and is located in the Resources folder.");
                return;
            }
            registry.Rebuild();
        }

        /// <summary>
        /// Rebuilds the registry by clearing existing keys/ids and repopulating them with the currently registred data.
        /// </summary>
        public void Rebuild()
        {
            ushort statusEffectId = 0;
            m_IdToStatusEffectData = new();
            m_KeyToIdForStatusEffectData = new();

            AddToDictionary(ref statusEffectId, m_StatusEffectDatas, m_IdToStatusEffectData, m_KeyToIdForStatusEffectData);

            ushort statusNameId = 0;
            m_IdToStatusName = new();
            m_KeyToIdForStatusName = new();

            AddToDictionary(ref statusNameId, m_StatusNames, m_IdToStatusName, m_KeyToIdForStatusName);

            ushort comparableNameId = 0;
            m_IdToComparableName = new();
            m_KeyToIdForComparableName = new();

            AddToDictionary(ref comparableNameId, m_ComparableNames, m_IdToComparableName, m_KeyToIdForComparableName);

            ushort statusEventId = 0;
            m_IdToStatusEvent = new();
            m_KeyToIdForStatusEvent = new();

            AddToDictionary(ref statusEventId, m_StatusEvents, m_IdToStatusEvent, m_KeyToIdForStatusEvent);

#if ADDRESSABLES
            if (m_Dependencies != null)
                foreach (var dependency in m_Dependencies)
                {
                    AddToDictionary(ref statusEffectId, dependency.StatusEffectDatas, m_IdToStatusEffectData, m_KeyToIdForStatusEffectData);
                    AddToDictionary(ref statusNameId, dependency.StatusNames, m_IdToStatusName, m_KeyToIdForStatusName);
                    AddToDictionary(ref comparableNameId, dependency.ComparableNames, m_IdToComparableName, m_KeyToIdForComparableName);
                    AddToDictionary(ref statusEventId, dependency.StatusEvents, m_IdToStatusEvent, m_KeyToIdForStatusEvent);
                }
#endif
            RegistryRebuilt?.Invoke();

            void AddToDictionary<T>(ref ushort id, IEnumerable<T> list, Dictionary<ushort, T> idToItem, Dictionary<string, ushort> keyToId) where T : Registrant
            {
                foreach (var item in list)
                {
                    if (item == null)
                        continue;

                    if (!keyToId.TryAdd(item.UniqueKey, id))
                    {
                        Debug.LogWarning($"Duplicate key found: {item.UniqueKey}. Skipping registration for this {nameof(T)}.");
                        continue;
                    }
                    idToItem[id] = item;
                    id++;
                }
            }
        }
#if ADDRESSABLES

        /// <summary>
        /// Registers a dependency for the registry. Automatically calls <see cref="Rebuild"/> after 
        /// the dependency is added so if multiple dependencies are going to be added at once it is 
        /// better to call <see cref="RegisterDependencyWithoutNotify"/> for each one and then manually 
        /// invoke <see cref="Rebuild"/> once the loading of all dependenciesis complete.
        /// </summary>
        public void RegisterDependency(StatusEffectRegistryDependency dependency)
        {
            RegisterDependencyWithoutNotify(dependency);
            Rebuild();
        }

        /// <summary>
        /// Registers a dependency for the registry. 
        /// </summary>
        public void RegisterDependencyWithoutNotify(StatusEffectRegistryDependency dependency)
        {
            if (m_Dependencies == null)
                m_Dependencies = new();

            if (!m_Dependencies.Contains(dependency))
                m_Dependencies.Add(dependency);
        }
#endif
    }
}
