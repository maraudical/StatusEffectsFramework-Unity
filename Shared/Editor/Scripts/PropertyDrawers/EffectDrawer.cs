using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using UnityEditor;
using UnityEngine;

namespace StatusEffectFramework.Editor
{
    [CustomPropertyDrawer(typeof(Effect))]
    internal class EffectDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var statusNameProperty = property.FindPropertyRelative($"m_{nameof(Effect.StatusName)}");
            var valueModifierProperty = property.FindPropertyRelative($"m_{nameof(Effect.ValueModifier)}");
            var priorityProperty = property.FindPropertyRelative($"m_{nameof(Effect.Priority)}");
            var valueSourceProperty = property.FindPropertyRelative($"m_{nameof(Effect.ValueSource)}");
            var floatProperty = property.FindPropertyRelative($"m_{nameof(Effect.FloatValue)}");
            var intProperty = property.FindPropertyRelative($"m_{nameof(Effect.IntValue)}");
            var boolProperty = property.FindPropertyRelative($"m_{nameof(Effect.BoolValue)}");
            var dynamicFloatProperty = property.FindPropertyRelative($"m_{nameof(Effect.DynamicFloatEffect)}");
            var dynamicIntProperty = property.FindPropertyRelative($"m_{nameof(Effect.DynamicIntEffect)}");
            var dynamicBoolProperty = property.FindPropertyRelative($"m_{nameof(Effect.DynamicBoolEffect)}");

            StatusName statusNameReference;
            Type statusNameType;
            Type statusNameTypeDummy;

            Label statusNameLabel = default;

            var root = new VisualElement();

            var statusName = new PropertyField(statusNameProperty);
            root.Add(statusName);

            var typeDifferenceContainer = new VisualElement();
            typeDifferenceContainer.style.flexGrow = 1;
            typeDifferenceContainer.style.flexShrink = 1;
            root.Add(typeDifferenceContainer);

            var valueModifier = new PropertyField(valueModifierProperty);
            typeDifferenceContainer.Add(valueModifier);

            var priority = new PropertyField(priorityProperty);
            typeDifferenceContainer.Add(priority);

            var valueContainer = new VisualElement();
            valueContainer.style.flexGrow = 1;
            valueContainer.style.flexShrink = 1;
            typeDifferenceContainer.Add(valueContainer);

            var usingBaseValueContainer = new TextField();
            usingBaseValueContainer.label = " ";
            usingBaseValueContainer.value = "Using Base Value";
            usingBaseValueContainer.focusable = false;
            usingBaseValueContainer.SetEnabled(false);
            usingBaseValueContainer.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            valueContainer.Add(usingBaseValueContainer);

            var explicitValueContainer = new VisualElement();
            explicitValueContainer.style.flexGrow = 1;
            explicitValueContainer.style.flexShrink = 1;
            valueContainer.Add(explicitValueContainer);

            var floatValue = new PropertyField(floatProperty, " ");
            explicitValueContainer.Add(floatValue);

            var intValue = new PropertyField(intProperty, " ");
            explicitValueContainer.Add(intValue);

            var boolValue = new PropertyField(boolProperty, " ");
            explicitValueContainer.Add(boolValue);

            var dynamicValueContainer = new VisualElement();
            dynamicValueContainer.style.flexGrow = 1;
            dynamicValueContainer.style.flexShrink = 1;
            valueContainer.Add(dynamicValueContainer);

            var dynamicFloatValue = new PropertyField(dynamicFloatProperty, " ");
            dynamicValueContainer.Add(dynamicFloatValue);

            var dynamicIntValue = new PropertyField(dynamicIntProperty, " ");
            dynamicValueContainer.Add(dynamicIntValue);

            var dynamicBoolValue = new PropertyField(dynamicBoolProperty, " ");
            dynamicValueContainer.Add(dynamicBoolValue);

            var valueSource = new PropertyField(valueSourceProperty, string.Empty);
            valueSource.style.position = Position.Absolute;
            valueSource.style.marginLeft = -3;
            valueSource.style.minWidth = 83;
            valueContainer.Add(valueSource);

