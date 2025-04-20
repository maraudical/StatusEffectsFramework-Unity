using StatusEffects.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(ModuleContainer))]
    internal class ModuleContainerDrawer : PropertyDrawer
    {
        private UnityEngine.Object m_Target;
        private object m_ModuleReference;
        private object m_ModuleInstanceReference;
        private Type m_PreviousModuleInstanceType;
        private Type m_ModuleInstanceType;
        private Attribute m_Attribute;

        private List<UnityEngine.Object> m_ModuleInstances;

        private SerializedObject m_Instance;
        private SerializedProperty m_Iterator;

        private readonly string k_ModuleName = $"m_{nameof(ModuleContainer.Module)}";
        private readonly string k_ModuleInstanceName = $"m_{nameof(ModuleContainer.ModuleInstance)}";

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
                if (m_ModuleInstances == null)
                    m_ModuleInstances = new();
                else
                    m_ModuleInstances.Clear();

                m_PreviousModuleInstanceType = null;
                // Find all relevant module instance references
                bool isDirty = false;
                bool differentModuleTypes = false;
                bool containsNullModule = false;
                int count = property.serializedObject.targetObjects.Length;
                for (int i = 0; i < count; i++)
                {
                    m_Target = property.serializedObject.targetObjects[i];
                    bool requireReimport = false;
                    m_ModuleReference = moduleProperty.GetParent(m_Target).GetValue(k_ModuleName);
                    m_ModuleInstanceReference = moduleInstanceProperty.GetParent(m_Target).GetValue(k_ModuleInstanceName);
                    // If the module reference was removed destroy the instance
                    if (m_ModuleReference == null && m_ModuleInstanceReference != null && !m_ModuleInstanceReference.Equals(null))
                    {
                        ScriptableObject instance = m_ModuleInstanceReference as ScriptableObject;
                        AssetDatabase.RemoveObjectFromAsset(instance);
                        EditorUtility.SetDirty(m_Target);
                        isDirty = true;
                        UnityEngine.Object.DestroyImmediate(instance);
                    }
                    if (m_ModuleReference != null)
                    {
                        m_Attribute = Attribute.GetCustomAttribute(m_ModuleReference.GetType(), typeof(AttachModuleInstanceAttribute));

                        if (m_Attribute != null)
                            m_ModuleInstanceType = ((AttachModuleInstanceAttribute)m_Attribute).Type;

                        // If the module reference was changed we add or destroy the instance, note that
                        // we also check in the StatusEffectDataEditor for when list items are removed.
                        if (m_ModuleInstanceReference != null && !m_ModuleInstanceReference.Equals(null) && (m_Attribute == null || m_ModuleInstanceType != m_ModuleInstanceReference.GetType()))
                        {
                            ScriptableObject instance = m_ModuleInstanceReference as ScriptableObject;
                            AssetDatabase.RemoveObjectFromAsset(instance);
                            EditorUtility.SetDirty(m_Target);
                            isDirty = true;
                            requireReimport = true;
                            UnityEngine.Object.DestroyImmediate(instance);
                        }

                        if (m_Attribute == null)
                            goto SaveAsset;

                        if (m_ModuleInstanceReference == null || m_ModuleInstanceReference.Equals(null))
                        {
                            ScriptableObject instance = ScriptableObject.CreateInstance(m_ModuleInstanceType);
                            AssetDatabase.AddObjectToAsset(instance, m_Target as ScriptableObject);
                            moduleInstanceProperty.GetParent(m_Target).SetValue(k_ModuleInstanceName, instance);
                            m_ModuleInstanceReference = instance;
                            EditorUtility.SetDirty(m_Target);
                            isDirty = true;
                        }
                        // Check if there are different module instance types
                        if (i > 0 && m_PreviousModuleInstanceType != m_ModuleInstanceType)
                            differentModuleTypes = true;

                        m_PreviousModuleInstanceType = m_ModuleInstanceType;
                        m_ModuleInstances.Add(m_ModuleInstanceReference as ModuleInstance);
                    }
                    else
                        containsNullModule = true;

                    SaveAsset:

                    AssetDatabase.SaveAssetIfDirty(m_Target);

                    if (requireReimport)
                        // Unity bug with removing the final sub asset by changing the module to
                        // one without a module instance.
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(m_Target), ImportAssetOptions.ImportRecursive);
                }

                if (isDirty)
                {
                    property.serializedObject.ApplyModifiedProperties();
                    property.serializedObject.Update();
                }

                foreach (var child in instanceRoot.Children().Reverse())
                    instanceRoot.Remove(child);

                if (!differentModuleTypes && !containsNullModule && m_ModuleInstances.Count > 0)
                {
                    // Iterate through the module instance to display properties even if they are derived.
                    m_Instance = new SerializedObject(m_ModuleInstances.ToArray());

                    m_Iterator = m_Instance.GetIterator();
                    // Skip the script property
                    m_Iterator.NextVisible(true);
                    
                    if (m_Iterator.NextVisible(true))
                    {
                        do
                        {
                            var propertyField = new PropertyField() { name = "property-field: " + m_Iterator.propertyPath };
                            propertyField.BindProperty(m_Iterator.Copy());

                            instanceRoot.Add(propertyField);
                        }
                        while (m_Iterator.NextVisible(false));
                    }
                }
            }
        }
    }
}