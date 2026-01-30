using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffects.Editor
{
    [CustomEditor(typeof(StatusEffectData))]
    [CanEditMultipleObjects]
    internal class StatusEffectDataEditor : UnityEditor.Editor
    {
        private StatusEffectDatabase m_Database;
        private StatusEffectData m_Data;
        private Condition m_Condition;

        public VisualTreeAsset VisualTree;

        public override VisualElement CreateInspectorGUI()
        {
            bool isPlaying = EditorApplication.isPlaying;
            m_Database = StatusEffectDatabase.Get();

            // Remove any loose nested scriptable objects and
            // iterate in reverse, to match the selected object order.
            // Also check that the StatusEffect is added to the Database.
            foreach (var target in targets)
            {
                // Get the property on the corresponding serializedObject.
                var path = AssetDatabase.GetAssetPath(target);

                m_Data = target as StatusEffectData;

                if (!EditorApplication.isPlaying && !m_Database.ContainsKey(m_Data.Id))
                {
                    m_Database.Add(m_Data.Id, m_Data);
                    EditorUtility.SetDirty(m_Database);
                }

                var modules = m_Data.Modules.Select(m => m.ModuleInstance);
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

            var idProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Id)}");
            var automaticallyAddToDatabaseProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.AutomaticallyAddToDatabase)}");
            var groupProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Group)}");
            var comparableNameProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.ComparableName)}");
            var baseValueProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.BaseValue)}");
            var iconProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Icon)}");
            var colorProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Color)}");
            var nameProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.StatusEffectName)}");
            var acronymProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Acronym)}");
            var descriptionProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Description)}");
            var allowEffectStackingProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.AllowEffectStacking)}");
            var nonStackingBehaviourProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.NonStackingBehaviour)}");
            var maxStacksProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.MaxStacks)}");
            var effectsProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Effects)}");
            var conditionsProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Conditions)}");
            var modulesProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Modules)}");

            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var id = root.Q<PropertyField>("id");
            id.BindProperty(idProperty);
            id.SetEnabled(false);

            var automaticallyAddToDatabase = root.Q<PropertyField>("automatically-add-to-database");
            automaticallyAddToDatabase.BindProperty(automaticallyAddToDatabaseProperty);

            var group = root.Q<PropertyField>("group");
            group.BindProperty(groupProperty);

            var comparableName = root.Q<PropertyField>("comparable-name");
            comparableName.BindProperty(comparableNameProperty);

            var baseValueError = new HelpBox() { text = "Base value cannot be 0!", messageType = HelpBoxMessageType.Error };
            root.Q("base-value-error").Add(baseValueError);

            var baseValue = root.Q<PropertyField>("base-value");
            baseValue.BindProperty(baseValueProperty);

            var icon = root.Q<PropertyField>("icon");
            icon.BindProperty(iconProperty);

            var color = root.Q<PropertyField>("color");
            color.BindProperty(colorProperty);

            var name = root.Q<PropertyField>("name");
            name.BindProperty(nameProperty);

            var acronym = root.Q<PropertyField>("acronym");
            acronym.BindProperty(acronymProperty);

            var description = root.Q<PropertyField>("description");
            description.BindProperty(descriptionProperty);

#if UNITY_2023_1_OR_NEWER

            var optionalTogglesContainer = root.Q("optional-toggles-container");

            var optionalFieldToggles = new ToggleButtonGroup();
            optionalFieldToggles.allowEmptySelection = true;
            optionalFieldToggles.isMultipleSelection = true;
            optionalTogglesContainer.Add(optionalFieldToggles);

            var iconToggle = new Button();
            iconToggle.text = "Icon";
            iconToggle.style.overflow = Overflow.Hidden;
            iconToggle.style.textOverflow = TextOverflow.Ellipsis;
            iconToggle.style.width = Length.Percent(20f);
            optionalFieldToggles.Add(iconToggle);
            var colorToggle = new Button();
            colorToggle.text = "Color";
            colorToggle.style.overflow = Overflow.Hidden;
            colorToggle.style.textOverflow = TextOverflow.Ellipsis;
            colorToggle.style.width = Length.Percent(20f);
            optionalFieldToggles.Add(colorToggle);
            var nameToggle = new Button();
            nameToggle.text = "Name";
            nameToggle.style.overflow = Overflow.Hidden;
            nameToggle.style.textOverflow = TextOverflow.Ellipsis;
            nameToggle.style.width = Length.Percent(20f);
            optionalFieldToggles.Add(nameToggle);
            var acronymToggle = new Button();
            acronymToggle.text = "Acronym";
            acronymToggle.style.overflow = Overflow.Hidden;
            acronymToggle.style.textOverflow = TextOverflow.Ellipsis;
            acronymToggle.style.width = Length.Percent(20f);
            optionalFieldToggles.Add(acronymToggle);
            var descriptionToggle = new Button();
            descriptionToggle.text = "Description";
            descriptionToggle.style.overflow = Overflow.Hidden;
            descriptionToggle.style.textOverflow = TextOverflow.Ellipsis;
            descriptionToggle.style.width = Length.Percent(20f);
            optionalFieldToggles.Add(descriptionToggle);

            List<PropertyField> optionalFields = new()
            {
                icon,
                color,
                name,
                acronym,
                description
            };
