using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    public class NamePostProcessor : AssetPostprocessor
    {
        private static Dictionary<Hash128, Name> s_NameIds;
        private static Name m_NameReference;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            int importedCount = importedAssets.Length;
            for (int i = 0; i < importedCount; i++)
            {
                if (i == 0)
                    s_NameIds = new();

                string assetPath = importedAssets[i];
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                if (asset == null)
                    continue;

                if (asset is Name name)
                {
                    if (name.Id != default)
                        if (s_NameIds.TryGetValue(name.Id, out m_NameReference) && m_NameReference != name)
                            GenerateUntilAddable();
                        else
                            s_NameIds.Add(name.Id, name);
                    else
                        GenerateUntilAddable();

                    void GenerateUntilAddable()
                    {
                        name.GenerateId();
                        if (!s_NameIds.TryAdd(name.Id, name))                           
                            GenerateUntilAddable();
                    }
                }
            }
        }
    }
}