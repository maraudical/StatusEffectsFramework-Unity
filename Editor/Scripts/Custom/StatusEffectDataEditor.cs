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
    [CustomEditor(typeof(StatusEffectData))]
    [CanEditMultipleObjects]
    internal class StatusEffectDataEditor : Editor
    {
        public VisualTreeAsset VisualTree;

        private StatusEffectDatabase m_Database;
        private StatusEffectData m_Data;
        private Condition m_Condition;

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
    }
}