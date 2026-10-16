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
    {
        /// <summary>
        /// Refreshes the registry asset before a build. Unity creates its own instance of every build
        /// callback type, so this is kept off <see cref="StatusRegistry"/> and preprocesses the asset itself.
        /// </summary>
        internal sealed class BuildPreprocessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report)
            {
                Get().Preprocess();
            }
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
        
        private void FindAssets<T>(List<T> assets) where T : Registrant
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
        /// <summary>
        /// The name of the registry asset, which is also what it is loaded by from Resources.
        /// </summary>
        public const string RegistryName = "StatusRegistry";
        /// <summary>
        /// Where the registry asset is created in the editor. It must be in a Resources folder.
        /// </summary>
        public const string RegistryPath = "Assets/Settings/Resources/" + RegistryName + ".asset";
        /// <summary>
        /// Id that is never assigned to a <see cref="Registrant"/>, used to mean no reference.
        /// Registered ids start after it.
        /// </summary>
        public const ushort NullId = 0;

        /// <summary>
        /// Raised at the end of <see cref="Rebuild"/>, once the ids have been reassigned.
        /// </summary>
        public event Action RegistryRebuilt;

        /// <summary>
        /// The registered <see cref="StatusEffectData"/> by id.
        /// </summary>
        public IReadOnlyDictionary<ushort, StatusEffectData> IdToStatusEffectData => m_IdToStatusEffectData;
        /// <summary>
        /// The registered <see cref="StatusName"/>s by id.
        /// </summary>
        public IReadOnlyDictionary<ushort, StatusName> IdToStatusName => m_IdToStatusName;
        /// <summary>
        /// The registered <see cref="ComparableName"/>s by id.
        /// </summary>
        public IReadOnlyDictionary<ushort, ComparableName> IdToComparableName => m_IdToComparableName;
        /// <summary>
        /// The registered <see cref="StatusEvent"/>s by id.
        /// </summary>
        public IReadOnlyDictionary<ushort, StatusEvent> IdToStatusEvent => m_IdToStatusEvent;
        /// <summary>
        /// The id of each registered <see cref="Registrant"/> by the hash of its key. See <see cref="Registrant.GetKeyHash"/>.
        /// </summary>
        public IReadOnlyDictionary<Hash128, ushort> KeyToId => m_KeyToId;
        /// <summary>
        /// Hash of every registered <see cref="Registrant.Key"/> in id order. Two registries
        /// with the same hash assign the same ids. Updated on every <see cref="Rebuild"/>.
        /// </summary>
        public Hash128 RegistryHash { get; private set; }
        /// <summary>
        /// <see cref="RegistryHash"/> as text, so assemblies that don't reference Entities can compare registries.
        /// </summary>
        public string RegistryHashString => RegistryHash.ToString();
        /// <summary>
        /// Gets the id assigned to a <see cref="Registrant"/>. Returns false if it isn't registered.
        /// </summary>
        public bool TryGetId(Registrant registrant, out ushort id)
        {
            id = NullId;
            return registrant && m_KeyToId != null && m_KeyToId.TryGetValue(registrant.GetKeyHash(), out id);
        }
        /// <summary>
        /// Gets the id assigned to a <see cref="Registrant.Key"/>. Returns false if it isn't registered.
        /// </summary>
        public bool TryGetId(string key, out ushort id)
        {
            id = NullId;
            return !string.IsNullOrEmpty(key) && m_StringKeyToId != null && m_StringKeyToId.TryGetValue(key, out id);
        }
        /// <summary>
        /// Gets the registered <typeparamref name="T"/> with a <see cref="Registrant.Key"/>. Returns false if
        /// nothing is registered with the key or the registrant with it isn't a <typeparamref name="T"/>.
        /// </summary>
        public bool TryGet<T>(string key, out T registrant) where T : Registrant
        {
            registrant = null;

            // Nothing is registered until the first rebuild, so there is nothing to report yet either.
            if (string.IsNullOrEmpty(key) || m_StringKeyToId == null)
                return false;

            if (!m_StringKeyToId.TryGetValue(key, out var id) || !m_IdToRegistrant.TryGetValue(id, out var item))
            {
                // The same missing reference is usually looked up repeatedly, so it is only reported once.
                if (m_MissingKeys.Add(key))
                    Debug.LogWarning($"Nothing is registered in the {nameof(StatusRegistry)} with the unique key \"{key}\". If it comes from separately built content, like a mod, make sure that content is loaded.");
                return false;
            }

            registrant = item as T;
            return registrant is not null;
        }

        private Dictionary<ushort, StatusEffectData> m_IdToStatusEffectData;
        private Dictionary<ushort, StatusName> m_IdToStatusName;
        private Dictionary<ushort, ComparableName> m_IdToComparableName;
        private Dictionary<ushort, StatusEvent> m_IdToStatusEvent;
        private Dictionary<Hash128, ushort> m_KeyToId;
        private Dictionary<string, ushort> m_StringKeyToId;
        private Dictionary<ushort, Registrant> m_IdToRegistrant;
        private readonly HashSet<string> m_MissingKeys = new(StringComparer.Ordinal);

        [SerializeField, HideInInspector]
        private List<StatusEffectData> m_StatusEffectDatas;
        [SerializeField, HideInInspector]
        private List<StatusName> m_StatusNames;
        [SerializeField, HideInInspector]
        private List<ComparableName> m_ComparableNames;
        [SerializeField, HideInInspector]
        private List<StatusEvent> m_StatusEvents;

#if ADDRESSABLES
        private List<StatusRegistryDependency> m_Dependencies;

