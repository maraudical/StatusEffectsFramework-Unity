using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffectsFramework.Editor
{
    [CustomEditor(typeof(StatusEffectData))]
    [CanEditMultipleObjects]
    internal class StatusEffectDataEditor : UnityEditor.Editor
    {
        public VisualTreeAsset VisualTree;

        public override VisualElement CreateInspectorGUI()
        {
            bool isPlaying = EditorApplication.isPlaying;

#if ADDRESSABLES
            var optionalRegistryDependencyProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.OptionalRegistryDependency)}");
#endif
            var keyProperty = serializedObject.FindProperty($"m_{nameof(StatusEffectData.Key)}");
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

#if ADDRESSABLES
            var optionalRegistryDependency = root.Q<PropertyField>("optional-registry-dependency");
            optionalRegistryDependency.BindProperty(optionalRegistryDependencyProperty);

#endif
            var key = root.Q<PropertyField>("key");
            key.BindProperty(keyProperty);

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


            var optionalTogglesContainer = root.Q("optional-toggles-container");

            var optionalFieldToggles = new ToggleButtonGroup();
            optionalFieldToggles.allowEmptySelection = true;
            optionalFieldToggles.isMultipleSelection = true;
            optionalTogglesContainer.Add(optionalFieldToggles);

            foreach (var optionalFieldName in new[] { "Icon", "Color", "Name", "Acronym", "Description" })
                optionalFieldToggles.Add(CreateOptionalFieldToggle(optionalFieldName));

            List<PropertyField> optionalFields = new()
            {
                icon,
                color,
                name,
                acronym,
                description
            };

            var allowEffectStacking = root.Q<PropertyField>("allow-effect-stacking");
            allowEffectStacking.BindProperty(allowEffectStackingProperty);

            var nonStackingBehaviour = root.Q<PropertyField>("non-stacking-behaviour");
            nonStackingBehaviour.BindProperty(nonStackingBehaviourProperty);

            var maxStacks = root.Q<PropertyField>("max-stacks");
            maxStacks.BindProperty(maxStacksProperty);

            var effectsList = root.Q<ListView>("effects-list");
            effectsList.BindProperty(effectsProperty);

            var conditionsWarning = new HelpBox()
            {
                text = "Do not recursively add status datas! " +
                                                           "Avoid adding a status data to itself! " +
                                                           "Make sure there aren't two that add each other!",
                messageType = HelpBoxMessageType.Warning
            };
            root.Q("conditions-warning").Add(conditionsWarning);

            var conditionsList = root.Q<ListView>("conditions-list");
            conditionsList.makeItem = () =>
            {
                var propertyField = new PropertyField();
                // Items are reused when scrolling, so the callback is only added once.
                propertyField.RegisterValueChangeCallback(ConditionChanged);
                return propertyField;
            };
            conditionsList.bindItem = (existingElement, index) =>
            {
                var propertyField = existingElement as PropertyField;
                propertyField.BindProperty(conditionsProperty.GetArrayElementAtIndex(index));
            };
            conditionsList.BindProperty(conditionsProperty);

            var modulesList = root.Q<ListView>("modules-list");
            modulesList.SetEnabled(!isPlaying);
            modulesList.showAddRemoveFooter = !isPlaying;
            modulesList.itemsAdded += ModulesAdded;
            modulesList.itemsRemoved += ModulesRemoved;
            modulesList.BindProperty(modulesProperty);



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

            Button CreateOptionalFieldToggle(string text)
            {
                var toggle = new Button { text = text };
                toggle.style.overflow = Overflow.Hidden;
                toggle.style.textOverflow = TextOverflow.Ellipsis;
                toggle.style.width = Length.Percent(20f);
                return toggle;
            }

            BaseValueChanged(default);
            baseValue.RegisterValueChangeCallback(BaseValueChanged);

            ConditionChanged(default);

            AllowEffectStackingChanged(default);
            allowEffectStacking.RegisterValueChangeCallback(AllowEffectStackingChanged);

            return root;

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

                var data = target as StatusEffectData;

                for (int i = 0; i < conditionsProperty.arraySize; i++)
                {
                    var condition = data.Conditions.ElementAtOrDefault(i);

                    if (condition == null || !condition.IsAdded || condition.ActionData != data)
                        continue;

                    displayConditionsWarning = true;
                    break;
                }

                conditionsWarning.style.display = displayConditionsWarning ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void ModulesAdded(IEnumerable<int> enumerable)
            {
                // The new items are cleared directly, so undo needs to know about them.
                Undo.RecordObjects(targets, "Add Module");

                foreach (var target in targets)
                {
                    var data = target as StatusEffectData;

                    foreach (var index in enumerable)
                    {
                        var moduleContainer = data.m_Modules[index];
                        moduleContainer.m_Module = null;
                        moduleContainer.m_ModuleInstance = null;
                    }

                    RemoveLooseModuleInstances(data);

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

        /// <summary>
        /// Removes nested <see cref="ModuleInstance"/> assets that no module references. Other sub assets are
        /// left alone. Returns the number removed. Also runs whenever the asset is saved, see
        /// <see cref="ModuleInstanceSaveProcessor"/>.
        /// </summary>
        internal static int RemoveLooseModuleInstances(StatusEffectData data)
        {
            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(data));
            if (subAssets.Length == 0)
                return 0;

            var moduleInstances = new HashSet<UnityEngine.Object>();
            foreach (var module in data.Modules)
                if (module.ModuleInstance != null)
                    moduleInstances.Add(module.ModuleInstance);

            int removed = 0;
            for (int i = subAssets.Length - 1; i >= 0; i--)
            {
                // Sub assets whose script can't be loaded come back as null, so they also fail this check.
                // They can't be removed here, and the script may only be missing temporarily, such as during
                // a compile error.
                if (subAssets[i] is not ModuleInstance nestedAsset)
                    continue;
                // If there is somehow a loose module we need to clean it up.
                if (moduleInstances.Contains(nestedAsset))
                    continue;

                AssetDatabase.RemoveObjectFromAsset(nestedAsset);
                DestroyImmediate(nestedAsset);
                removed++;
            }

            if (removed > 0)
                EditorUtility.SetDirty(data);

            return removed;
        }

        /// <summary>
        /// Removes <see cref="ModuleInstance"/> assets no module references and nested assets whose script can't
        /// be loaded. Missing scripts are only removed here, not in <see cref="RemoveLooseModuleInstances"/>,
        /// since the script may only be missing temporarily.
        /// </summary>
        [MenuItem("CONTEXT/StatusEffectData/Remove Loose Sub Assets")]
        private static void RemoveLooseSubAssets(MenuCommand command)
        {
            var path = AssetDatabase.GetAssetPath(command.context);
            int looseCount = RemoveLooseModuleInstances((StatusEffectData)command.context);
            int missingCount = AssetDatabase.RemoveScriptableObjectsWithMissingScript(path);

            if (looseCount + missingCount > 0)
            {
                AssetDatabase.SaveAssetIfDirty(command.context);
                // The Project window keeps showing the removed sub assets until the asset is reimported.
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            Debug.Log($"Removed {looseCount} loose module instance(s) and {missingCount} sub asset(s) with missing scripts from {path}.", command.context);
        }
    }
}