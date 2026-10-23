using UnityEditor;

namespace StatusEffectsFramework.Editor
{
    /// <summary>
    /// Removes loose <see cref="ModuleInstance"/> sub assets from a <see cref="StatusEffectData"/> as it is saved, so
    /// only assets that are already being saved are changed.
    /// </summary>
    internal class ModuleInstanceSaveProcessor : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            // Runtime code may still reference the instances, so they are cleaned up on the next save outside play mode.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return paths;

            foreach (var path in paths)
            {
                if (!path.EndsWith(".asset") || !typeof(StatusEffectData).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path)))
                    continue;

                var data = AssetDatabase.LoadAssetAtPath<StatusEffectData>(path);
                if (data != null)
                    StatusEffectDataEditor.RemoveLooseModuleInstances(data);
            }

            return paths;
        }
    }
}
