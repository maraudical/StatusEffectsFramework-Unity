using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using UnityEditor;
using UnityEngine;

namespace StatusEffectFramework.Editor
{
    [CustomPropertyDrawer(typeof(Condition))]
    internal class ConditionDrawer : PropertyDrawer
    {
        public VisualTreeAsset VisualTree;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var searchableConfigurableProperty = property.FindPropertyRelative($"m_{nameof(Condition.SearchableConfigurable)}");
            var searchableDataProperty = property.FindPropertyRelative($"m_{nameof(Condition.SearchableData)}");
            var searchableComparableNameProperty = property.FindPropertyRelative($"m_{nameof(Condition.SearchableComparableName)}");
            var searchableGroupProperty = property.FindPropertyRelative($"m_{nameof(Condition.SearchableGroup)}");
            var existsProperty = property.FindPropertyRelative($"m_{nameof(Condition.Exists)}");
            var addProperty = property.FindPropertyRelative($"m_{nameof(Condition.Add)}");
            var stacksProperty = property.FindPropertyRelative($"m_{nameof(Condition.Stacks)}");
            var scaledProperty = property.FindPropertyRelative($"m_{nameof(Condition.Scaled)}");
            var useStacksProperty = property.FindPropertyRelative($"m_{nameof(Condition.UseStacks)}");
            var actionConfigurableProperty = property.FindPropertyRelative($"m_{nameof(Condition.ActionConfigurable)}");
            var actionDataProperty = property.FindPropertyRelative($"m_{nameof(Condition.ActionData)}");
            var actionComparableNameProperty = property.FindPropertyRelative($"m_{nameof(Condition.ActionComparableName)}");
            var actionGroupProperty = property.FindPropertyRelative($"m_{nameof(Condition.ActionGroup)}");
            var durationProperty = property.FindPropertyRelative($"m_{nameof(Condition.Duration)}");
            var timingProperty = property.FindPropertyRelative($"m_{nameof(Condition.Timing)}");

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.marginRight = -3;
            root.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
            root.AddToClassList(StatusEffectsStyleSheet.MaskFieldSizeClassName);

            var ifLabel = new Label("If");
            ifLabel.style.paddingLeft = 0;
            ifLabel.style.paddingRight = 1;
            ifLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(ifLabel);

            var searchableConfigurable = new PropertyField(searchableConfigurableProperty, string.Empty);
            searchableConfigurable.style.flexGrow = 0;
            searchableConfigurable.style.flexShrink = 0;
            root.Add(searchableConfigurable);

            var searchableData = new PropertyField(searchableDataProperty, string.Empty);
            searchableData.style.minWidth = 92;
            searchableData.style.flexGrow = 1;
            root.Add(searchableData);
            
            var searchableComparableName = new PropertyField(searchableComparableNameProperty, string.Empty);
            searchableComparableName.style.minWidth = 92;
            searchableComparableName.style.flexGrow = 1;
            root.Add(searchableComparableName);
            
            var searchableGroup = new PropertyField(searchableGroupProperty, string.Empty);
            searchableGroup.style.minWidth = 92;
            searchableGroup.style.flexGrow = 1;
            root.Add(searchableGroup);
            
            var isLabel = new Label("is");
            isLabel.style.paddingLeft = 2;
            isLabel.style.paddingRight = 2;
            isLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(isLabel);

            var existence = new EnumField();
            existence.Init(new Existence());
            root.Add(existence);

            var thenLabel = new Label("then");
            thenLabel.style.paddingLeft = 2;
            thenLabel.style.paddingRight = 2;
            thenLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(thenLabel);

            var configurability = new EnumField();
            configurability.Init(new Configurability());
            root.Add(configurability);
            
            var stacks = new PropertyField(stacksProperty, string.Empty);
            stacks.style.flexShrink = 0;
            stacks.style.marginRight = 0;
            stacks.style.marginLeft = 0;
            root.Add(stacks);

            var scaleOption = new EnumField();
            scaleOption.Init(new ScaleOption());
            root.Add(scaleOption);

            var removeOption = new EnumField();
            removeOption.Init(new RemoveOption());
            root.Add(removeOption);

            var stacksLabel = new Label("stacks");
            stacksLabel.style.paddingLeft = 2;
            stacksLabel.style.paddingRight = 2;
            stacksLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(stacksLabel);

            var ofLabel = new Label("of");
            ofLabel.style.paddingLeft = 2;
            ofLabel.style.paddingRight = 2;
            ofLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(ofLabel);

            var actionConfigurable = new PropertyField(actionConfigurableProperty, string.Empty);
            actionConfigurable.style.flexGrow = 0;
            actionConfigurable.style.flexShrink = 0;
            root.Add(actionConfigurable);

            var actionData = new PropertyField(actionDataProperty, string.Empty);
            actionData.style.minWidth = 92;
            actionData.style.flexGrow = 1;
            root.Add(actionData);

            var actionComparableName = new PropertyField(actionComparableNameProperty, string.Empty);
            actionComparableName.style.minWidth = 92;
            actionComparableName.style.flexGrow = 1;
            root.Add(actionComparableName);

            var actionGroup = new PropertyField(actionGroupProperty, string.Empty);
            actionGroup.style.minWidth = 92;
            actionGroup.style.flexGrow = 1;
            root.Add(actionGroup);
            
            var duration = new PropertyField(durationProperty, string.Empty);
            duration.style.flexShrink = 0;
            root.Add(duration);

            var timing = new PropertyField(timingProperty, string.Empty);
            timing.style.flexShrink = 0;
            root.Add(timing);

