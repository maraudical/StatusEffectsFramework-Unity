#if UNITY_2023_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#else
using UnityEditorInternal;
#endif
using StatusEffects.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectData))]
    [CanEditMultipleObjects]
    internal class StatusEffectDataEditor : Editor
    {
        private StatusEffectDatabase m_Database;
        private StatusEffectData m_Data;
        private Condition m_Condition;

#if UNITY_2023_1_OR_NEWER
        public VisualTreeAsset VisualTree;

        public override VisualElement CreateInspectorGUI()
        {
            m_Database = StatusEffectDatabase.Get();
            
            // Remove any loose nested scriptable objects and
            // iterate in reverse, to match the selected object order.
            // Also check that the StatusEffect is added to the Database.
            foreach (var target in targets)
            {
                // Get the property on the corresponding serializedObject.
                var path = AssetDatabase.GetAssetPath(target);

                StatusEffectData data = target as StatusEffectData;

                if (!EditorApplication.isPlaying && !m_Database.ContainsKey(data.Id))
                {
                    m_Database.Add(data.Id, data);
                    EditorUtility.SetDirty(m_Database);
                }

                var modules = data.Modules.Select(m => m.ModuleInstance);
                var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

                for (int v = subAssets.Length - 1; v >= 0; v--)
                {
                    var nestedModule = subAssets[v];
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

            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var automaticallyAddToDatabase = root.Q<PropertyField>("automatically-add-to-database");
            var baseValueError = new HelpBox() { text = "Base value cannot be 0!", messageType = HelpBoxMessageType.Error };
            var baseValue = root.Q<PropertyField>("base-value");
            var optionalFieldToggles = root.Q<ToggleButtonGroup>("optional-field-toggles");
            List<PropertyField> optionalFields = new()
            {
                root.Q<PropertyField>("icon"),
                root.Q<PropertyField>("color"),
                root.Q<PropertyField>("name"),
                root.Q<PropertyField>("acronym"),
                root.Q<PropertyField>("description")
            };
            var allowEffectStacking = root.Q<PropertyField>("allow-effect-stacking");
            var nonStackingBehaviour = root.Q<PropertyField>("non-stacking-behaviour");
            var maxStacks = root.Q<PropertyField>("max-stacks");
            var ConditionsWarning = new HelpBox() { text = "Do not recursively add status datas! " +
                                                           "Avoid adding a status data to itself! " +
                                                           "Make sure there aren't two that add each other!", 
                                                    messageType = HelpBoxMessageType.Warning };
            var conditionsList = root.Q<ListView>("conditions-list");
            var modulesList = root.Q<ListView>("modules-list");

            var baseValueProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.BaseValue)}");
            List<SerializedProperty> optionalProperties = new()
            {
                serializedObject.FindProperty("m_EnableIcon"),
                serializedObject.FindProperty("m_EnableColor"),
                serializedObject.FindProperty("m_EnableName"),
                serializedObject.FindProperty("m_EnableAcronym"),
                serializedObject.FindProperty("m_EnableDescription"),
            };
            var allowEffectStackingProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.AllowEffectStacking)}");
            var conditionsProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Conditions)}");
            var modulesProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Modules)}");

            automaticallyAddToDatabase.RegisterCallback<ChangeEvent<bool>>(AutomaticallyAddToDatabaseChanged);
            
            root.Q("base-value-error").Add(baseValueError);

            BaseValueChanged(default);
            baseValue.RegisterValueChangeCallback(BaseValueChanged);

            int bitMask = 0;
            for (int i = 0; i < optionalProperties.Count; i++)
            {
                bool value = optionalProperties[i].boolValue;
                bitMask |= value ? 1 << i : 0;
                optionalFields[i].style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
            optionalFieldToggles.SetValueWithoutNotify(new ToggleButtonGroupState(Convert.ToUInt64(bitMask), optionalProperties.Count));
            optionalFieldToggles.RegisterValueChangedCallback(OptionalFieldTogglesChanged);

            AllowEffectStackingChanged(default);
            allowEffectStacking.RegisterValueChangeCallback(AllowEffectStackingChanged);

            root.Q("conditions-warning").Add(ConditionsWarning);

            ConditionChanged(default);
            conditionsList.makeItem = () =>
            {
                return new PropertyField();
            };
            conditionsList.bindItem = (existingElement, index) =>
            {
                var propertyField = existingElement as PropertyField;
                propertyField.BindProperty(conditionsProperty.FindPropertyRelative($"Array.data[{index}]"));
                propertyField.RegisterValueChangeCallback(ConditionChanged);
            };

            bool isPlaying = EditorApplication.isPlaying;
            modulesList.SetEnabled(!isPlaying);
            modulesList.showAddRemoveFooter = !isPlaying;
            modulesList.onAdd += ModulesAdded;
            modulesList.itemsRemoved += ModulesRemoved;

            return root;

            void AutomaticallyAddToDatabaseChanged(ChangeEvent<bool> changeEvent)
            {
                foreach (var target in targets)
                {
                    m_Data = target as StatusEffectData;
                    if (changeEvent.newValue)
                    {
                        if (!m_Database.Values.ContainsKey(m_Data.Id))
                        {
                            m_Database.Values.Add(m_Data.Id, m_Data);
                            EditorUtility.SetDirty(m_Database);
                        }
                    }
                    else
                    {
                        if (m_Database.Values.ContainsKey(m_Data.Id))
                        {
                            m_Database.Values.Remove(m_Data.Id);
                            EditorUtility.SetDirty(m_Database);
                        }
                    }
                }
                AssetDatabase.SaveAssetIfDirty(m_Database);
            }

            void BaseValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                baseValueError.style.display = baseValueProperty.floatValue == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void OptionalFieldTogglesChanged(ChangeEvent<ToggleButtonGroupState> changeEvent)
            {
                for (int i = 0; i < changeEvent.newValue.length; i++)
                {
                    bool value = changeEvent.newValue[i];
                    optionalFields[i].style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                    optionalProperties[i].boolValue = value;
                }

                serializedObject.ApplyModifiedProperties();
            }
            
            void AllowEffectStackingChanged(SerializedPropertyChangeEvent changeEvent)
            {
                bool multipleValues = allowEffectStackingProperty.hasMultipleDifferentValues;
                nonStackingBehaviour.style.display = !allowEffectStackingProperty.boolValue && !multipleValues ? DisplayStyle.Flex : DisplayStyle.None;
                maxStacks.style.display = allowEffectStackingProperty.boolValue && !multipleValues ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void ConditionChanged(SerializedPropertyChangeEvent changeEvent)
            {
                bool displayConditionsWarning = false;

                m_Data = target as StatusEffectData;

                for (int i = 0; i < conditionsProperty.arraySize; i++)
                {
                    m_Condition = m_Data.Conditions.ElementAtOrDefault(i);

                    if (m_Condition == null || !m_Condition.Add || m_Condition.ActionData != target)
                        continue;

                    displayConditionsWarning = true;
                    break;
                }

                ConditionsWarning.style.display = displayConditionsWarning ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void ModulesAdded(BaseListView view)
            {
                foreach (var target in targets)
                {
                    m_Data = target as StatusEffectData;

                    m_Data.m_Modules.Add(new ModuleContainer());

                    EditorUtility.SetDirty(target);

                    AssetDatabase.SaveAssetIfDirty(target);
                }
            }

            void ModulesRemoved(IEnumerable<int> enumerable)
            {
                foreach (var target in targets)
                {
                    foreach (var index in enumerable)
                    {
                        // Get the module instance
                        ScriptableObject moduleInstance = (target as StatusEffectData).Modules.ElementAt(index)?.ModuleInstance;
                        // Remove the scriptable object from nested assets
                        if (moduleInstance != null)
                        {
                            AssetDatabase.RemoveObjectFromAsset(moduleInstance);
                            DestroyImmediate(moduleInstance);
                        }
                        EditorUtility.SetDirty(target);
                    }

                    AssetDatabase.SaveAssetIfDirty(target);
                }
            }
        }
#else
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
                element.FindPropertyRelative($"m_{nameof(ModuleContainer.Module)}").objectReferenceValue = null;
                element.FindPropertyRelative($"m_{nameof(ModuleContainer.ModuleInstance)}").objectReferenceValue = null;
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
#endif
    }
}