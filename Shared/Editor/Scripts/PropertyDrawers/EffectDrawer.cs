using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using UnityEditor;

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
            var useBaseValueProperty = property.FindPropertyRelative($"m_{nameof(Effect.UseBaseValue)}");
            var floatProperty = property.FindPropertyRelative($"m_{nameof(Effect.FloatValue)}");
            var intProperty = property.FindPropertyRelative($"m_{nameof(Effect.IntValue)}");
            var boolProperty = property.FindPropertyRelative($"m_{nameof(Effect.BoolValue)}");

            StatusName statusNameReference;
            Type statusNameType;
            Type statusNameTypeDummy;

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
            usingBaseValueContainer.value = "Using Base Value";
            usingBaseValueContainer.focusable = false;
            usingBaseValueContainer.SetEnabled(false);
            usingBaseValueContainer.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            valueContainer.Add(usingBaseValueContainer);

            var valuesContainer = new VisualElement();
            valuesContainer.style.flexGrow = 1;
            valuesContainer.style.flexShrink = 1;
            valueContainer.Add(valuesContainer);

            var floatValue = new PropertyField(floatProperty, "Float Value");
            floatValue.style.flexGrow = 1;
            floatValue.style.flexShrink = 1;
            valuesContainer.Add(floatValue);

            var intValue = new PropertyField(intProperty, "Int Value");
            intValue.style.flexGrow = 1;
            intValue.style.flexShrink = 1;
            valuesContainer.Add(intValue);

            var boolValue = new PropertyField(boolProperty, "Bool Value");
            boolValue.style.flexGrow = 1;
            boolValue.style.flexShrink = 1;
            valuesContainer.Add(boolValue);
            
            var useBaseValue = new PropertyField(useBaseValueProperty, " ");
            useBaseValue.style.position = Position.Absolute;
            useBaseValue.style.left = 0;
            useBaseValue.style.right = 0;
            useBaseValue.style.top = 0;
            useBaseValue.style.bottom = 0;
            useBaseValue.style.flexGrow = 1;
            useBaseValue.style.flexShrink = 1;
            useBaseValue.style.flexDirection = FlexDirection.ColumnReverse;
            valueContainer.Add(useBaseValue);

            statusName.RegisterValueChangeCallback(StatusNameChanged);

            valueModifier.RegisterValueChangeCallback(ValueModifierChanged);

            useBaseValue.RegisterValueChangeCallback(UseBaseValueChanged);
            useBaseValue.RegisterCallback<GeometryChangedEvent>(UseBaseValueGeometryChanged);

            usingBaseValueContainer.RegisterCallback<GeometryChangedEvent>(UsingBaseValueTextGeometryChanged);

            floatValue.RegisterCallback<GeometryChangedEvent>(FloatGeometryChanged);
            
            intValue.RegisterCallback<GeometryChangedEvent>(IntGeometryChanged);
            
            boolValue.RegisterCallback<GeometryChangedEvent>(BoolGeometryChanged);

            StatusNameChanged(default);

            return root;

            void IgnoreExcept(VisualElement root, string exception)
            {
                if (root.name == exception)
                {
                    root.pickingMode = PickingMode.Position;
                    return;
                }

                root.pickingMode = PickingMode.Ignore;

                foreach(var child in root.Children())
                    IgnoreExcept(child, exception);
            }

            void UseBaseValueGeometryChanged(GeometryChangedEvent changeEvent)
            {
                useBaseValue.UnregisterCallback<GeometryChangedEvent>(UseBaseValueGeometryChanged);
                IgnoreExcept(useBaseValue, "unity-checkmark");
            }

            void UsingBaseValueTextGeometryChanged(GeometryChangedEvent changeEvent)
            {
                usingBaseValueContainer.UnregisterCallback<GeometryChangedEvent>(UsingBaseValueTextGeometryChanged);
                var element = usingBaseValueContainer.Q("unity-text-input");
                AdjustElement(element);
            }

            void FloatGeometryChanged(GeometryChangedEvent changeEvent)
            {
                floatValue.UnregisterCallback<GeometryChangedEvent>(FloatGeometryChanged);
                var element = floatValue.Q("unity-text-input");
                AdjustElement(element);
            }

            void IntGeometryChanged(GeometryChangedEvent changeEvent)
            {
                intValue.UnregisterCallback<GeometryChangedEvent>(IntGeometryChanged);
                var element = intValue.Q("unity-text-input");
                AdjustElement(element);
            }

            void BoolGeometryChanged(GeometryChangedEvent changeEvent)
            {
                boolValue.UnregisterCallback<GeometryChangedEvent>(BoolGeometryChanged);
                var element = boolValue.Q("unity-checkmark")?.parent;
                AdjustElement(element);
            }

            void AdjustElement(VisualElement element)
            {
                if (element != null)
                {
                    element.style.marginRight = 18;
                    var styleTranslate = element.style.translate;
                    var translate = styleTranslate.value;
                    translate.x = 18;
                    styleTranslate.value = translate;
                    element.style.translate = styleTranslate;
                }
            }

            void StatusNameChanged(SerializedPropertyChangeEvent changeEvent)
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
                usingBaseValueContainer.label = isBool ? boolValue.label : isInt ? intValue.label : floatValue.label;
            }

            void ValueModifierChanged(SerializedPropertyChangeEvent evt)
            {
                bool typeDifference = ValidateStatusNameType();
                bool isBool = statusNameType == typeof(StatusNameBool);
                bool numberWithPriority = (valueModifierProperty.enumValueFlag & (int)(ValueModifier.Overwrite | ValueModifier.Minimum | ValueModifier.Maximum)) != 0;
                priority.style.display = isBool || numberWithPriority ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void UseBaseValueChanged(SerializedPropertyChangeEvent evt)
            {
                valuesContainer.style.display = useBaseValueProperty.boolValue ? DisplayStyle.None : DisplayStyle.Flex;
                usingBaseValueContainer.style.display = useBaseValueProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
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
