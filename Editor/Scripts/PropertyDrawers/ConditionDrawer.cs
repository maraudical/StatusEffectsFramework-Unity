using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(Condition))]
    internal class ConditionDrawer : PropertyDrawer
    {
        public VisualTreeAsset VisualTree;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var searchableConfigurable = root.Q<PropertyField>("searchable-configurable");
            var searchableData = root.Q<PropertyField>("searchable-data");
            var searchableComparableName = root.Q<PropertyField>("searchable-comparable-name");
            var searchableGroup = root.Q<PropertyField>("searchable-group");
            var isLabel = root.Q<Label>("is-label");
            var existence = root.Q<EnumField>("existence");
            var thenLabel = root.Q<Label>("then-label");
            var configurability = root.Q<EnumField>("configurability");
            var stacks = root.Q<PropertyField>("stacks");
            var scaleOption = root.Q<EnumField>("scale-option");
            var removeOption = root.Q<EnumField>("remove-option");
            var stacksLabel = root.Q<Label>("stacks-label");
            var ofLabel = root.Q<Label>("of-label");
            var actionConfigurable = root.Q<PropertyField>("action-configurable");
            var actionData = root.Q<PropertyField>("action-data");
            var actionComparableName = root.Q<PropertyField>("action-comparable-name");
            var actionGroup = root.Q<PropertyField>("action-group");
            var duration = root.Q<PropertyField>("duration");
            var timing = root.Q<PropertyField>("timing");
            var dashLabel = root.Q<Label>("dash-label");

            var searchableConfigurableProperty = property.FindPropertyRelative($"m_{nameof(Condition.SearchableConfigurable)}");
            var existsProperty = property.FindPropertyRelative($"m_{nameof(Condition.Exists)}");
            var addProperty = property.FindPropertyRelative($"m_{nameof(Condition.Add)}");
            var scaledProperty = property.FindPropertyRelative($"m_{nameof(Condition.Scaled)}");
            var useStacksProperty = property.FindPropertyRelative($"m_{nameof(Condition.UseStacks)}");
            var actionConfigurableProperty = property.FindPropertyRelative($"m_{nameof(Condition.ActionConfigurable)}");
            var timingProperty = property.FindPropertyRelative($"m_{nameof(Condition.Timing)}");

            searchableConfigurable.label = string.Empty;
            searchableConfigurable.RegisterValueChangeCallback(SearchableConfigurableValueChanged);

            searchableData.label = string.Empty;

            searchableComparableName.label = string.Empty;

            searchableGroup.RegisterCallbackOnce<GeometryChangedEvent>(SearchableGroupGeometryChanged);

            existence.SetValueWithoutNotify((Existence)Convert.ToInt32(existsProperty.boolValue));
            existence.showMixedValue = existsProperty.hasMultipleDifferentValues;
            existence.RegisterValueChangedCallback(ExistenceValueChanged);

            configurability.SetValueWithoutNotify((Configurability)Convert.ToInt32(addProperty.boolValue));
            configurability.showMixedValue = addProperty.hasMultipleDifferentValues;
            configurability.RegisterValueChangedCallback(ConfigurabilityValueChanged);

            scaleOption.SetValueWithoutNotify((ScaleOption)Convert.ToInt32(scaledProperty.boolValue));
            scaleOption.showMixedValue = scaledProperty.hasMultipleDifferentValues;
            scaleOption.RegisterValueChangedCallback(ScaleOptionValueChanged);

            removeOption.SetValueWithoutNotify((RemoveOption)Convert.ToInt32(useStacksProperty.boolValue));
            removeOption.showMixedValue = useStacksProperty.hasMultipleDifferentValues;
            removeOption.RegisterValueChangedCallback(RemoveOptionValueChanged);

            actionConfigurable.label = string.Empty;
            actionConfigurable.RegisterValueChangeCallback(ActionConfigurableValueChanged);

            actionData.label = string.Empty;

            actionComparableName.label = string.Empty;

            actionGroup.RegisterCallbackOnce<GeometryChangedEvent>(ActionGroupGeometryChanged);

            duration.label = string.Empty;

            timing.label = string.Empty;
            timing.RegisterValueChangeCallback(TimingValueChanged);

            EvaluateProperties();

            return root;

            void SearchableConfigurableValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
            }

            void SearchableGroupGeometryChanged(GeometryChangedEvent changeEvent)
            {
                searchableGroup.Q<MaskField>("mask-field").label = string.Empty;
            }

            void ExistenceValueChanged(ChangeEvent<Enum> changeEvent)
            {
                existsProperty.boolValue = Convert.ToBoolean((int)(Existence)changeEvent.newValue);
                existsProperty.serializedObject.ApplyModifiedProperties();
            }

            void ConfigurabilityValueChanged(ChangeEvent<Enum> changeEvent)
            {
                addProperty.boolValue = Convert.ToBoolean((int)(Configurability)changeEvent.newValue);
                addProperty.serializedObject.ApplyModifiedProperties();
                EvaluateProperties();
            }

            void ScaleOptionValueChanged(ChangeEvent<Enum> changeEvent)
            {
                scaledProperty.boolValue = Convert.ToBoolean((int)(ScaleOption)changeEvent.newValue);
                scaledProperty.serializedObject.ApplyModifiedProperties();
            }

            void RemoveOptionValueChanged(ChangeEvent<Enum> changeEvent)
            {
                useStacksProperty.boolValue = Convert.ToBoolean((int)(RemoveOption)changeEvent.newValue);
                useStacksProperty.serializedObject.ApplyModifiedProperties();
                EvaluateProperties();
            }

            void ActionConfigurableValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
            }

            void ActionGroupGeometryChanged(GeometryChangedEvent changeEvent)
            {
                actionGroup.Q<MaskField>("mask-field").label = string.Empty;
            }

            void TimingValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
            }

            void EvaluateProperties()
            {
                var searchableConfigurableValue = (ConditionalConfigurable)searchableConfigurableProperty.enumValueIndex;
                var actionConfigurableValue = (ConditionalConfigurable)actionConfigurableProperty.enumValueIndex;
                var conditionalTiming = (ConditionalTiming)timingProperty.enumValueIndex;

                bool searchableDifference = searchableConfigurableProperty.hasMultipleDifferentValues;
                bool addDifference = searchableDifference || addProperty.hasMultipleDifferentValues;
                bool useStacksDifference = addDifference || (!addProperty.boolValue && useStacksProperty.hasMultipleDifferentValues);
                bool actionDifference = addDifference || (!addProperty.boolValue && actionConfigurableProperty.hasMultipleDifferentValues);
                bool timingDifference = addDifference || (addProperty.boolValue && timingProperty.hasMultipleDifferentValues);

                bool anyDifference = searchableDifference || addDifference || useStacksDifference || actionDifference;

                searchableData.style.display = !searchableDifference && searchableConfigurableValue is ConditionalConfigurable.Data ? DisplayStyle.Flex : DisplayStyle.None;
                searchableComparableName.style.display = !searchableDifference && searchableConfigurableValue is ConditionalConfigurable.Name ? DisplayStyle.Flex : DisplayStyle.None;
                searchableGroup.style.display = !searchableDifference && searchableConfigurableValue is ConditionalConfigurable.Group ? DisplayStyle.Flex : DisplayStyle.None;
                isLabel.style.display = !searchableDifference ? DisplayStyle.Flex : DisplayStyle.None;
                existence.style.display = !searchableDifference ? DisplayStyle.Flex : DisplayStyle.None;
                thenLabel.style.display = !searchableDifference ? DisplayStyle.Flex : DisplayStyle.None;
                configurability.style.display = !searchableDifference ? DisplayStyle.Flex : DisplayStyle.None;
                stacks.style.display = !addDifference && !useStacksDifference && (addProperty.boolValue || useStacksProperty.boolValue) ? DisplayStyle.Flex : DisplayStyle.None;
                scaleOption.style.display = !addDifference && !useStacksDifference && (addProperty.boolValue || useStacksProperty.boolValue) ? DisplayStyle.Flex : DisplayStyle.None;
                removeOption.style.display = !addDifference && !addProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                stacksLabel.style.display = !addDifference && addProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                ofLabel.style.display = !addDifference && !useStacksDifference ? DisplayStyle.Flex : DisplayStyle.None;
                actionConfigurable.style.display = !useStacksDifference && !addProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                actionData.style.display = !addDifference && !actionDifference && (addProperty.boolValue || actionConfigurableValue is ConditionalConfigurable.Data) ? DisplayStyle.Flex : DisplayStyle.None;
                actionComparableName.style.display = !addDifference && !actionDifference && !addProperty.boolValue && actionConfigurableValue is ConditionalConfigurable.Name ? DisplayStyle.Flex : DisplayStyle.None;
                actionGroup.style.display = !addDifference && !actionDifference && !addProperty.boolValue && actionConfigurableValue is ConditionalConfigurable.Group ? DisplayStyle.Flex : DisplayStyle.None;
                duration.style.display = !timingDifference && addProperty.boolValue && conditionalTiming is ConditionalTiming.Duration ? DisplayStyle.Flex : DisplayStyle.None;
                timing.style.display = !addDifference && addProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                dashLabel.style.display = anyDifference ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public enum Existence
        {
            Inactive,
            Active
        }

        public enum Configurability
        {
            Remove,
            Add
        }

        public enum RemoveOption
        {
            All,
            Stacks,
        }

        public enum ScaleOption
        {
            Unscaled,
            Scaled
        }
    }
}
