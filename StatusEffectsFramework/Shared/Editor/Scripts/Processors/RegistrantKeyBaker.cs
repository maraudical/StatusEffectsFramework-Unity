using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
#if SCRIPTABLE_BUILD_PIPELINE
using UnityEditor.Build.Pipeline;
#endif
using Object = UnityEngine.Object;

namespace StatusEffectsFramework.Editor
{
    /// <summary>
    /// Builds only contain a <see cref="RegistrantKey{T}"/>'s key, so before a build the key of each assigned asset is
    /// baked into it. An <see cref="Effect"/>'s value type is baked from its status name asset the same way. The editor
    /// always reads both from the asset, so this only changes what builds contain.
    /// </summary>
    internal static class RegistrantKeyBaker
    {
        /// <summary>
        /// Bakes the keys in every asset and prefab that references a registrant, and saves the ones that changed.
        /// Scenes are baked as they are built instead, see <see cref="SceneProcessor"/>.
        /// </summary>
        [MenuItem("Tools/Status Effects Framework/Bake Registrant Keys")]
        public static void BakeAssets()
        {
            var registrantPaths = new HashSet<string>(AssetDatabase.FindAssets($"t:{nameof(Registrant)}").Select(AssetDatabase.GUIDToAssetPath));
            int bakedKeys = 0;
            int bakedAssets = 0;

            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.EndsWith(".asset") && !path.EndsWith(".prefab"))
                    continue;

                if (!IsEditable(path))
                    continue;

                // Assigned assets are still editor dependencies even though builds leave them
                // out, so only assets that reference a registrant need to be loaded.
                if (!AssetDatabase.GetDependencies(path, false).Any(registrantPaths.Contains))
                    continue;

                int baked = 0;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (!asset)
                        continue;

                    int bakedInAsset = Bake(asset);
                    if (bakedInAsset > 0)
                        EditorUtility.SetDirty(asset);
                    baked += bakedInAsset;
                }

                if (baked == 0)
                    continue;

                AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(path));
                bakedKeys += baked;
                bakedAssets++;
            }

            if (bakedKeys > 0)
                Debug.Log($"{nameof(RegistrantKeyBaker)}: Baked {bakedKeys} registrant key(s) and value type(s) into {bakedAssets} asset(s).");
        }

        /// <summary>
        /// Bakes the key of the assigned asset into every <see cref="RegistrantKey{T}"/> serialized in
        /// <paramref name="target"/>, and the value type into every <see cref="Effect"/>. Returns how many values changed.
        /// </summary>
        internal static int Bake(Object target)
        {
            using var serializedObject = new SerializedObject(target);
            var property = serializedObject.GetIterator();
            int baked = 0;

            // Only containers are entered, so strings aren't walked character by character.
            while (property.Next(property.propertyType is SerializedPropertyType.Generic or SerializedPropertyType.ManagedReference))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference
                    || property.name != RegistrantKeyDrawer.AssetPropertyName
                    || property.objectReferenceValue is not Registrant registrant)
                    continue;

                // The path of the RegistrantKey field, without the trailing ".m_Asset".
                var assetPath = property.propertyPath;
                var fieldPath = assetPath.Substring(0, assetPath.Length - RegistrantKeyDrawer.AssetPropertyName.Length - 1);

                var keyProperty = serializedObject.FindProperty($"{fieldPath}.{RegistrantKeyDrawer.KeyPropertyName}");
                if (keyProperty != null && keyProperty.propertyType == SerializedPropertyType.String && keyProperty.stringValue != registrant.Key)
                {
                    keyProperty.stringValue = registrant.Key;
                    baked++;
                }

                if (registrant is StatusName statusName
                    && TryFindEffectValueType(serializedObject, fieldPath, out var valueTypeProperty)
                    && valueTypeProperty.enumValueIndex != (int)statusName.ValueType)
                {
                    valueTypeProperty.enumValueIndex = (int)statusName.ValueType;
                    baked++;
                }
            }

            if (baked > 0)
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return baked;
        }

        /// <summary>
        /// Finds the value type of the <see cref="Effect"/> that holds the status name at <paramref name="fieldPath"/>,
        /// if an effect holds it.
        /// </summary>
        private static bool TryFindEffectValueType(SerializedObject serializedObject, string fieldPath, out SerializedProperty valueTypeProperty)
        {
            valueTypeProperty = null;

            int parentEnd = fieldPath.LastIndexOf('.');
            if (parentEnd < 0)
                return false;

            var parent = serializedObject.FindProperty(fieldPath.Substring(0, parentEnd));
            if (parent == null || parent.type != nameof(Effect))
                return false;

            valueTypeProperty = parent.FindPropertyRelative($"m_{nameof(Effect.ValueType)}");
            return valueTypeProperty != null && valueTypeProperty.propertyType == SerializedPropertyType.Enum;
        }

        /// <summary>
        /// Assets in immutable packages can't be saved.
        /// </summary>
        private static bool IsEditable(string path)
        {
            if (path.StartsWith("Assets/"))
                return true;

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
            return package != null && package.source is PackageSource.Embedded or PackageSource.Local;
        }

        internal sealed class BuildPreprocessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => -1;

            public void OnPreprocessBuild(BuildReport report)
            {
                BakeAssets();
            }
        }

        /// <summary>
        /// Bakes each scene as it is built. This changes the build's copy of the scene, not the scene file.
        /// </summary>
        internal sealed class SceneProcessor : IProcessSceneWithReport
        {
            public int callbackOrder => 0;

            public void OnProcessScene(Scene scene, BuildReport report)
            {
                // Scenes are also processed when entering play mode, where keys are read from the assets anyway.
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                foreach (var root in scene.GetRootGameObjects())
                    foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                        if (behaviour)
                            Bake(behaviour);
            }
        }
#if SCRIPTABLE_BUILD_PIPELINE

        /// <summary>
        /// Addressables has no event before it builds, but every Scriptable Build Pipeline build (which Addressables
        /// uses) runs this callback after compiling scripts and before working out what goes into each bundle.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RegisterScriptableBuildPipelineCallback()
        {
            var previousCallback = ContentPipeline.BuildCallbacks.PostScriptsCallbacks;
            ContentPipeline.BuildCallbacks.PostScriptsCallbacks = (parameters, results) =>
            {
                BakeAssets();
                return previousCallback?.Invoke(parameters, results) ?? ReturnCode.Success;
            };
        }
#endif
    }
}
