using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffectsFramework.Editor
{
    public class StatusEffectDataPostProcessor : AssetPostprocessor
    {
        /*static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            var database = StatusEffectDatabase.Get();
            StatusEffectData statusEffectData;
            
            for (int i = database.HiddenValues.Count - 1; i >= 0; i--)
            {
                var kvp = database.HiddenValues.ElementAt(i);
                if (kvp.Value == null || kvp.Key != kvp.Value.Id)
                {
                    database.HiddenValues.Remove(kvp.Key);
                    EditorUtility.SetDirty(database);
                } 
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
            
            AssetDatabase.SaveAssetIfDirty(database);

            void ValidateAsset(string assetPath)
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                if (asset == null)
                    return;

                if (asset is not StatusEffectData data)
                    return;
                
                if (data.Id != default)
                    if (database.TryGetValue(data.Id, out statusEffectData))
                    {
                        if (statusEffectData != data)
                            GenerateUntilAddable();
                    }
                    else
                    {
                        database.Add(data.Id, data);
                        EditorUtility.SetDirty(database);
                    }
                else
                    GenerateUntilAddable();

                void GenerateUntilAddable()
                {
                    data.GenerateId();
                    if (!database.TryAdd(data.Id, data))
                    {
                        GenerateUntilAddable();
                        return;
                    }
                    EditorUtility.SetDirty(database);
                }
            }
        }*/
    }
}