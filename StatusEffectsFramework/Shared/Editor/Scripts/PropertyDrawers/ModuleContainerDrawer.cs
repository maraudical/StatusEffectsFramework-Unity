using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StatusEffectsFramework.Editor
{
    [CustomPropertyDrawer(typeof(ModuleContainer))]
    internal class ModuleContainerDrawer : PropertyDrawer
    {
        private static readonly string k_ModuleName = $"m_{nameof(ModuleContainer.Module)}";
        private static readonly string k_ModuleInstanceName = $"m_{nameof(ModuleContainer.ModuleInstance)}";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var moduleProperty = property.FindPropertyRelative(k_ModuleName);
            var moduleInstanceProperty = property.FindPropertyRelative(k_ModuleInstanceName);

            var root = new VisualElement();
            var instanceRoot = new VisualElement();
            var module = new PropertyField(moduleProperty, string.Empty);

            root.Add(module);
            root.Add(instanceRoot);

            module.RegisterValueChangeCallback(ModuleChanged);

            return root;

            void ModuleChanged(SerializedPropertyChangeEvent callback)
            {
                var moduleInstances = new List<UnityEngine.Object>();
                Type previousModuleInstanceType = null;
                // Find all relevant module instance references
                bool isDirty = false;
                bool differentModuleTypes = false;
                bool containsNullModule = false;
                var targetObjects = property.serializedObject.targetObjects;
                for (int i = 0; i < targetObjects.Length; i++)
                {
                    var target = targetObjects[i];
                    // Assets are only touched and saved when something had to be fixed.
                    bool changed = false;
                    bool requireReimport = false;
                    object moduleReference = moduleProperty.GetParent(target).GetValue(k_ModuleName);
                    object moduleInstanceReference = moduleInstanceProperty.GetParent(target).GetValue(k_ModuleInstanceName);
                    // If the module reference was removed destroy the instance
                    if (moduleReference == null && moduleInstanceReference != null && !moduleInstanceReference.Equals(null))
                    {
                        ScriptableObject instance = moduleInstanceReference as ScriptableObject;
                        AssetDatabase.RemoveObjectFromAsset(instance);
                        EditorUtility.SetDirty(target);
                        changed = true;
                        UnityEngine.Object.DestroyImmediate(instance);
                    }
                    if (moduleReference != null)
                    {
                        var attribute = (AttachModuleInstanceAttribute)Attribute.GetCustomAttribute(moduleReference.GetType(), typeof(AttachModuleInstanceAttribute));
                        Type moduleInstanceType = attribute?.Type;

                        // If the module reference was changed we add or destroy the instance, note that
                        // we also check in the StatusEffectDataEditor for when list items are removed.
                        if (moduleInstanceReference != null && !moduleInstanceReference.Equals(null) && (attribute == null || moduleInstanceType != moduleInstanceReference.GetType()))
                        {
                            ScriptableObject instance = moduleInstanceReference as ScriptableObject;
                            AssetDatabase.RemoveObjectFromAsset(instance);
                            EditorUtility.SetDirty(target);
                            changed = true;
                            requireReimport = true;
                            UnityEngine.Object.DestroyImmediate(instance);
                        }

                        if (attribute != null)
                        {
                            if (moduleInstanceReference == null || moduleInstanceReference.Equals(null))
                            {
                                ScriptableObject instance = ScriptableObject.CreateInstance(moduleInstanceType);
                                AssetDatabase.AddObjectToAsset(instance, target as ScriptableObject);
                                moduleInstanceProperty.GetParent(target).SetValue(k_ModuleInstanceName, instance);
                                moduleInstanceReference = instance;
                                EditorUtility.SetDirty(target);
                                changed = true;
                            }

                            // Check if there are different module instance types
                            if (i > 0 && previousModuleInstanceType != moduleInstanceType)
                                differentModuleTypes = true;

                            previousModuleInstanceType = moduleInstanceType;
                            moduleInstances.Add(moduleInstanceReference as ModuleInstance);
                        }
                    }
                    else
                        containsNullModule = true;

                    if (changed)
                    {
                        isDirty = true;
                        AssetDatabase.SaveAssetIfDirty(target);
                    }

                    if (requireReimport)
                        // Unity bug with removing the final sub asset by changing the module to
                        // one without a module instance.
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target), ImportAssetOptions.ImportRecursive);
                }

                if (isDirty)
                {
                    property.serializedObject.ApplyModifiedProperties();
                    property.serializedObject.Update();
                }

                foreach (var child in instanceRoot.Children().Reverse())
                    instanceRoot.Remove(child);

                if (!differentModuleTypes && !containsNullModule && moduleInstances.Count > 0)
                {
                    // Iterate through the module instance to display properties even if they are derived.
                    var moduleInstancesObject = new SerializedObject(moduleInstances.ToArray());

                    var iterator = moduleInstancesObject.GetIterator();
                    // Skip the script property
                    iterator.NextVisible(true);

                    if (iterator.NextVisible(true))
                    {
                        do
                        {
                            var propertyField = new PropertyField() { name = "property-field: " + iterator.propertyPath };
                            propertyField.BindProperty(iterator.Copy());

                            instanceRoot.Add(propertyField);
                        }
                        while (iterator.NextVisible(false));
                    }
                }
            }
        }
    }
}