            var dashLabel = new Label("—");
            dashLabel.style.paddingLeft = 2;
            dashLabel.style.paddingRight = 2;
            dashLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            dashLabel.style.opacity = 0.5f;
            root.Add(dashLabel);

            searchableConfigurable.RegisterValueChangeCallback(SearchableConfigurableValueChanged);

            searchableGroup.RegisterCallback<GeometryChangedEvent>(SearchableGroupGeometryChanged);

            existence.SetValueWithoutNotify((Existence)Convert.ToInt32(existsProperty.boolValue));
            existence.showMixedValue = existsProperty.hasMultipleDifferentValues;
            existence.RegisterValueChangedCallback(ExistenceValueChanged);

            configurability.SetValueWithoutNotify((Configurability)Convert.ToInt32(addProperty.boolValue));
            configurability.showMixedValue = addProperty.hasMultipleDifferentValues;
            configurability.RegisterValueChangedCallback(ConfigurabilityValueChanged);

            stacks.RegisterCallback<GeometryChangedEvent>(StacksGeometryChanged);

            scaleOption.SetValueWithoutNotify((ScaleOption)Convert.ToInt32(scaledProperty.boolValue));
            scaleOption.showMixedValue = scaledProperty.hasMultipleDifferentValues;
            scaleOption.RegisterValueChangedCallback(ScaleOptionValueChanged);

            removeOption.SetValueWithoutNotify((RemoveOption)Convert.ToInt32(useStacksProperty.boolValue));
            removeOption.showMixedValue = useStacksProperty.hasMultipleDifferentValues;
            removeOption.RegisterValueChangedCallback(RemoveOptionValueChanged);

            actionConfigurable.RegisterValueChangeCallback(ActionConfigurableValueChanged);

            actionGroup.RegisterCallback<GeometryChangedEvent>(ActionGroupGeometryChanged);

            duration.RegisterCallback<GeometryChangedEvent>(DurationGeometryChanged);

            timing.RegisterValueChangeCallback(TimingValueChanged);

            EvaluateProperties();

            // Fix for unity inspector margins
            root.RegisterCallback<GeometryChangedEvent>(RootGeometryChanged);

            return root;

            void SearchableConfigurableValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
            }

            void SearchableGroupGeometryChanged(GeometryChangedEvent changeEvent)
            {
                searchableGroup.UnregisterCallback<GeometryChangedEvent>(SearchableGroupGeometryChanged);
                searchableGroup.Q<MaskField>().label = string.Empty;
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

            void StacksGeometryChanged(GeometryChangedEvent changeEvent)
            {
                stacks.UnregisterCallback<GeometryChangedEvent>(StacksGeometryChanged);
                stacks.Q<IntegerField>().label = string.Empty;
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
                actionGroup.UnregisterCallback<GeometryChangedEvent>(ActionGroupGeometryChanged);
                actionGroup.Q<MaskField>().label = string.Empty;
            }

            void DurationGeometryChanged(GeometryChangedEvent changeEvent)
            {
                duration.UnregisterCallback<GeometryChangedEvent>(DurationGeometryChanged);
                duration.Q<FloatField>().label = string.Empty;
            }

            void TimingValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
            }

            void RootGeometryChanged(GeometryChangedEvent evt)
            {
                timing.UnregisterCallback<GeometryChangedEvent>(RootGeometryChanged);

                SetMargin(searchableConfigurable.Q(className: PopupField<int>.ussClassName));
                SetMargin(searchableData.Q<ObjectField>());
                SetMargin(searchableComparableName.Q<ObjectField>());
                SetMargin(searchableGroup.Q<MaskField>());
                SetMargin(existence.Q<EnumField>());
                SetMargin(configurability.Q<EnumField>());
                SetMargin(stacks.Q<IntegerField>());
                SetMargin(scaleOption.Q<EnumField>());
                SetMargin(removeOption.Q<EnumField>());
                SetMargin(actionConfigurable.Q(className: PopupField<int>.ussClassName));
                SetMargin(actionData.Q<ObjectField>());
                SetMargin(actionComparableName.Q<ObjectField>());
                SetMargin(actionGroup.Q<MaskField>());
                SetMargin(duration.Q<FloatField>());
                SetMargin(timing.Q(className: PopupField<int>.ussClassName));

                void SetMargin(VisualElement element)
                {
                    element.style.marginRight = 1;
                    element.style.marginLeft = 1;
                }
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
                searchableGroup.style.display = !searchableDifference && searchableConfigurableValue is ConditionalConfigurable.AllGroups or ConditionalConfigurable.AnyGroups ? DisplayStyle.Flex : DisplayStyle.None;
                isLabel.style.display = !searchableDifference ? DisplayStyle.Flex : DisplayStyle.None;
                isLabel.text = searchableConfigurableValue is ConditionalConfigurable.AllGroups or ConditionalConfigurable.AnyGroups ? "are" : "is";
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
                actionGroup.style.display = !addDifference && !actionDifference && !addProperty.boolValue && actionConfigurableValue is ConditionalConfigurable.AllGroups or ConditionalConfigurable.AnyGroups ? DisplayStyle.Flex : DisplayStyle.None;
                duration.style.display = !timingDifference && addProperty.boolValue && conditionalTiming is ConditionalTiming.Duration ? DisplayStyle.Flex : DisplayStyle.None;
                timing.style.display = !addDifference && addProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                dashLabel.style.display = anyDifference ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public enum Existence
        {
            Absent,
            Present
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
