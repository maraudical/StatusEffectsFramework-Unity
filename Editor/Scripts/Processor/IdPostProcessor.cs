using System.Collections.Generic;
using UnityEditor;

namespace StatusEffects.Inspector
{
    public class IdPostProcessor : AssetPostprocessor
    {
        private static Dictionary<string, Name> s_NameIds;
        private static Name m_NameReference;

        private static Dictionary<string, StatusEffectData> s_StatusEffectDataIds;
        private static StatusEffectData m_StatusEffectDataReference;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            int importedCount = importedAssets.Length;
            for (int i = 0; i < importedCount; i++)
            {
                if (i == 0)
                {
                    s_NameIds = new Dictionary<string, Name>();
#if ENTITIES && ADDRESSABLES
                    s_StatusEffectDataIds = new Dictionary<string, StatusEffectData>();
#endif
                }

                string assetPath = importedAssets[i];
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

                if (asset == null)
                    continue;

                if (asset is Name name)
                {
                    if (!string.IsNullOrWhiteSpace(name.Id))
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
#if ENTITIES && ADDRESSABLES
                else if ( asset is StatusEffectData data)
                {
                    if (!string.IsNullOrWhiteSpace(data.Id))
                        if (s_StatusEffectDataIds.TryGetValue(data.Id, out m_StatusEffectDataReference) && m_StatusEffectDataReference != data)
                            GenerateUntilAddable();
                        else
                            s_StatusEffectDataIds.Add(data.Id, data);
                    else
                        GenerateUntilAddable();

                    void GenerateUntilAddable()
                    {
                        data.GenerateId();
                        if (!s_StatusEffectDataIds.TryAdd(data.Id, data)) 
                            GenerateUntilAddable();
                    }
                }
#endif
            }
        }
    }
}