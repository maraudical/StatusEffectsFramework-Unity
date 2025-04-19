using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(Effect))]
    public class EffectDrawer : PropertyDrawer
    {
        public VisualTreeAsset VisualTree;

        

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            StatusName statusNameReference;
            Type statusNameType;
            Type statusNameTypeDummy;

            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var statusName = root.Q<PropertyField>("status-name");
            var typeDifferenceContainer = root.Q("type-difference-container");
            var valueModifier = root.Q<PropertyField>("value-modifier");
            var priority = root.Q<PropertyField>("priority");
            var useBaseValue = root.Q<PropertyField>("use-base-value");
            var usingBaseValueContainer = root.Q<TextField>("using-base-value-container");
            var valuesContainer = root.Q("values-container");
            var floatValue = root.Q<PropertyField>("float-value");
            var intValue = root.Q<PropertyField>("int-value");
            var boolValue = root.Q<PropertyField>("bool-value");

            var statusNameProperty = property.FindPropertyRelative($"m_{nameof(Effect.StatusName)}");
            var useBaseValueProperty = property.FindPropertyRelative($"m_{nameof(Effect.UseBaseValue)}");
            var valueModifierProperty = property.FindPropertyRelative($"m_{nameof(Effect.ValueModifier)}");
            
            statusName.RegisterValueChangeCallback(StatusNameChanged);

            valueModifier.RegisterValueChangeCallback(ValueModifierChanged);

            useBaseValue.RegisterValueChangeCallback(UseBaseValueChanged);
            useBaseValue.RegisterCallbackOnce<GeometryChangedEvent>(UseBaseValueGeometryChanged);

            usingBaseValueContainer.RegisterCallbackOnce<GeometryChangedEvent>(UsingBaseValueTextGeometryChanged);

            floatValue.RegisterCallbackOnce<GeometryChangedEvent>(FloatGeometryChanged);
            
            intValue.RegisterCallbackOnce<GeometryChangedEvent>(IntGeometryChanged);
            
            boolValue.RegisterCallbackOnce<GeometryChangedEvent>(BoolGeometryChanged);

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
                IgnoreExcept(useBaseValue, "unity-checkmark");
            }

            void UsingBaseValueTextGeometryChanged(GeometryChangedEvent changeEvent)
            {
                var element = usingBaseValueContainer.Q("unity-text-input");
                AdjustElement(element);
            }

            void FloatGeometryChanged(GeometryChangedEvent changeEvent)
            {
                var element = floatValue.Q("unity-text-input");
                AdjustElement(element);
            }

            void IntGeometryChanged(GeometryChangedEvent changeEvent)
            {
                var element = intValue.Q("unity-text-input");
                AdjustElement(element);
            }

            void BoolGeometryChanged(GeometryChangedEvent changeEvent)
            {
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
                typeDifferenceContainer.style.display = !typeDifference? DisplayStyle.Flex : DisplayStyle.None;
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
                                          : statusNameReference is StatusNameInt ? typeof(StatusNameInt)
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