#endif
        /// <summary>
        /// The registry, which is loaded from Resources. In the editor it is created when missing.
        /// </summary>
        public static StatusRegistry Instance { get => s_Instance != null ? s_Instance : Get(); }
        private static StatusRegistry s_Instance;
        private static StatusRegistry Get()
        {
            s_Instance = Resources.Load<StatusRegistry>(RegistryName);

#if UNITY_EDITOR
            if (s_Instance == null)
            {
                s_Instance = CreateInstance<StatusRegistry>();
                AssetDatabase.CreateAsset(s_Instance, RegistryPath);
                AssetDatabase.SaveAssets();
            }

#endif
            return s_Instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            if (Instance == null)
            {
                Debug.LogError($"{nameof(StatusRegistry)} could not be found at path {RegistryPath}. Please ensure that the registry exists and is located in the Resources folder.");
                return;
            }
            Instance.Rebuild();
        }

        /// <summary>
        /// Rebuilds the registry by clearing existing keys/ids and repopulating them with the currently registred data.
        /// </summary>
        /// <remarks>
        /// Every <see cref="Registrant"/> is collected into one list and sorted by its
        /// <see cref="Registrant.Key"/>, so ids don't depend on the order assets
        /// were found or dependencies were registered.
        /// </remarks>
        public void Rebuild()
        {
            var entries = new List<RegistryEntry>();

            CollectEntries(entries, m_StatusEffectDatas, RegistrantType.StatusEffectData);
            CollectEntries(entries, m_StatusNames, RegistrantType.StatusName);
            CollectEntries(entries, m_ComparableNames, RegistrantType.ComparableName);
            CollectEntries(entries, m_StatusEvents, RegistrantType.StatusEvent);

#if ADDRESSABLES
            if (m_Dependencies != null)
                foreach (var dependency in m_Dependencies)
                {
                    if (dependency == null)
                        continue;

                    CollectEntries(entries, dependency.StatusEffectDatas, RegistrantType.StatusEffectData);
                    CollectEntries(entries, dependency.StatusNames, RegistrantType.StatusName);
                    CollectEntries(entries, dependency.ComparableNames, RegistrantType.ComparableName);
                    CollectEntries(entries, dependency.StatusEvents, RegistrantType.StatusEvent);
                }
#endif
            // Duplicate keys are tie broken by type, then asset name, then collection order
            // so which duplicate wins is the same everywhere the same assets are registered.
            entries.Sort((x, y) =>
            {
                int comparison = string.CompareOrdinal(x.Item.Key, y.Item.Key);
                if (comparison != 0)
                    return comparison;
                comparison = x.Type.CompareTo(y.Type);
                if (comparison != 0)
                    return comparison;
                comparison = string.CompareOrdinal(x.Item.name, y.Item.name);
                if (comparison != 0)
                    return comparison;
                return x.Order.CompareTo(y.Order);
            });

            m_KeyToId = new();
            m_StringKeyToId = new(StringComparer.Ordinal);
            m_IdToRegistrant = new();
            m_MissingKeys.Clear();
            m_IdToStatusEffectData = new();
            m_IdToStatusName = new();
            m_IdToComparableName = new();
            m_IdToStatusEvent = new();

            var registryHash = new UnityEngine.Hash128();
            ushort id = NullId + 1;

            foreach (var entry in entries)
            {
                var item = entry.Item;

                // The id wrapped around past ushort.MaxValue so there are no ids left to assign.
                if (id == NullId)
                {
                    Debug.LogError($"The {nameof(StatusRegistry)} ran out of ids. Skipping registration for this {entry.Type} {item.name}.");
                    continue;
                }

                if (!m_KeyToId.TryAdd(item.GetKeyHash(), id))
                {
                    Debug.LogWarning($"Duplicate key found: {item.Key}. Skipping registration for this {entry.Type} {item.name}.");
                    continue;
                }

                m_StringKeyToId[item.Key ?? string.Empty] = id;
                m_IdToRegistrant[id] = item;

                switch (entry.Type)
                {
                    case RegistrantType.StatusEffectData:
                        m_IdToStatusEffectData[id] = (StatusEffectData)item;
                        break;
                    case RegistrantType.StatusName:
                        m_IdToStatusName[id] = (StatusName)item;
                        break;
                    case RegistrantType.ComparableName:
                        m_IdToComparableName[id] = (ComparableName)item;
                        break;
                    case RegistrantType.StatusEvent:
                        m_IdToStatusEvent[id] = (StatusEvent)item;
                        break;
                }

                // Ids are assigned in order, so hashing the keys in order covers which key every id maps to.
                registryHash.Append(item.Key ?? string.Empty);

                id++;
            }

            RegistryHash = registryHash;

            RegistryRebuilt?.Invoke();

            static void CollectEntries<T>(List<RegistryEntry> entries, IEnumerable<T> items, RegistrantType type) where T : Registrant
            {
                if (items == null)
                    return;

                foreach (var item in items)
                    if (item != null)
                        entries.Add(new RegistryEntry(item, type, entries.Count));
            }
        }

        private enum RegistrantType
        {
            StatusEffectData,
            StatusName,
            ComparableName,
            StatusEvent,
        }

        private readonly struct RegistryEntry
        {
            public readonly Registrant Item;
            public readonly RegistrantType Type;
            /// <summary>
            /// The order the entry was collected in, only used as a last tie break.
            /// </summary>
            public readonly int Order;

            public RegistryEntry(Registrant item, RegistrantType type, int order)
            {
                Item = item;
                Type = type;
                Order = order;
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
