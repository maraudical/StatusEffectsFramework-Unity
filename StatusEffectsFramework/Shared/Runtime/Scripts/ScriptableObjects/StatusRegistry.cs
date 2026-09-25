using UnityEngine;
using System.Collections.Generic;
using System;
#if ENTITIES
using Hash128 = Unity.Entities.Hash128;
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using System.Linq;
#endif

namespace StatusEffectsFramework
{
    /// <summary>
    /// A ScriptableObject that serves as a registry for various types of status-related data. 
    /// It maintains a dictionary of keys and ids, allowing for efficient retrieval and 
    /// management of status effects in the game. The database can be dynamically updated 
    /// during runtime.
    /// </summary>
    public class StatusRegistry : ScriptableObject
#if UNITY_EDITOR
        , IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Preprocess();
        }

        [InitializeOnEnterPlayMode]
        private static void OnPlayModeStateChanged()
        {
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
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            
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
        public const string RegistryName = "StatusRegistry";
        public const string RegistryPath = "Assets/Settings/Resources/" + RegistryName + ".asset";

        public event Action RegistryRebuilt;

        public IReadOnlyDictionary<ushort, StatusEffectData> IdToStatusEffectData => m_IdToStatusEffectData;
        public IReadOnlyDictionary<ushort, StatusName> IdToStatusName => m_IdToStatusName;
        public IReadOnlyDictionary<ushort, ComparableName> IdToComparableName => m_IdToComparableName;
        public IReadOnlyDictionary<ushort, StatusEvent> IdToStatusEvent => m_IdToStatusEvent;
        public IReadOnlyDictionary<Hash128, ushort> KeyToId => m_KeyToId;

        private Dictionary<ushort, StatusEffectData> m_IdToStatusEffectData;
        private Dictionary<ushort, StatusName> m_IdToStatusName;
        private Dictionary<ushort, ComparableName> m_IdToComparableName;
        private Dictionary<ushort, StatusEvent> m_IdToStatusEvent;
        private Dictionary<Hash128, ushort> m_KeyToId;

        [SerializeField, HideInInspector]
        private List<StatusEffectData> m_StatusEffectDatas;
        [SerializeField, HideInInspector]
        private List<StatusName> m_StatusNames;
        [SerializeField, HideInInspector]
        private List<ComparableName> m_ComparableNames;
        [SerializeField, HideInInspector]
        private List<StatusEvent> m_StatusEvents;

#if ADDRESSABLES
        private HashSet<StatusRegistryDependency> m_Dependencies;

#endif
        public static StatusRegistry Get()
        {
            var registry = Resources.Load<StatusRegistry>(RegistryName);

#if UNITY_EDITOR
            if (registry == null)
            {
                registry = CreateInstance<StatusRegistry>();
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
                Debug.LogError($"{nameof(StatusRegistry)} could not be found at path {RegistryPath}. Please ensure that the registry exists and is located in the Resources folder.");
                return;
            }
            registry.Rebuild();
        }

        /// <summary>
        /// Rebuilds the registry by clearing existing keys/ids and repopulating them with the currently registred data.
        /// </summary>
        public void Rebuild()
        {
            m_KeyToId = new();

            ushort id = 0;

            m_IdToStatusEffectData = new();
            m_IdToStatusName = new();
            m_IdToComparableName = new();
            m_IdToStatusEvent = new();

            AddToDictionary(ref id, m_StatusEffectDatas, m_IdToStatusEffectData, m_KeyToId);
            AddToDictionary(ref id, m_StatusNames, m_IdToStatusName, m_KeyToId);
            AddToDictionary(ref id, m_ComparableNames, m_IdToComparableName, m_KeyToId);
            AddToDictionary(ref id, m_StatusEvents, m_IdToStatusEvent, m_KeyToId);

#if ADDRESSABLES
            if (m_Dependencies != null)
                foreach (var dependency in m_Dependencies)
                {
                    AddToDictionary(ref id, dependency.StatusEffectDatas, m_IdToStatusEffectData, m_KeyToId);
                    AddToDictionary(ref id, dependency.StatusNames, m_IdToStatusName, m_KeyToId);
                    AddToDictionary(ref id, dependency.ComparableNames, m_IdToComparableName, m_KeyToId);
                    AddToDictionary(ref id, dependency.StatusEvents, m_IdToStatusEvent, m_KeyToId);
                }
#endif
            RegistryRebuilt?.Invoke();

            void AddToDictionary<T>(ref ushort id, IEnumerable<T> list, Dictionary<ushort, T> idToItem, Dictionary<Hash128, ushort> keyToId) where T : Registrant
            {
                foreach (var item in list)
                {
                    if (item == null)
                        continue;

                    if (!keyToId.TryAdd(item.GetUniqueKeyHash(), id))
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
        public void RegisterDependency(StatusRegistryDependency dependency)
        {
            RegisterDependencyWithoutNotify(dependency);
            Rebuild();
        }

        /// <summary>
        /// Registers a dependency for the registry. 
        /// </summary>
        public void RegisterDependencyWithoutNotify(StatusRegistryDependency dependency)
        {
            if (m_Dependencies == null)
                m_Dependencies = new();

            if (!m_Dependencies.Contains(dependency))
                m_Dependencies.Add(dependency);
        }
#endif
    }
}