            StatusNameChanged(default);

            statusName.RegisterValueChangeCallback(StatusNameChanged);

            valueModifier.RegisterValueChangeCallback(ValueModifierChanged);

            valueSource.RegisterValueChangeCallback(ValueSourceChanged);

            statusName.RegisterCallback<GeometryChangedEvent>(StatusNameGeometryChanged);

            return root;

            void StatusNameGeometryChanged(GeometryChangedEvent evt)
            {
                if (statusNameLabel == null)
                    statusNameLabel = statusName.Q<Label>();
                valueSource.style.maxWidth = Mathf.Max(statusNameLabel.style.width.value.value + 3, valueSource.style.minWidth.value.value);
            }

            void StatusNameChanged(SerializedPropertyChangeEvent evt)
            {
                bool typeDifference = ValidateStatusNameType();
                typeDifferenceContainer.style.display = !typeDifference ? DisplayStyle.Flex : DisplayStyle.None;
                bool isFloat = statusNameType == typeof(StatusNameFloat);
                bool isInt = statusNameType == typeof(StatusNameInt);
                bool isBool = statusNameType == typeof(StatusNameBool);
                valueModifier.style.display = !isBool ? DisplayStyle.Flex : DisplayStyle.None;
                ValueModifierChanged(default);
                floatValue.style.display = isFloat ? DisplayStyle.Flex : DisplayStyle.None;
                intValue.style.display = isInt ? DisplayStyle.Flex : DisplayStyle.None;
                boolValue.style.display = isBool ? DisplayStyle.Flex : DisplayStyle.None;
                dynamicFloatValue.style.display = isFloat ? DisplayStyle.Flex : DisplayStyle.None;
                dynamicIntValue.style.display = isInt ? DisplayStyle.Flex : DisplayStyle.None;
                dynamicBoolValue.style.display = isBool ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void ValueModifierChanged(SerializedPropertyChangeEvent evt)
            {
                bool isBool = statusNameType == typeof(StatusNameBool);
                bool numberWithPriority = (valueModifierProperty.enumValueFlag & (int)(ValueModifier.Overwrite | ValueModifier.Minimum | ValueModifier.Maximum)) != 0;
                priority.style.display = isBool || numberWithPriority ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void ValueSourceChanged(SerializedPropertyChangeEvent evt)
            {
                switch (evt.changedProperty.enumValueIndex)
                {
                    case (int)ValueSource.BaseValue:
                        usingBaseValueContainer.style.display = DisplayStyle.Flex;
                        explicitValueContainer.style.display = DisplayStyle.None;
                        dynamicValueContainer.style.display = DisplayStyle.None;
                        break;
                    case (int)ValueSource.DynamicValue:
                        usingBaseValueContainer.style.display = DisplayStyle.None;
                        explicitValueContainer.style.display = DisplayStyle.None;
                        dynamicValueContainer.style.display = DisplayStyle.Flex;
                        break;
                    default:
                        usingBaseValueContainer.style.display = DisplayStyle.None;
                        explicitValueContainer.style.display = DisplayStyle.Flex;
                        dynamicValueContainer.style.display = DisplayStyle.None;
                        break;
                }
            }

            bool ValidateStatusNameType()
            {
                int count = property.serializedObject.targetObjects.Length;
                var typeDifference = false;
                statusNameType = null;

                for (int i = 0; i < count; i++)
                {
                    statusNameReference = (statusNameProperty.GetParent(property.serializedObject.targetObjects[i]) as Effect).StatusName;
                    statusNameTypeDummy = statusNameReference is StatusNameBool ? typeof(StatusNameBool)
                                        : statusNameReference is StatusNameInt  ? typeof(StatusNameInt)
                                                                                : typeof(StatusNameFloat);

                    if (i > 0 && statusNameTypeDummy != statusNameType)
                    {
                        typeDifference = true;
                        break;
                    }

                    statusNameType = statusNameTypeDummy;
                }

                return typeDifference;
            }
        }
    }
}
