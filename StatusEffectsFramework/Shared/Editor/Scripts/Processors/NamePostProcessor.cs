using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffectsFramework.Editor
{
    public class NamePostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            Dictionary<Hash128, Name> nameIds;
            Name nameReference;

            nameIds = new();

            if (didDomainReload)
            {
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
                        if (nameIds.TryGetValue(name.Id, out nameReference))
                        {
                            if (!ReferenceEquals(nameReference, name))
                                GenerateUntilAddable();
                        }
                        else
                            nameIds.Add(name.Id, name);
                    else
                        GenerateUntilAddable();

                    void GenerateUntilAddable()
                    {
                        name.GenerateId();
                        if (!nameIds.TryAdd(name.Id, name))
                            GenerateUntilAddable();
                    }
                }
            }
        }
    }
}