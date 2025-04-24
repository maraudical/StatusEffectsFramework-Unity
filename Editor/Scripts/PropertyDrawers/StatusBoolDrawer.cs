using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusBool))]
    internal class StatusBoolDrawer : PropertyDrawer
    {
        public VisualTreeAsset VisualTree;

        private MethodInfo m_MethodInfo;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var foldout = root.Q<Foldout>("foldout");
            var unityCheckmark = root.Q("unity-checkmark");
            var errorIcon = root.Q("error-icon");
            var headerProperty = root.Q<PropertyField>("header-property");
            var statusName = root.Q<PropertyField>("status-name");
            var baseValue = root.Q<PropertyField>("base-value");
            var valueLabel = root.Q<Label>("value-label");
            var value = root.Q<PropertyField>("value");

            var statusNameProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.StatusName)}");
            var baseValueProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.BaseValue)}");
            var valueProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.Value)}");

            foldout.text = property.displayName;
            foldout.viewDataKey = property.propertyPath + "-foldout";
            foldout.RegisterValueChangedCallback(FoldoutChanged);

            bool isPlaying = EditorApplication.isPlaying;

            headerProperty.SetEnabled(!isPlaying);

            statusName.SetEnabled(!isPlaying);
            statusName.RegisterValueChangeCallback(StatusNameChanged);

            if (isPlaying)
                baseValue.RegisterValueChangeCallback(BaseValueChanged);

            valueLabel.text = valueProperty.displayName;
            valueLabel.style.unityFontStyleAndWeight = isPlaying ? FontStyle.Bold : FontStyle.Normal;

            value.BindProperty(isPlaying ? valueProperty : baseValueProperty);
            value.RegisterCallbackOnce<GeometryChangedEvent>(GeometryChanged);

            return root;

            void GeometryChanged(GeometryChangedEvent changeEvent)
            {
                EvaluateProperties();
            }

            void FoldoutChanged(ChangeEvent<bool> changeEvent)
            {
                EvaluateProperties();
            }

            void StatusNameChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
            }

            void BaseValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                m_MethodInfo = property.GetPropertyType().GetMethod("BaseValueUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var statusVariable in property.serializedObject.targetObjects)
                    m_MethodInfo.Invoke(valueProperty.GetParent(statusVariable), null);
            }

            void EvaluateProperties()
            {
                bool isNull = statusNameProperty.objectReferenceValue == null;

                if (isNull)
                {
                    if (foldout.value)
                    {
                        unityCheckmark.RemoveFromClassList("error-icon");
                        headerProperty.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        unityCheckmark.AddToClassList("error-icon");
                        headerProperty.style.display = DisplayStyle.Flex;
                    }

                    foldout.RemoveFromClassList("standard-field-size");

                    headerProperty.BindProperty(statusNameProperty);

                    errorIcon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    if (foldout.value)
                    {
                        foldout.RemoveFromClassList("standard-field-size");
                        headerProperty.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        foldout.AddToClassList("standard-field-size");
                        headerProperty.style.display = DisplayStyle.Flex;
                    }

                    unityCheckmark.RemoveFromClassList("error-icon");

                    if (EditorApplication.isPlaying)
                        headerProperty.BindProperty(valueProperty);
                    else
                        headerProperty.BindProperty(baseValueProperty);

                    errorIcon.style.display = DisplayStyle.None;
                }
            }
        }

        private SerializedProperty m_StatusName;
        private SerializedProperty m_BaseValue;
        private SerializedProperty m_Value;

        private readonly float m_FieldSize = EditorGUIUtility.singleLineHeight;
        private readonly float m_Padding = EditorGUIUtility.standardVerticalSpacing;
        private const float k_TopFix = 0.035f;
        private const int k_FieldCount = 4;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_StatusName = property.FindPropertyRelative($"m_{nameof(StatusFloat.StatusName)}");
            m_BaseValue = property.FindPropertyRelative($"m_{nameof(StatusFloat.BaseValue)}");
            m_Value = property.FindPropertyRelative($"m_{nameof(StatusFloat.Value)}");

            position.height = m_FieldSize;
            position.y -= k_TopFix;
            float width = position.width;
            position.width = property.isExpanded ? width : EditorGUIUtility.labelWidth;

            GUI.color = !m_StatusName.objectReferenceValue && !property.isExpanded ? Color.red : Color.white;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            GUI.color = Color.white;

            position.width = width;

            EditorGUI.BeginProperty(position, label, property);

            int indent = EditorGUI.indentLevel;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel = indent + 1;
                position.y += m_FieldSize + m_Padding;
                GUI.color = !m_StatusName.objectReferenceValue ? Color.red : Color.white;
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                EditorGUI.PropertyField(position, m_StatusName);
                EditorGUI.EndDisabledGroup();
                GUI.color = Color.white;
                position.y += m_FieldSize + m_Padding;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, m_BaseValue);
                if (EditorGUI.EndChangeCheck() && EditorApplication.isPlaying)
                {
                    MethodInfo baseValueUpdate = property.GetPropertyType().GetMethod("BaseValueUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var statusVariable in property.serializedObject.targetObjects)
                        baseValueUpdate.Invoke(m_Value.GetParent(statusVariable), null);
                }
                position.y += m_FieldSize + m_Padding;

                Rect propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(m_Value.displayName));
                EditorGUI.indentLevel = indent;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(propertyPosition, EditorApplication.isPlaying ? m_Value : m_BaseValue, GUIContent.none);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                Rect propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(" "));
                EditorGUI.PropertyField(propertyPosition, EditorApplication.isPlaying ? m_Value : m_BaseValue, GUIContent.none);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (m_FieldSize + m_Padding) * (property.isExpanded ? k_FieldCount : 1) - m_Padding;
        }
    }
}