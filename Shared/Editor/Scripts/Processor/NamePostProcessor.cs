using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    public class NamePostProcessor : AssetPostprocessor
    {
        private static Dictionary<Hash128, Name> s_NameIds;
        private static Name m_NameReference;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (s_NameIds != null)
                for (int i = s_NameIds.Count - 1; i >= 0; i--)
                {
                    var kvp = s_NameIds.ElementAt(i);
                    if (kvp.Value == null || kvp.Key != kvp.Value.Id)
                        s_NameIds.Remove(kvp.Key);
                }

            if (didDomainReload)
            {
                s_NameIds = new();
                var guids = AssetDatabase.FindAssets($"t:{nameof(Name)}");
                var assetPaths = guids.Select((guid) => AssetDatabase.GUIDToAssetPath(guid));

                foreach (string assetPath in assetPaths)
                    ValidateAsset(assetPath);
            }
            else
            {
                foreach (string assetPath in importedAssets)
                    ValidateAsset(assetPath);
            }
            
            void ValidateAsset(string assetPath)
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                
                if (asset == null)
                    return;

                if (asset is Name name)
                {
                    if (name.Id != default)
                        if (s_NameIds.TryGetValue(name.Id, out m_NameReference))
                        {
                            if (!ReferenceEquals(m_NameReference, name))
                                GenerateUntilAddable();
                        }
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