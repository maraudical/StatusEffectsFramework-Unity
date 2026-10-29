using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using UnityEditor;

namespace StatusEffectsFramework.Editor
{
    [CustomPropertyDrawer(typeof(Condition))]
    internal class ConditionDrawer : PropertyDrawer
    {
        /// <summary>
        /// Main USS class name of the Condition.
        /// </summary>
        public const string ussClassName = "sef-condition";

        /// <summary>
        /// USS class name of the Condition's labels.
        /// </summary>
        public const string labelUssClassName = ussClassName + "__label";

        /// <summary>
        /// USS class name of the Condition's "If" label.
        /// </summary>
        public const string ifLabelUssClassName = ussClassName + "__if-label";

        /// <summary>
        /// USS class name of the Condition's searchable configurable field.
        /// </summary>
        public const string searchableConfigurableUssClassName = ussClassName + "__searchable-configurable";

        /// <summary>
        /// USS class name of the Condition's searchable data field.
        /// </summary>
        public const string searchableDataUssClassName = ussClassName + "__searchable-data";

        /// <summary>
        /// USS class name of the Condition's searchable comparable name field.
        /// </summary>
        public const string searchableComparableNameUssClassName = ussClassName + "__searchable-comparable-name";

        /// <summary>
        /// USS class name of the Condition's searchable group field.
        /// </summary>
        public const string searchableGroupUssClassName = ussClassName + "__searchable-group";

        /// <summary>
        /// USS class name of the Condition's "is" label.
        /// </summary>
        public const string isLabelUssClassName = ussClassName + "__is-label";

        /// <summary>
        /// USS class name of the Condition's existence field.
        /// </summary>
        public const string existenceUssClassName = ussClassName + "__existence";

        /// <summary>
        /// USS class name of the Condition's "then" label.
        /// </summary>
        public const string thenLabelUssClassName = ussClassName + "__then-label";

        /// <summary>
        /// USS class name of the Condition's configurability field.
        /// </summary>
        public const string configurabilityUssClassName = ussClassName + "__configurability";

        /// <summary>
        /// USS class name of the Condition's stacks field.
        /// </summary>
        public const string stacksUssClassName = ussClassName + "__stacks";

        /// <summary>
        /// USS class name of the Condition's scale option field.
        /// </summary>
        public const string scaleOptionUssClassName = ussClassName + "__scale-option";

        /// <summary>
        /// USS class name of the Condition's remove option field.
        /// </summary>
        public const string removeOptionUssClassName = ussClassName + "__remove-option";

        /// <summary>
        /// USS class name of the Condition's "stacks" label.
        /// </summary>
        public const string stacksLabelUssClassName = ussClassName + "__stacks-label";

        /// <summary>
        /// USS class name of the Condition's "of" label.
        /// </summary>
        public const string ofLabelUssClassName = ussClassName + "__of-label";

        /// <summary>
        /// USS class name of the Condition's action configurable field.
        /// </summary>
        public const string actionConfigurableUssClassName = ussClassName + "__action-configurable";

        /// <summary>
        /// USS class name of the Condition's action data field.
        /// </summary>
        public const string actionDataUssClassName = ussClassName + "__action-data";

        /// <summary>
        /// USS class name of the Condition's action comparable name field.
        /// </summary>
        public const string actionComparableNameUssClassName = ussClassName + "__action-comparable-name";

        /// <summary>
        /// USS class name of the Condition's action group field.
        /// </summary>
        public const string actionGroupUssClassName = ussClassName + "__action-group";

        /// <summary>
        /// USS class name of the Condition's duration field.
        /// </summary>
        public const string durationUssClassName = ussClassName + "__duration";

        /// <summary>
        /// USS class name of the Condition's timing field.
        /// </summary>
        public const string timingUssClassName = ussClassName + "__timing";

