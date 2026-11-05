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

                // Assigned assets are still editor dependencies even though builds leave them out, so only assets that
                // reference a registrant need to be loaded. A prefab made from another prefab can reference one only through
                // its source, such as once it reverts its asset override, so prefabs are checked through their sources too.
                if (!AssetDatabase.GetDependencies(path, path.EndsWith(".prefab")).Any(registrantPaths.Contains))
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

            // In a prefab asset made from another prefab, the baked values are overridden exactly when the assigned asset is.
            // One that overrides the asset but inherits the baked values would pick up whatever its source prefab is baked
            // to later, so those values are written as overrides even when they already match. One that inherits the asset
            // inherits the values its source prefab is baked to, so overrides left from reverting the asset are reverted.
            bool isPrefabInstanceAsset = EditorUtility.IsPersistent(target) && PrefabUtility.IsPartOfPrefabInstance(target);
            // A source prefab in an immutable package isn't baked, so prefabs made from it bake their own values.
            bool sourceIsBaked = isPrefabInstanceAsset && IsEditable(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(target)));
            var inheritedPaths = new List<string>();
            var leftoverPaths = new List<string>();

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

                bool overridesAsset = isPrefabInstanceAsset && property.prefabOverride;
                bool inheritsAsset = sourceIsBaked && !property.prefabOverride;

                var keyProperty = serializedObject.FindProperty($"{fieldPath}.{RegistrantKeyDrawer.KeyPropertyName}");
                if (keyProperty != null && keyProperty.propertyType == SerializedPropertyType.String)
                {
                    if (inheritsAsset)
                    {
                        if (keyProperty.prefabOverride)
                            leftoverPaths.Add(keyProperty.propertyPath);
                    }
                    else if (keyProperty.stringValue != registrant.Key)
                    {
                        keyProperty.stringValue = registrant.Key;
                        baked++;
                    }
                    else if (overridesAsset && !keyProperty.prefabOverride)
                    {
                        inheritedPaths.Add(keyProperty.propertyPath);
                    }
                }

                if (registrant is StatusName statusName
                    && TryFindEffectValueType(serializedObject, fieldPath, out var valueTypeProperty))
                {
                    if (inheritsAsset)
                    {
                        if (valueTypeProperty.prefabOverride)
                            leftoverPaths.Add(valueTypeProperty.propertyPath);
                    }
                    else if (valueTypeProperty.enumValueIndex != (int)statusName.ValueType)
                    {
                        valueTypeProperty.enumValueIndex = (int)statusName.ValueType;
                        baked++;
                    }
                    else if (overridesAsset && !valueTypeProperty.prefabOverride)
                    {
                        inheritedPaths.Add(valueTypeProperty.propertyPath);
                    }
                }
            }

            if (baked > 0)
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (leftoverPaths.Count > 0)
            {
                foreach (var path in leftoverPaths)
                    PrefabUtility.RevertPropertyOverride(serializedObject.FindProperty(path), InteractionMode.AutomatedAction);
                serializedObject.Update();
                baked += leftoverPaths.Count;
            }

            if (inheritedPaths.Count > 0)
            {
                OverrideInheritedValues(target, serializedObject, inheritedPaths);
                baked += inheritedPaths.Count;
            }

            return baked;
        }

        /// <summary>
        /// Adds a prefab override for each property at <paramref name="paths"/>, keeping its current value. Setting a
        /// property to the value it already has doesn't record an override, so the modifications are added directly.
        /// </summary>
        private static void OverrideInheritedValues(Object target, SerializedObject serializedObject, List<string> paths)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(target);
            var modifications = new List<PropertyModification>(PrefabUtility.GetPropertyModifications(target) ?? new PropertyModification[0]);

            foreach (var path in paths)
            {
                var property = serializedObject.FindProperty(path);
                modifications.Add(new PropertyModification
                {
                    target = source,
                    propertyPath = path,
                    // Enums are stored as their underlying value, not their index.
                    value = property.propertyType == SerializedPropertyType.String ? property.stringValue : property.intValue.ToString(),
                });
            }

            PrefabUtility.SetPropertyModifications(target, modifications.ToArray());
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
                // The build has already collected its content, so manifests are refreshed but not moved.
                StatusRegistryManifestGenerator.GenerateAll(automaticOnly: true, allowGroupChanges: false);
                BakeAssets();
                return previousCallback?.Invoke(parameters, results) ?? ReturnCode.Success;
            };
        }
#endif
    }
}
