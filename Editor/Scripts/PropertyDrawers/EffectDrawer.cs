#if UNITY_2023_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#else
using UnityEngine;
#endif
using System;
using UnityEditor;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(Effect))]
    internal class EffectDrawer : PropertyDrawer
    {
#if UNITY_2023_1_OR_NEWER
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
#else
        private SerializedProperty m_StatusName;
        private SerializedProperty m_UseBaseValue;
        private SerializedProperty m_Primary;
        private SerializedProperty m_Secondary;
        private SerializedProperty m_Tertiary;

        private StatusName m_StatusNameReference;
        private Type m_StatusNameType;
        private Type m_StatusNameTypeDummy;
        private GUIStyle m_Style;

        private readonly float m_FieldSize = EditorGUIUtility.singleLineHeight;
        private readonly float m_Padding = EditorGUIUtility.standardVerticalSpacing;
        private const float k_HorizontalPadding = 3;
        private const int k_FieldCount = 3;
        private const int k_ToggleSize = 15;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_Style = GUI.skin.label;
            m_Style.alignment = TextAnchor.MiddleCenter;

            int multiObjectCount = property.serializedObject.targetObjects.Length;
            bool typeDifference = false;

            m_StatusName = property.FindPropertyRelative($"m_{nameof(Effect.StatusName)}");
            m_UseBaseValue = property.FindPropertyRelative($"m_{nameof(Effect.UseBaseValue)}");

            for (int i = 0; i < multiObjectCount; i++)
            {
                m_StatusNameReference = (m_StatusName.GetParent(property.serializedObject.targetObjects[i]) as Effect).StatusName;
                m_StatusNameTypeDummy = m_StatusNameReference is StatusNameBool ? typeof(StatusNameBool)
                                      : m_StatusNameReference is StatusNameInt  ? typeof(StatusNameInt)
                                                                                : typeof(StatusNameFloat);

                if (i > 0 && m_StatusNameTypeDummy != m_StatusNameType)
                {
                    typeDifference = true;
                    break;
                }

                m_StatusNameType = m_StatusNameTypeDummy;
            }

            m_Primary = m_StatusNameType == typeof(StatusNameBool) ? property.FindPropertyRelative($"m_{nameof(Effect.BoolValue)}")
                      : m_StatusNameType == typeof(StatusNameInt)  ? property.FindPropertyRelative($"m_{nameof(Effect.IntValue)}")
                                                                   : property.FindPropertyRelative($"m_{nameof(Effect.FloatValue)}");

            m_Secondary = m_StatusNameType == typeof(StatusNameBool) ? property.FindPropertyRelative($"m_{nameof(Effect.Priority)}")
                                                                     : property.FindPropertyRelative($"m_{nameof(Effect.ValueModifier)}");

            m_Tertiary = m_StatusNameType == typeof(StatusNameBool) || (m_Secondary.enumValueFlag & (int)(ValueModifier.Overwrite | ValueModifier.Minimum | ValueModifier.Maximum)) == 0 ? null
                                                                    : property.FindPropertyRelative($"m_{nameof(Effect.Priority)}");

            EditorGUI.BeginProperty(position, label, property);

            position.height = m_FieldSize;
            position.y += m_Padding;

            EditorGUI.PropertyField(position, m_StatusName, new GUIContent(m_StatusName.displayName));
            position.y += m_FieldSize + m_Padding;


            var color = GUI.color;

            if (typeDifference)
            {
                GUI.color = Color.yellow;
                EditorGUI.LabelField(position, "Cannot display information due", m_Style);
            }
            else
            {
                EditorGUI.PropertyField(position, m_Secondary, new GUIContent(m_Secondary.displayName));
            }
            position.y += m_FieldSize + m_Padding;

            if (m_Tertiary != null && !typeDifference)
            {
                EditorGUI.PropertyField(position, m_Tertiary, new GUIContent(m_Tertiary.displayName));
                position.y += m_FieldSize + m_Padding;
            }

            if (typeDifference)
            {
                EditorGUI.LabelField(position, "to Status Name type difference.", m_Style);
            }
            else
            {
                var propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(m_Primary.displayName));
                var offset = new Rect(propertyPosition.x, propertyPosition.y, k_ToggleSize, propertyPosition.height);
                EditorGUI.PropertyField(offset, m_UseBaseValue, GUIContent.none);

                offset = new Rect(propertyPosition.x + k_ToggleSize + k_HorizontalPadding, propertyPosition.y, propertyPosition.width - k_ToggleSize - k_HorizontalPadding, propertyPosition.height);
                if (!m_UseBaseValue.boolValue)
                    EditorGUI.PropertyField(offset, m_Primary, GUIContent.none);
                else
                    EditorGUI.LabelField(offset, "Using Base Value");
            }

            GUI.color = color;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int multiObjectCount = property.serializedObject.targetObjects.Length;
            var typeDifference = false;

            m_StatusName = property.FindPropertyRelative($"m_{nameof(Effect.StatusName)}");

            for (int i = 0; i < multiObjectCount; i++)
            {
                m_StatusNameReference = (m_StatusName.GetParent(property.serializedObject.targetObjects[i]) as Effect).StatusName;
                m_StatusNameTypeDummy = m_StatusNameReference is StatusNameBool ? typeof(StatusNameBool)
                                      : m_StatusNameReference is StatusNameInt  ? typeof(StatusNameInt)
                                                                                : typeof(StatusNameFloat);

                if (i > 0 && m_StatusNameTypeDummy != m_StatusNameType)
                {
                    typeDifference = true;
                    break;
                }

                m_StatusNameType = m_StatusNameTypeDummy;
            }

            bool extraField = !typeDifference && m_StatusNameType != typeof(StatusNameBool) && ((ValueModifier)property.FindPropertyRelative($"m_{nameof(Effect.ValueModifier)}").enumValueFlag & (ValueModifier.Overwrite | ValueModifier.Minimum | ValueModifier.Maximum)) != 0;

            return (m_FieldSize + m_Padding) * (k_FieldCount + (extraField ? 1 : 0)) + m_Padding;
        }
#endif
    }
}