        /// <summary>
        /// USS class name of the Condition's dash label, shown while there are mixed values.
        /// </summary>
        public const string dashLabelUssClassName = ussClassName + "__dash-label";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var searchableConfigurableProperty = property.FindPropertyRelative($"m_{nameof(Condition.SearchableConfigurable)}");
            var searchableDataProperty = property.FindPropertyRelative($"m_{nameof(Condition.SearchableData)}");
            var searchableComparableNameProperty = property.FindPropertyRelative($"m_{nameof(Condition.SearchableComparableName)}");
            var searchableGroupProperty = property.FindPropertyRelative($"m_{nameof(Condition.SearchableGroup)}");
            var isPresentProperty = property.FindPropertyRelative($"m_{nameof(Condition.IsPresent)}");
            var isAddedProperty = property.FindPropertyRelative($"m_{nameof(Condition.IsAdded)}");
            var stacksProperty = property.FindPropertyRelative($"m_{nameof(Condition.Stacks)}");
            var isScaledProperty = property.FindPropertyRelative($"m_{nameof(Condition.IsScaled)}");
            var useStacksProperty = property.FindPropertyRelative($"m_{nameof(Condition.UseStacks)}");
            var actionConfigurableProperty = property.FindPropertyRelative($"m_{nameof(Condition.ActionConfigurable)}");
            var actionDataProperty = property.FindPropertyRelative($"m_{nameof(Condition.ActionData)}");
            var actionComparableNameProperty = property.FindPropertyRelative($"m_{nameof(Condition.ActionComparableName)}");
            var actionGroupProperty = property.FindPropertyRelative($"m_{nameof(Condition.ActionGroup)}");
            var durationProperty = property.FindPropertyRelative($"m_{nameof(Condition.Duration)}");
            var timingProperty = property.FindPropertyRelative($"m_{nameof(Condition.Timing)}");

            var root = new VisualElement
            {
                name = ussClassName
            };
            root.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
            root.AddToClassList(ussClassName);
            root.AddToClassList(StatusEffectsStyleSheet.MaskFieldSizeClassName);

            var ifLabel = new Label("If")
            {
                name = ifLabelUssClassName
            };
            ifLabel.AddToClassList(labelUssClassName);
            ifLabel.AddToClassList(ifLabelUssClassName);
            root.Add(ifLabel);

            var searchableConfigurable = new PropertyField(searchableConfigurableProperty, string.Empty)
            {
                name = searchableConfigurableUssClassName
            };
            searchableConfigurable.AddToClassList(searchableConfigurableUssClassName);
            root.Add(searchableConfigurable);

            var searchableData = new PropertyField(searchableDataProperty, string.Empty)
            {
                name = searchableDataUssClassName
            };
            searchableData.AddToClassList(searchableDataUssClassName);
            root.Add(searchableData);

            var searchableComparableName = new PropertyField(searchableComparableNameProperty, string.Empty)
            {
                name = searchableComparableNameUssClassName
            };
            searchableComparableName.AddToClassList(searchableComparableNameUssClassName);
            root.Add(searchableComparableName);

            var searchableGroup = new PropertyField(searchableGroupProperty, string.Empty)
            {
                name = searchableGroupUssClassName
            };
            searchableGroup.AddToClassList(searchableGroupUssClassName);
            root.Add(searchableGroup);

            var isLabel = new Label("is")
            {
                name = isLabelUssClassName
            };
            isLabel.AddToClassList(labelUssClassName);
            isLabel.AddToClassList(isLabelUssClassName);
            root.Add(isLabel);

            var existence = new EnumField
            {
                name = existenceUssClassName
            };
            existence.Init(new Existence());
            existence.AddToClassList(existenceUssClassName);
            root.Add(existence);

            var thenLabel = new Label("then")
            {
                name = thenLabelUssClassName
            };
            thenLabel.AddToClassList(labelUssClassName);
            thenLabel.AddToClassList(thenLabelUssClassName);
            root.Add(thenLabel);

