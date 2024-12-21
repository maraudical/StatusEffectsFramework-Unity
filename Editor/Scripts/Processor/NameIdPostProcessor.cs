using System.Collections.Generic;
using UnityEditor;

namespace StatusEffects.Inspector
{
    public class NameIdPostProcessor : AssetPostprocessor
    {
#if UNITY_EDITOR
        private static Dictionary<string, Name> s_NameIds;
#endif
        private static Name m_Reference;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            int importedCount = importedAssets.Length;
            for (int i = 0; i < importedCount; i++)
            {
                string assetPath = importedAssets[i];
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                
                if (asset != null && asset is Name name)
                {
                    if (s_NameIds == null)
                        s_NameIds = new Dictionary<string, Name>();

                    if (!string.IsNullOrWhiteSpace(name.Id))
                        if (s_NameIds.TryGetValue(name.Id, out m_Reference) && m_Reference != name)
                            GenerateUntilAddable();
                        else
                            s_NameIds.Add(name.Id, name);
                    else
                        GenerateUntilAddable();

                    void GenerateUntilAddable()
                    {
                        name.GenerateId();
                        if (!s_NameIds.TryAdd(name.Id, name)
)                           GenerateUntilAddable();
                    }
                }
            }
        }
    }
}