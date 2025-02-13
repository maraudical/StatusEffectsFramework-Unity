using StatusEffects.Modules;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(ModuleContainer))]
    public class ModuleContainerDrawer : PropertyDrawer
    {
        private SerializedProperty m_Module;
        private SerializedProperty m_ModuleInstance;

        private SerializedObject m_Instance;
        private SerializedProperty m_Iterator;

        private float m_DerivedPropertyHeight;
        private float m_DerivedFieldCount;

        private UnityEngine.Object m_Target;
        private object m_ModuleReference;
        private object m_ModuleInstanceReference;
        private Type m_PreviousModuleInstanceType;
        private Type m_ModuleInstanceType;
        private Attribute m_Attribute;

        private List<UnityEngine.Object> m_ModuleInstances;

        private readonly float m_FieldSize = EditorGUIUtility.singleLineHeight;
        private readonly float m_Padding = EditorGUIUtility.standardVerticalSpacing;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.targetObject is not ScriptableObject)
            {
                Color color = GUI.color;
                GUI.color = Color.yellow;
                EditorGUI.LabelField(position, "The containing class must be a Scriptable Object!");
                GUI.color = color;
                return;
            }

            m_Module = property.FindPropertyRelative(nameof(ModuleContainer.Module));
            m_ModuleInstance = property.FindPropertyRelative(nameof(ModuleContainer.ModuleInstance));
            
            EditorGUI.BeginProperty(position, label, property);

            position.height = m_FieldSize;
            position.y += m_Padding;
            // Draw base module reference
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, m_Module, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            position.y += m_FieldSize + m_Padding;
            
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
                m_ModuleReference = m_Module.GetParent(m_Target).GetValue(nameof(ModuleContainer.Module));
                m_ModuleInstanceReference = m_ModuleInstance.GetParent(m_Target).GetValue(nameof(ModuleContainer.ModuleInstance));
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
                        m_ModuleInstance.GetParent(m_Target).SetValue(nameof(ModuleContainer.ModuleInstance), instance);
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

            if (!differentModuleTypes && !containsNullModule && m_ModuleInstances.Count > 0)
            {
                // Iterate through the module instance to display properties even if they are derived.
                m_Instance = new SerializedObject(m_ModuleInstances.ToArray());

                m_Iterator = m_Instance.GetIterator();
                // Skip the script property
                m_Iterator.NextVisible(true);

                while (m_Iterator.NextVisible(false))
                {
                    EditorGUI.PropertyField(position, m_Iterator);
                    position.y += EditorGUI.GetPropertyHeight(m_Iterator) + m_Padding;
                }

                m_Instance.ApplyModifiedProperties();
                m_Instance.Dispose();
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            m_Module = property.FindPropertyRelative(nameof(ModuleContainer.Module));
            m_ModuleInstance = property.FindPropertyRelative(nameof(ModuleContainer.ModuleInstance));

            if (m_Module.objectReferenceValue == null || m_ModuleInstance.objectReferenceValue == null)
                return DrawDefault();

            m_Attribute = Attribute.GetCustomAttribute(m_Module.objectReferenceValue.GetType(), typeof(AttachModuleInstanceAttribute));

            if (m_Attribute == null)
                return DrawDefault();

            m_ModuleInstanceType = ((AttachModuleInstanceAttribute)m_Attribute).Type;

            foreach (var target in property.serializedObject.targetObjects)
            {
                m_ModuleInstanceReference = m_ModuleInstance.GetParent(target).GetValue(nameof(ModuleContainer.ModuleInstance));

                if (m_ModuleInstanceType != m_ModuleInstanceReference?.GetType())
                    return DrawDefault();

                m_ModuleInstanceType = m_ModuleInstanceReference.GetType();
            }

            m_DerivedFieldCount = 0; 
            m_DerivedPropertyHeight = 0;
            // Iterate through the module instance to display properties even if they are derived.
            m_Instance = new SerializedObject(m_ModuleInstance.objectReferenceValue);

            m_Iterator = m_Instance.GetIterator();
            // Skip the script property
            m_Iterator.NextVisible(true);

            while (m_Iterator.NextVisible(false))
            {
                m_DerivedFieldCount++;
                m_DerivedPropertyHeight += EditorGUI.GetPropertyHeight(m_Iterator);
            }
            
            m_Instance.Dispose();
            
            return m_FieldSize + m_Padding * (m_DerivedFieldCount + 2) + m_DerivedPropertyHeight;

            float DrawDefault()
            {
                return m_FieldSize + m_Padding * 2;
            }
        }
    }
}