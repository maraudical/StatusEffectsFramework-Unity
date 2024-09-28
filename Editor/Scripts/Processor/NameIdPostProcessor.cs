using UnityEditor;

namespace StatusEffects.Inspector
{
    public class NameIdPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            int importedCount = importedAssets.Length;
            for (int i = 0; i < importedCount; i++)
            {
                string assetPath = importedAssets[i];
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

                if (asset != null && asset is Name name && string.IsNullOrWhiteSpace(name.Id))
                    name.GenerateId();
            }
        }
    }
}