#endif

            var allowEffectStacking = root.Q<PropertyField>("allow-effect-stacking");
            allowEffectStacking.BindProperty(allowEffectStackingProperty);

            var nonStackingBehaviour = root.Q<PropertyField>("non-stacking-behaviour");
            nonStackingBehaviour.BindProperty(nonStackingBehaviourProperty);

            var maxStacks = root.Q<PropertyField>("max-stacks");
            maxStacks.BindProperty(maxStacksProperty);

            var effectsList = root.Q<ListView>("effects-list");
            effectsList.BindProperty(effectsProperty);

            var conditionsWarning = new HelpBox() { text = "Do not recursively add status datas! " +
                                                           "Avoid adding a status data to itself! " +
                                                           "Make sure there aren't two that add each other!", 
                                                    messageType = HelpBoxMessageType.Warning };
            root.Q("conditions-warning").Add(conditionsWarning);

            var conditionsList = root.Q<ListView>("conditions-list");
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
            conditionsList.BindProperty(conditionsProperty);

            var modulesList = root.Q<ListView>("modules-list");
            modulesList.SetEnabled(!isPlaying);
#if UNITY_2021_1_OR_NEWER
            modulesList.showAddRemoveFooter = !isPlaying;
#else
            modulesList.showAddRemoveFooter = !modulesProperty.hasMultipleDifferentValues && !isPlaying;
            //modulesList.reorderable = false;
#endif
            modulesList.itemsAdded += ModulesAdded;
            modulesList.itemsRemoved += ModulesRemoved;
            modulesList.BindProperty(modulesProperty);

            

#if UNITY_2023_1_OR_NEWER
            List<SerializedProperty> optionalProperties = new()
            {
                serializedObject.FindProperty("m_EnableIcon"),
                serializedObject.FindProperty("m_EnableColor"),
                serializedObject.FindProperty("m_EnableName"),
                serializedObject.FindProperty("m_EnableAcronym"),
                serializedObject.FindProperty("m_EnableDescription"),
            };

            int bitMask = 0;
            for (int i = 0; i < optionalProperties.Count; i++)
            {
                bool value = optionalProperties[i].boolValue;
                bitMask |= value ? 1 << i : 0;
                optionalFields[i].style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }

            optionalFieldToggles.SetValueWithoutNotify(new ToggleButtonGroupState(Convert.ToUInt64(bitMask), optionalProperties.Count));
            optionalFieldToggles.RegisterValueChangedCallback(OptionalFieldTogglesChanged);

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
#endif

            automaticallyAddToDatabase.RegisterCallback<ChangeEvent<bool>>(AutomaticallyAddToDatabaseChanged);

            BaseValueChanged(default);
            baseValue.RegisterValueChangeCallback(BaseValueChanged);

            ConditionChanged(default);

            AllowEffectStackingChanged(default);
            allowEffectStacking.RegisterValueChangeCallback(AllowEffectStackingChanged);

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

                conditionsWarning.style.display = displayConditionsWarning ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void ModulesAdded(IEnumerable<int> enumerable)
            {
                foreach (var target in targets)
                {
                    m_Data = target as StatusEffectData;

                    foreach (var index in enumerable)
                    {
                        var moduleContainer = m_Data.m_Modules[index];
                        moduleContainer.m_Module = null;
                        moduleContainer.m_ModuleInstance = null;
                    }

                    var path = AssetDatabase.GetAssetPath(target);

                    var modules = m_Data.Modules.Select(m => m.ModuleInstance);
                    var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

                    for (int v = subAssets.Length - 1; v >= 0; v--)
                    {
                        var nestedModule = subAssets[v];
                        // If there is somehow a loose module we need to clean it up.
                        if (!modules.Contains(nestedModule))
                        {
                            AssetDatabase.RemoveObjectFromAsset(nestedModule);
                            DestroyImmediate(nestedModule);
                        }
                    }

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
                    }

                    EditorUtility.SetDirty(target);
                    AssetDatabase.SaveAssetIfDirty(target);
                }
            }
        }
    }
}