using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace StatusEffectsFramework.Editor
{
    [CustomPropertyDrawer(typeof(Effect))]
    internal class EffectDrawer : PropertyDrawer
    {
        /// <summary>
        /// Main USS class name of the Effect.
        /// </summary>
        public const string ussClassName = "sef-effect";

        /// <summary>
        /// USS class name of the Effect's status name field.
        /// </summary>
        public const string statusNameUssClassName = ussClassName + "__status-name";

        /// <summary>
        /// USS class name of the Effect's value type field.
        /// </summary>
        public const string valueTypeUssClassName = ussClassName + "__value-type";

        /// <summary>
        /// USS class name of the status name's label while the value type field is shown in it.
        /// </summary>
        public const string statusNameLabelWithValueTypeUssClassName = ussClassName + "__status-name-label--value-type";

        /// <summary>
        /// USS class name of the Effect's value modifier field.
        /// </summary>
        public const string valueModifierUssClassName = ussClassName + "__value-modifier";

        /// <summary>
        /// USS class name of the Effect's priority field.
        /// </summary>
        public const string priorityUssClassName = ussClassName + "__priority";

        /// <summary>
        /// USS class name of the Effect's value source field.
        /// </summary>
        public const string valueSourceUssClassName = ussClassName + "__value-source";

        /// <summary>
        /// USS class name of the Effect's type difference container field.
        /// </summary>
        public const string typeDifferenceContainerUssClassName = ussClassName + "__type-difference-container";

        /// <summary>
        /// USS class name of the Effect's value container field.
        /// </summary>
        public const string valueContainerUssClassName = ussClassName + "__value-container";

        /// <summary>
        /// USS class name of the Effect's explicit value container field.
        /// </summary>
        public const string explicitValueContainerUssClassName = ussClassName + "__explicit-value-container";

        /// <summary>
        /// USS class name of the Effect's dynamic value container field.
        /// </summary>
        public const string dynamicValueContainerUssClassName = ussClassName + "__dynamic-value-container";

        /// <summary>
        /// USS class name of the Effect's float value field.
        /// </summary>
        public const string floatValueUssClassName = ussClassName + "__float-value";

        /// <summary>
        /// USS class name of the Effect's int value field.
        /// </summary>
        public const string intValueUssClassName = ussClassName + "__int-value";

        /// <summary>
        /// USS class name of the Effect's bool value field.
        /// </summary>
        public const string boolValueUssClassName = ussClassName + "__bool-value";

        /// <summary>
        /// USS class name of the Effect's dynamic float field.
        /// </summary>
        public const string dynamicFloatUssClassName = ussClassName + "__dynamic-float";

        /// <summary>
        /// USS class name of the Effect's dynamic int field.
        /// </summary>
        public const string dynamicIntUssClassName = ussClassName + "__dynamic-int";

        /// <summary>
        /// USS class name of the Effect's dynamic bool field.
        /// </summary>
        public const string dynamicBoolUssClassName = ussClassName + "__dynamic-bool";

        private const int k_ModifierPriorityFlags = (int)(ValueModifier.Overwrite | ValueModifier.Minimum | ValueModifier.Maximum);

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var statusNameProperty = property.FindPropertyRelative($"m_{nameof(Effect.StatusName)}");
            var statusNameAssetProperty = statusNameProperty.FindPropertyRelative(RegistrantKeyDrawer.AssetPropertyName);
            var valueTypeProperty = property.FindPropertyRelative($"m_{nameof(Effect.ValueType)}");
            var valueModifierProperty = property.FindPropertyRelative($"m_{nameof(Effect.ValueModifier)}");
            var priorityProperty = property.FindPropertyRelative($"m_{nameof(Effect.Priority)}");
            var valueSourceProperty = property.FindPropertyRelative($"m_{nameof(Effect.ValueSource)}");
            var floatProperty = property.FindPropertyRelative($"m_{nameof(Effect.FloatValue)}");
            var intProperty = property.FindPropertyRelative($"m_{nameof(Effect.IntValue)}");
            var boolProperty = property.FindPropertyRelative($"m_{nameof(Effect.BoolValue)}");
            var dynamicFloatProperty = property.FindPropertyRelative($"m_{nameof(Effect.DynamicFloat)}");
            var dynamicIntProperty = property.FindPropertyRelative($"m_{nameof(Effect.DynamicInt)}");
            var dynamicBoolProperty = property.FindPropertyRelative($"m_{nameof(Effect.DynamicBool)}");

            // Set initial value, this will be ovewritten when checking for the StatusName type.
            ValueType statusNameType = ValueType.Float;

            Label statusNameLabel = default;

            var root = new VisualElement
            {
                name = ussClassName
            };
            root.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
            root.AddToClassList(ussClassName);

            var statusName = new PropertyField(statusNameProperty)
            {
                name = statusNameUssClassName
            };
            statusName.AddToClassList(statusNameUssClassName);
            root.Add(statusName);

            // Only shown while the status name is a typed key. It is moved into the status name's label once the
            // label exists, so it sits on the same line without pushing the key field out of line with the others.
            var valueType = new PropertyField(valueTypeProperty, string.Empty)
            {
                name = valueTypeUssClassName
            };
            valueType.AddToClassList(valueTypeUssClassName);
            valueType.BindProperty(valueTypeProperty);

            // The type of the last assigned asset, so clearing the asset to type the key keeps the same type.
            ValueType? assetValueType = GetAssetValueType();

            var typeDifferenceContainer = new VisualElement
            {
                name = typeDifferenceContainerUssClassName
            };
            typeDifferenceContainer.AddToClassList(typeDifferenceContainerUssClassName);
            root.Add(typeDifferenceContainer);

            var valueModifier = new PropertyField(valueModifierProperty)
            {
                name = valueModifierUssClassName
            };
            valueModifier.AddToClassList(valueModifierUssClassName);
            typeDifferenceContainer.Add(valueModifier);

            var priority = new PropertyField(priorityProperty)
            {
                name = priorityUssClassName
            };
            priority.AddToClassList(priorityUssClassName);
            typeDifferenceContainer.Add(priority);

            var valueContainer = new VisualElement
            {
                name = valueContainerUssClassName
            };
            valueContainer.AddToClassList(valueContainerUssClassName);
            typeDifferenceContainer.Add(valueContainer);

            var usingBaseValueContainer = new TextField
            {
                label = StatusEffectsStyleSheet.EmptyPropertyLabel,
                value = "Using Base Value",
                focusable = false
            };
            usingBaseValueContainer.SetEnabled(false);
            usingBaseValueContainer.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            valueContainer.Add(usingBaseValueContainer);

            var explicitValueContainer = new VisualElement
            {
                name = explicitValueContainerUssClassName
            };
            explicitValueContainer.AddToClassList(explicitValueContainerUssClassName);
            valueContainer.Add(explicitValueContainer);

            var floatValue = new PropertyField(floatProperty, StatusEffectsStyleSheet.EmptyPropertyLabel)
            {
                name = floatValueUssClassName
            };
            floatValue.AddToClassList(floatValueUssClassName);
            explicitValueContainer.Add(floatValue);

            var intValue = new PropertyField(intProperty, StatusEffectsStyleSheet.EmptyPropertyLabel)
            {
                name = intValueUssClassName
            };
            intValue.AddToClassList(intValueUssClassName);
            explicitValueContainer.Add(intValue);

            var boolValue = new PropertyField(boolProperty, StatusEffectsStyleSheet.EmptyPropertyLabel)
            {
                name = boolValueUssClassName
            };
            boolValue.AddToClassList(boolValueUssClassName);
            explicitValueContainer.Add(boolValue);

            var dynamicValueContainer = new VisualElement
            {
                name = dynamicValueContainerUssClassName
            };
            dynamicValueContainer.AddToClassList(dynamicValueContainerUssClassName);
            valueContainer.Add(dynamicValueContainer);

            var dynamicFloatValue = new PropertyField(dynamicFloatProperty, StatusEffectsStyleSheet.EmptyPropertyLabel)
            {
                name = dynamicFloatUssClassName
            };
            dynamicFloatValue.AddToClassList(dynamicFloatUssClassName);
            dynamicValueContainer.Add(dynamicFloatValue);

            var dynamicIntValue = new PropertyField(dynamicIntProperty, StatusEffectsStyleSheet.EmptyPropertyLabel)
            {
                name = dynamicIntUssClassName
            };
            dynamicIntValue.AddToClassList(dynamicIntUssClassName);
            dynamicValueContainer.Add(dynamicIntValue);

            var dynamicBoolValue = new PropertyField(dynamicBoolProperty, StatusEffectsStyleSheet.EmptyPropertyLabel)
            {
                name = dynamicBoolUssClassName
            };
            dynamicBoolValue.AddToClassList(dynamicBoolUssClassName);
            dynamicValueContainer.Add(dynamicBoolValue);

            var valueSource = new PropertyField(valueSourceProperty, string.Empty)
            {
                name = valueSourceUssClassName
            };
            valueSource.AddToClassList(valueSourceUssClassName);
            valueContainer.Add(valueSource);

            StatusNameChanged(default);

            // The status name is picked or typed inside its own drawer, so its property is tracked directly.
            root.TrackPropertyValue(statusNameProperty, _ =>
            {
                SyncValueType();
                StatusNameChanged(default);
            });
            root.TrackPropertyValue(valueTypeProperty, _ => StatusNameChanged(default));

            valueModifier.RegisterValueChangeCallback(ValueModifierChanged);

            valueSource.RegisterValueChangeCallback(ValueSourceChanged);

            statusName.RegisterCallback<GeometryChangedEvent>(StatusNameGeometryChanged);

            return root;

            void StatusNameGeometryChanged(GeometryChangedEvent evt)
            {
                // The status name's drawer builds its label when it is bound, and builds it again if it is rebound.
                var label = statusName.Q<Label>(className: BaseField<UnityEngine.Object>.labelUssClassName);
                if (label == null)
                    return;

                if (label != statusNameLabel)
                {
                    statusNameLabel = label;
                    label.Add(valueType);
                    UpdateValueTypeVisibility();
                }

                // Sized from the label's resolved width, so this stays inline rather than in the style sheet.
                valueSource.style.maxWidth = Mathf.Max(statusNameLabel.resolvedStyle.width + 3, valueSource.resolvedStyle.minWidth.value);
            }

            ValueType? GetAssetValueType()
            {
                return !statusNameAssetProperty.hasMultipleDifferentValues && statusNameAssetProperty.objectReferenceValue is StatusName asset
                     ? asset.ValueType
                     : null;
            }

            // The stored type follows the asset. Builds read it, and it is what shows once the asset is cleared.
            void SyncValueType()
            {
                var currentAssetValueType = GetAssetValueType();
                var syncedValueType = currentAssetValueType ?? assetValueType;
                assetValueType = currentAssetValueType;

                if (syncedValueType is not { } newValueType
                    || (!valueTypeProperty.hasMultipleDifferentValues && valueTypeProperty.enumValueIndex == (int)newValueType))
                    return;

                valueTypeProperty.enumValueIndex = (int)newValueType;
                valueTypeProperty.serializedObject.ApplyModifiedProperties();
            }

            void UpdateValueTypeVisibility()
            {
                bool isTypedKey = !statusNameAssetProperty.hasMultipleDifferentValues && statusNameAssetProperty.objectReferenceValue == null;
                valueType.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !isTypedKey);
                statusNameLabel?.EnableInClassList(statusNameLabelWithValueTypeUssClassName, isTypedKey);
            }

            void StatusNameChanged(SerializedPropertyChangeEvent evt)
            {
                UpdateValueTypeVisibility();
                bool typeDifference = ValidateStatusNameType();
                typeDifferenceContainer.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, typeDifference);
                bool isFloat = statusNameType is ValueType.Float;
                bool isInt = statusNameType is ValueType.Int;
                bool isBool = statusNameType is ValueType.Bool;
                valueModifier.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, isBool);
                ValueModifierChanged(default);
                floatValue.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !isFloat);
                intValue.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !isInt);
                boolValue.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !isBool);
                dynamicFloatValue.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !isFloat);
                dynamicIntValue.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !isInt);
                dynamicBoolValue.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !isBool);
            }

            void ValueModifierChanged(SerializedPropertyChangeEvent evt)
            {
                priority.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, 
                    statusNameType is not ValueType.Bool && (valueModifierProperty.enumValueFlag & k_ModifierPriorityFlags) == 0);
            }

            void ValueSourceChanged(SerializedPropertyChangeEvent evt)
            {
                switch (evt.changedProperty.enumValueIndex)
                {
                    case (int)ValueSource.BaseValue:
                        usingBaseValueContainer.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, false);
                        explicitValueContainer.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, true);
                        dynamicValueContainer.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, true);
                        break;
                    case (int)ValueSource.DynamicValue:
                        usingBaseValueContainer.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, true);
                        explicitValueContainer.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, true);
                        dynamicValueContainer.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, false);
                        break;
                    default:
                        usingBaseValueContainer.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, true);
                        explicitValueContainer.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, false);
                        dynamicValueContainer.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, true);
                        break;
                }
            }

            bool ValidateStatusNameType()
            {
                int count = property.serializedObject.targetObjects.Length;
                var typeDifference = false;

                for (int i = 0; i < count; i++)
                {
                    // The effect reads the type from its status name asset when there is one.
                    var newStatusNameType = (statusNameProperty.GetParent(property.serializedObject.targetObjects[i]) as Effect).ValueType;

                    if (i > 0 && newStatusNameType != statusNameType)
                    {
                        typeDifference = true;
                        break;
                    }

                    statusNameType = newStatusNameType;
                }

                return typeDifference;
            }
        }
    }
}
