using UnityEngine;
using System.IO;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StatusEffects
{
    // Create a new type of Database Asset.
    public class StatusEffectDatabase : ScriptableObject
    {
        public const string k_MyCustomDatabasePath = "Assets/Resources/StatusEffectDatabase.asset";
        [Space]
        public SerializedDictionary<Hash128, StatusEffectData> Values;

        public static StatusEffectDatabase Get()
        {
            var database = Resources.Load<StatusEffectDatabase>("StatusEffectDatabase");
#if UNITY_EDITOR
            if (database == null || database.Values == null)
            {
                database = CreateInstance<StatusEffectDatabase>();
                Directory.CreateDirectory($"{Application.dataPath}/Resources");
                AssetDatabase.CreateAsset(database, k_MyCustomDatabasePath);
                AssetDatabase.SaveAssets();
            }
#endif
            return database;
        }
    }
}
