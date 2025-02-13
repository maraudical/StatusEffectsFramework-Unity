using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    public class StatusEffectDataPostProcessor : AssetPostprocessor
    {
        private static StatusEffectDatabase m_Database;
        private static StatusEffectData m_StatusEffectDataReference;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            m_Database = StatusEffectDatabase.Get();

            for (int i = m_Database.Values.Count - 1; i >= 0; i--)
            {
                var kvp = m_Database.Values.ElementAt(i);
                if (kvp.Value == null || kvp.Key != kvp.Value.Id)
                    m_Database.Values.Remove(kvp.Key);
            }

            if (didDomainReload)
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(StatusEffectData)}");
                var assetPaths = guids.Select((guid) => AssetDatabase.GUIDToAssetPath(guid));

                foreach (string assetPath in assetPaths)
                    ValidateAsset(assetPath);
            }
            else
            {
                foreach (string assetPath in importedAssets)
                    ValidateAsset(assetPath);
            }

            AssetDatabase.SaveAssetIfDirty(m_Database);

            void ValidateAsset(string assetPath)
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                if (asset == null)
                    return;

                if (asset is StatusEffectData data)
                {
                    if (data.Id != default)
                        if (m_Database.Values.TryGetValue(data.Id, out m_StatusEffectDataReference))
                        {
                            if (m_StatusEffectDataReference != data)
                                GenerateUntilAddable();
                        }
                        else
                        {
                            m_Database.Values.Add(data.Id, data);
                            EditorUtility.SetDirty(m_Database);
                        }
                    else
                        GenerateUntilAddable();

                    void GenerateUntilAddable()
                    {
                        data.GenerateId();
                        if (!m_Database.Values.TryAdd(data.Id, data))
                        {
                            GenerateUntilAddable();
                            return;
                        }
                        EditorUtility.SetDirty(m_Database);
                    }
                }
            }
        }
    }
}