            var configurability = new EnumField
            {
                name = configurabilityUssClassName
            };
            configurability.Init(new Configurability());
            configurability.AddToClassList(configurabilityUssClassName);
            root.Add(configurability);

            var stacks = new PropertyField(stacksProperty, string.Empty)
            {
                name = stacksUssClassName
            };
            stacks.AddToClassList(stacksUssClassName);
            root.Add(stacks);

            var scaleOption = new EnumField
            {
                name = scaleOptionUssClassName
            };
            scaleOption.Init(new ScaleOption());
            scaleOption.AddToClassList(scaleOptionUssClassName);
            root.Add(scaleOption);

            var removeOption = new EnumField
            {
                name = removeOptionUssClassName
            };
            removeOption.Init(new RemoveOption());
            removeOption.AddToClassList(removeOptionUssClassName);
            root.Add(removeOption);

            var stacksLabel = new Label("stacks")
            {
                name = stacksLabelUssClassName
            };
            stacksLabel.AddToClassList(labelUssClassName);
            stacksLabel.AddToClassList(stacksLabelUssClassName);
            root.Add(stacksLabel);

            var ofLabel = new Label("of")
            {
                name = ofLabelUssClassName
            };
            ofLabel.AddToClassList(labelUssClassName);
            ofLabel.AddToClassList(ofLabelUssClassName);
            root.Add(ofLabel);

            var actionConfigurable = new PropertyField(actionConfigurableProperty, string.Empty)
            {
                name = actionConfigurableUssClassName
            };
            actionConfigurable.AddToClassList(actionConfigurableUssClassName);
            root.Add(actionConfigurable);

            var actionData = new PropertyField(actionDataProperty, string.Empty)
            {
                name = actionDataUssClassName
            };
            actionData.AddToClassList(actionDataUssClassName);
            root.Add(actionData);

            var actionComparableName = new PropertyField(actionComparableNameProperty, string.Empty)
            {
                name = actionComparableNameUssClassName
            };
            actionComparableName.AddToClassList(actionComparableNameUssClassName);
            root.Add(actionComparableName);

            var actionGroup = new PropertyField(actionGroupProperty, string.Empty)
            {
                name = actionGroupUssClassName
            };
            actionGroup.AddToClassList(actionGroupUssClassName);
            root.Add(actionGroup);

            var duration = new PropertyField(durationProperty, string.Empty)
            {
                name = durationUssClassName
            };
            duration.AddToClassList(durationUssClassName);
            root.Add(duration);

            var timing = new PropertyField(timingProperty, string.Empty)
            {
                name = timingUssClassName
            };
            timing.AddToClassList(timingUssClassName);
            root.Add(timing);

            var dashLabel = new Label("\u2014")
            {
                name = dashLabelUssClassName
            };
            dashLabel.AddToClassList(labelUssClassName);
            dashLabel.AddToClassList(dashLabelUssClassName);
            root.Add(dashLabel);

            searchableConfigurable.RegisterValueChangeCallback(SearchableConfigurableValueChanged);

            existence.SetValueWithoutNotify((Existence)Convert.ToInt32(isPresentProperty.boolValue));
            existence.showMixedValue = isPresentProperty.hasMultipleDifferentValues;
            existence.RegisterValueChangedCallback(ExistenceValueChanged);

            configurability.SetValueWithoutNotify((Configurability)Convert.ToInt32(isAddedProperty.boolValue));
            configurability.showMixedValue = isAddedProperty.hasMultipleDifferentValues;
            configurability.RegisterValueChangedCallback(ConfigurabilityValueChanged);

            scaleOption.SetValueWithoutNotify((ScaleOption)Convert.ToInt32(isScaledProperty.boolValue));
            scaleOption.showMixedValue = isScaledProperty.hasMultipleDifferentValues;
            scaleOption.RegisterValueChangedCallback(ScaleOptionValueChanged);

