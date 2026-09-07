using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StatusEffectsFramework
{
    // Create a new type of Database Asset.
    public class StatusEffectDatabase : ScriptableObject
    {
        public const string DatabaseName = "StatusEffectDatabase";
        public const string DatabasePath = "Assets/Settings/Resources/" + DatabaseName + ".asset";

        public event Action StatusEffectDatasRebuilt;
        
        public IReadOnlyDictionary<ushort, StatusEffectData> ReadOnlyDictionary => Values;

        internal Dictionary<ushort, StatusEffectData> Values;

        [SerializeField] private List<StatusEffectData> m_StatusEffectDatas;

/*#if UNITY_EDITOR
        public static bool IsLoadedDynamically(string guid)
        {
            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (addressableSettings != null && addressableSettings.FindAssetEntry(guid) != null)
                return true;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null && !string.IsNullOrEmpty(importer.assetBundleName))
                return true;

            return false;
        }
#endif

        public void Add(Hash128 key, StatusEffectData value)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                if (!Values.TryAdd(key, value))
                    Values[key] = value;
            }
            else
            {
                if (!HiddenValues.TryAdd(key, value))
                    HiddenValues[key] = value;
                if (value.AutomaticallyAddToDatabase)
                    if (!Values.TryAdd(key, value))
                        Values[key] = value;
            }
#else
             if (!Values.TryAdd(key, value))
                Values[key] = value;
#endif
        }

        public bool TryAdd(Hash128 key, StatusEffectData value)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                return Values.TryAdd(key, value);
            }
            else
            {
                bool added = HiddenValues.TryAdd(key, value);
                if (added && value.AutomaticallyAddToDatabase)
                    Values.TryAdd(key, value);
                return added;
            }
#else
            return Values.TryAdd(key, value);
#endif
        }
        /// <summary>
        /// Cannot remove during runtime due to issues with 
        /// destroying Netcode for Entities Ghosts.
        /// </summary>
        public void Remove(Hash128 key)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                Values.Remove(key);
                HiddenValues.Remove(key);
            }
#endif
        }

        public StatusEffectData GetValueOrDefault(Hash128 key)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                return Values.GetValueOrDefault(key);
            else
                return HiddenValues.GetValueOrDefault(key);
#else
            return Values.GetValueOrDefault(key);
#endif
        }

        public bool TryGetValue(Hash128 key, out StatusEffectData value)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                return Values.TryGetValue(key, out value);
            else
                return HiddenValues.TryGetValue(key, out value);
#else
            return Values.TryGetValue(key, out value);
#endif
        }

        public bool ContainsKey(Hash128 key)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                return Values.ContainsKey(key);
            else
                return HiddenValues.ContainsKey(key);
#else
            return Values.ContainsKey(key);
#endif
        }

        public bool IsUnique(Hash128 key)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                return !Values.ContainsKey(key);
            else
                return !HiddenValues.ContainsKey(key);
#else
            return !Values.ContainsKey(key);
#endif
        }

        public static StatusEffectDatabase Get()
        {
            var database = Resources.Load<StatusEffectDatabase>("StatusEffectDatabase");
#if UNITY_EDITOR
            if (database == null || database.Values == null)
            {
                database = CreateInstance<StatusEffectDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }
#endif
            return database;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [InitializeOnLoadMethod]
        private static async void SynchronizeValuesAndSave()
        {
            var initialTime = EditorApplication.timeSinceStartup;
            while (EditorApplication.timeSinceStartup - initialTime <= .05 || EditorApplication.isCompiling || EditorApplication.isUpdating)
                await Task.Yield();

            var database = Get();

            SynchronizeValues(database);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssetIfDirty(database);
        }

        private static void SynchronizeValues(StatusEffectDatabase database)
        {
            var values = database.HiddenValues.Where(kvp => kvp.Value.AutomaticallyAddToDatabase);

            database.Values.Clear();

            foreach (var value in values)
                database.Values.Add(value.Key, value.Value);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.ExitingPlayMode)
                SynchronizeValues(Get());
        }
#endif*/
    }
}
