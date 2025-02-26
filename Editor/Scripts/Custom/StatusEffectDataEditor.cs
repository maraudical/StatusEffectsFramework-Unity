using StatusEffects.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectData))]
    [CanEditMultipleObjects]
    public class StatusEffectDataEditor : Editor
    {
        private SerializedProperty m_Id;
        private SerializedProperty m_AutomaticallyAddToDatabase;

        private SerializedProperty m_Group;
        private SerializedProperty m_ComparableName;
        private SerializedProperty m_BaseValue;
        private SerializedProperty m_Icon;
        private SerializedProperty m_Color;
        private SerializedProperty m_StatusEffectName;
        private SerializedProperty m_Acronym;
        private SerializedProperty m_Description;
        private SerializedProperty m_AllowEffectStacking;
        private SerializedProperty m_NonStackingBehaviour;
        private SerializedProperty m_MaxStacks;
        private SerializedProperty m_Effects;
        private SerializedProperty m_Conditions;
        private SerializedProperty m_Modules;

        private SerializedProperty m_EnableIcon;
        private SerializedProperty m_EnableColor;
        private SerializedProperty m_EnableName;
        private SerializedProperty m_EnableAcronym;
        private SerializedProperty m_EnableDescription;

        private ReorderableList m_ModulesList;

        private StatusEffectDatabase m_Database;
        private StatusEffectData m_Data;
        private Condition m_Condition; 

        private bool m_DisplayConditionsWarning;

        private void OnEnable()
        {
            // Retrieve properties
            m_Id = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Id)}");
            m_AutomaticallyAddToDatabase = serializedObject.FindProperty($"m_{nameof(StatusEffectData.AutomaticallyAddToDatabase)}");

            m_Group = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Group)}");
            m_ComparableName = serializedObject.FindProperty($"m_{nameof(StatusEffectData.ComparableName)}");
            m_BaseValue = serializedObject.FindProperty($"m_{nameof(StatusEffectData.BaseValue)}");

            m_Icon = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Icon)}");
            m_Color = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Color)}");
            m_StatusEffectName = serializedObject.FindProperty($"m_{nameof(StatusEffectData.StatusEffectName)}");
            m_Acronym = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Acronym)}");
            m_Description = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Description)}");

            m_AllowEffectStacking = serializedObject.FindProperty($"m_{nameof(StatusEffectData.AllowEffectStacking)}");
            m_NonStackingBehaviour = serializedObject.FindProperty($"m_{nameof(StatusEffectData.NonStackingBehaviour)}");
            m_MaxStacks = serializedObject.FindProperty($"m_{nameof(StatusEffectData.MaxStacks)}");

            m_Effects = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Effects)}");
            m_Conditions = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Conditions)}");

            m_Modules = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Modules)}");

            m_EnableIcon = serializedObject.FindProperty("m_EnableIcon");
            m_EnableColor = serializedObject.FindProperty("m_EnableColor");
            m_EnableName = serializedObject.FindProperty("m_EnableName");
            m_EnableAcronym = serializedObject.FindProperty("m_EnableAcronym");
            m_EnableDescription = serializedObject.FindProperty("m_EnableDescription");

            // Check if it is added to the database.
            m_Database = StatusEffectDatabase.Get();
            UnityEngine.Object target;
            string path;
            IEnumerable<ModuleInstance> modules;
            UnityEngine.Object[] subAssets;
            UnityEngine.Object nestedModule;

            int objectCount = serializedObject.targetObjects.Length;
            // Remove any loose nested scriptable objects and
            // iterate in reverse, to match the selected object order.
            // Also check that the StatusEffect is added to the Database.
            for (int i = objectCount - 1; i >= 0; i--)
            {
                // Get the property on the corresponding serializedObject.
                target = serializedObject.targetObjects[i];
                path = AssetDatabase.GetAssetPath(target);

                StatusEffectData data = target as StatusEffectData;

                if (!EditorApplication.isPlaying && !m_Database.ContainsKey(data.Id))
                {
                    m_Database.Add(data.Id, data);
                    EditorUtility.SetDirty(m_Database);
                }

                modules = data.Modules.Select(m => m.ModuleInstance);
                subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

                for (int v = subAssets.Length - 1; v >= 0; v--)
                {
                    nestedModule = subAssets[v];
                    // If there is somehow a loose module we need to clean it up.
                    if (!modules.Contains(nestedModule))
                    {
                        AssetDatabase.RemoveObjectFromAsset(nestedModule);
                        DestroyImmediate(nestedModule);
                        EditorUtility.SetDirty(target);
                    }
                }

                AssetDatabase.SaveAssetIfDirty(target);
            }
            AssetDatabase.SaveAssetIfDirty(m_Database);
            // Setup the reorderable module list
            m_ModulesList = new ReorderableList(serializedObject, m_Modules, true, true, true, true);

            m_ModulesList.drawHeaderCallback = rect => 
            {
                EditorGUI.LabelField(rect, m_Modules.displayName);
            };
            m_ModulesList.onAddCallback = list =>
            {
                var index = list.serializedProperty.arraySize;
                // Special case where multi editing two objects with different
                // sizes and we need to clamp list sizes to the lowest
                foreach (var target in list.serializedProperty.serializedObject.targetObjects)
                {
                    // Get the module instance and any others beyond the current index
                    bool requireReimport = false;
                    m_Data = target as StatusEffectData;
                    for (int i = index; i < m_Data.Modules.Count; i++)
                    {
                        ScriptableObject moduleInstance = m_Data.Modules.ElementAt(index)?.ModuleInstance;
                        // Remove the scriptable object from nested assets
                        if (moduleInstance != null)
                        {
                            AssetDatabase.RemoveObjectFromAsset(moduleInstance);
                            DestroyImmediate(moduleInstance);
                            EditorUtility.SetDirty(target);
                            requireReimport = true;
                        }
                    }
                    AssetDatabase.SaveAssetIfDirty(target);

                    if (requireReimport)
                        // Unity bug with removing the final sub asset by changing the module to
                        // one without a module instance.
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target), ImportAssetOptions.ImportRecursive);
                }
                // Add one element
                list.serializedProperty.InsertArrayElementAtIndex(index);
                // Get that element
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                // Reset all properties
                element.FindPropertyRelative(nameof(ModuleContainer.Module)).objectReferenceValue = null;
                element.FindPropertyRelative(nameof(ModuleContainer.ModuleInstance)).objectReferenceValue = null;
            };
            m_ModulesList.onRemoveCallback = list =>
            {
                foreach (var target in list.serializedProperty.serializedObject.targetObjects)
                {
                    // Remove selected or default to the last element
                    if (list.selectedIndices.Count > 0)
                        foreach (int index in list.selectedIndices.OrderByDescending(x => x))
                            DestroyObjectAt(index);
                    else
                        DestroyObjectAt(list.serializedProperty.minArraySize - 1);

                    EditorUtility.SetDirty(target);
                    AssetDatabase.SaveAssetIfDirty(target);

                    void DestroyObjectAt(int index)
                    {
                        // Get the module instance
                        ScriptableObject moduleInstance = (target as StatusEffectData).Modules.ElementAt(index)?.ModuleInstance;
                        // Remove the scriptable object from nested assets
                        if (moduleInstance != null)
                        {
                            AssetDatabase.RemoveObjectFromAsset(moduleInstance);
                            DestroyImmediate(moduleInstance);
                        }
                    }
                }
                // Remove selected or default to the last element
                if (list.selectedIndices.Count > 0)
                    foreach (int index in list.selectedIndices.OrderByDescending(x => x))
                        list.serializedProperty.DeleteArrayElementAtIndex(index);
                else
                    list.serializedProperty.DeleteArrayElementAtIndex(list.serializedProperty.minArraySize - 1);
            };
            m_ModulesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_ModulesList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += EditorGUIUtility.standardVerticalSpacing / 1.515f;
                rect.height = EditorGUI.GetPropertyHeight(element);
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };
            m_ModulesList.elementHeightCallback = index => 
            {
                var element = m_ModulesList.serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element);
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_Data = target as StatusEffectData;
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(m_Id);
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            EditorGUILayout.PropertyField(m_AutomaticallyAddToDatabase);
            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var target in targets)
                {
                    StatusEffectData data = target as StatusEffectData;
                    if (m_AutomaticallyAddToDatabase.boolValue)
                    {
                        if (!m_Database.Values.ContainsKey(data.Id))
                        {
                            m_Database.Values.Add(data.Id, data);
                            EditorUtility.SetDirty(m_Database);
                        }
                    }
                    else
                    {
                        if (m_Database.Values.ContainsKey(data.Id))
                        {
                            m_Database.Values.Remove(data.Id);
                            EditorUtility.SetDirty(m_Database);
                        }
                    }
                }
                AssetDatabase.SaveAssetIfDirty(m_Database);
            }

            EditorGUILayout.BeginVertical("groupbox");
            EditorGUILayout.LabelField("Required Fields", EditorStyles.boldLabel);
            HorizontalLine();
            EditorGUILayout.PropertyField(m_Group);
            EditorGUILayout.PropertyField(m_ComparableName);
            if (m_BaseValue.floatValue == 0)
                EditorGUILayout.HelpBox("Base value cannot be 0!", MessageType.Warning);
            EditorGUILayout.PropertyField(m_BaseValue);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical("groupbox");
            EditorGUILayout.LabelField("Optional Fields", EditorStyles.boldLabel);
            HorizontalLine();
            EditorGUILayout.BeginHorizontal();
            m_EnableIcon.boolValue = EditorGUILayout.Toggle(m_EnableIcon.boolValue, GUILayout.MaxWidth(24));
            if (m_EnableIcon.boolValue)
                EditorGUILayout.PropertyField(m_Icon);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            m_EnableColor.boolValue = EditorGUILayout.Toggle(m_EnableColor.boolValue, GUILayout.MaxWidth(24));
            if (m_EnableColor.boolValue)
                EditorGUILayout.PropertyField(m_Color);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            m_EnableName.boolValue = EditorGUILayout.Toggle(m_EnableName.boolValue, GUILayout.MaxWidth(24));
            if (m_EnableName.boolValue)
                EditorGUILayout.PropertyField(m_StatusEffectName, new GUIContent("Name"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            m_EnableAcronym.boolValue = EditorGUILayout.Toggle(m_EnableAcronym.boolValue, GUILayout.MaxWidth(24));
            if (m_EnableAcronym.boolValue)
                EditorGUILayout.PropertyField(m_Acronym);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            m_EnableDescription.boolValue = EditorGUILayout.Toggle(m_EnableDescription.boolValue, GUILayout.MaxWidth(24));
            if (m_EnableDescription.boolValue)
                EditorGUILayout.PropertyField(m_Description);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("groupbox");
            EditorGUILayout.LabelField("Stacking", EditorStyles.boldLabel);
            HorizontalLine();
            EditorGUILayout.PropertyField(m_AllowEffectStacking);
            if (!m_AllowEffectStacking.boolValue)
                EditorGUILayout.PropertyField(m_NonStackingBehaviour);
            if (m_AllowEffectStacking.boolValue)
                EditorGUILayout.PropertyField(m_MaxStacks);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("groupbox");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_Effects);
			EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
			
			EditorGUILayout.BeginVertical("groupbox");
            EditorGUI.indentLevel++;

            m_DisplayConditionsWarning = false;

            for (int i = 0; i < m_Conditions.arraySize; i++)
            {
                m_Condition = m_Data.Conditions.ElementAtOrDefault(i);

                if (m_Condition == null || !m_Condition.Add || m_Condition.ActionData != target)
                    continue;

                m_DisplayConditionsWarning = true;
                break;
            }

            if (m_DisplayConditionsWarning)
                EditorGUILayout.HelpBox("Do not recursively add status datas! " +
                                        "Avoid adding a status data to itself! " +
                                        "Make sure there aren't two that add each other!", MessageType.Warning);

            EditorGUILayout.PropertyField(m_Conditions);

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(7.5f);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            m_ModulesList?.DoLayoutList();
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void HorizontalLine()
        {
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f), new Color(.5f, .5f, .5f, 0.3f));

            GUILayout.Space(4f);
        }
    }
}