            removeOption.SetValueWithoutNotify((RemoveOption)Convert.ToInt32(useStacksProperty.boolValue));
            removeOption.showMixedValue = useStacksProperty.hasMultipleDifferentValues;
            removeOption.RegisterValueChangedCallback(RemoveOptionValueChanged);

            actionConfigurable.RegisterValueChangeCallback(ActionConfigurableValueChanged);

            timing.RegisterValueChangeCallback(TimingValueChanged);

            EvaluateProperties();

            return root;

            void SearchableConfigurableValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
            }

            void ExistenceValueChanged(ChangeEvent<Enum> changeEvent)
            {
                isPresentProperty.boolValue = Convert.ToBoolean((int)(Existence)changeEvent.newValue);
                isPresentProperty.serializedObject.ApplyModifiedProperties();
            }

            void ConfigurabilityValueChanged(ChangeEvent<Enum> changeEvent)
            {
                isAddedProperty.boolValue = Convert.ToBoolean((int)(Configurability)changeEvent.newValue);
                isAddedProperty.serializedObject.ApplyModifiedProperties();
                EvaluateProperties();
            }

            void ScaleOptionValueChanged(ChangeEvent<Enum> changeEvent)
            {
                isScaledProperty.boolValue = Convert.ToBoolean((int)(ScaleOption)changeEvent.newValue);
                isScaledProperty.serializedObject.ApplyModifiedProperties();
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
                bool addDifference = searchableDifference || isAddedProperty.hasMultipleDifferentValues;
                bool useStacksDifference = addDifference || (!isAddedProperty.boolValue && useStacksProperty.hasMultipleDifferentValues);
                bool actionDifference = addDifference || (!isAddedProperty.boolValue && actionConfigurableProperty.hasMultipleDifferentValues);
                bool timingDifference = addDifference || (isAddedProperty.boolValue && timingProperty.hasMultipleDifferentValues);

                bool anyDifference = searchableDifference || addDifference || useStacksDifference || actionDifference;

                searchableData.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!searchableDifference && searchableConfigurableValue is ConditionalConfigurable.Data));
                searchableComparableName.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!searchableDifference && searchableConfigurableValue is ConditionalConfigurable.Name));
                searchableGroup.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!searchableDifference && searchableConfigurableValue is ConditionalConfigurable.AllGroups or ConditionalConfigurable.AnyGroups));
                isLabel.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, searchableDifference);
                isLabel.text = searchableConfigurableValue is ConditionalConfigurable.AllGroups or ConditionalConfigurable.AnyGroups ? "are" : "is";
                existence.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, searchableDifference);
                thenLabel.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, searchableDifference);
                configurability.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, searchableDifference);
                stacks.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!addDifference && !useStacksDifference && (isAddedProperty.boolValue || useStacksProperty.boolValue)));
                scaleOption.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!addDifference && !useStacksDifference && (isAddedProperty.boolValue || useStacksProperty.boolValue)));
                removeOption.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!addDifference && !isAddedProperty.boolValue));
                stacksLabel.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!addDifference && isAddedProperty.boolValue));
                ofLabel.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!addDifference && !useStacksDifference));
                actionConfigurable.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!useStacksDifference && !isAddedProperty.boolValue));
                actionData.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!addDifference && !actionDifference && (isAddedProperty.boolValue || actionConfigurableValue is ConditionalConfigurable.Data)));
                actionComparableName.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!addDifference && !actionDifference && !isAddedProperty.boolValue && actionConfigurableValue is ConditionalConfigurable.Name));
                actionGroup.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!addDifference && !actionDifference && !isAddedProperty.boolValue && actionConfigurableValue is ConditionalConfigurable.AllGroups or ConditionalConfigurable.AnyGroups));
                duration.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!timingDifference && isAddedProperty.boolValue && conditionalTiming is ConditionalTiming.Duration));
                timing.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName,
                    !(!addDifference && isAddedProperty.boolValue));
                dashLabel.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !anyDifference);
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
