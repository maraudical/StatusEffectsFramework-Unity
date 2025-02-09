using System;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusFloat))]
    [CustomPropertyDrawer(typeof(StatusInt))]
    [CustomPropertyDrawer(typeof(StatusBool))]
    public class StatusVariableDrawer : PropertyDrawer
    {
        private readonly float m_FieldSize = EditorGUIUtility.singleLineHeight;
        private readonly float m_Padding = EditorGUIUtility.standardVerticalSpacing;
        private const float k_TopFix = 0.035f;
        private const float k_HorizontalPadding = 3;
        private const int k_FieldCount = 4;
        private const int k_ToggleSize = 15;
        private const int k_SignSize = 20;

        private const string k_SignProtectedTooltip =
        "Toggles whether the value of this variable should limit " +
        "itself to being positive or negative. If the base value is " +
        "positive, the value will be prevented from going below 0. " +
        "If the base value is negative, the value will be prevented " +
        "from going above 0.";

        private SerializedProperty m_StatusName;
        private SerializedProperty m_BaseValue;
        private SerializedProperty m_SignProtected;
        private SerializedProperty m_Value;

        private Rect m_PropertyPosition;
        private Rect m_Offset;

        private int m_Indent;
        private bool m_Sign;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_StatusName = property.FindPropertyRelative($"m_{nameof(StatusFloat.StatusName)}");
            m_BaseValue = property.FindPropertyRelative($"m_{nameof(StatusFloat.BaseValue)}");
            m_SignProtected = property.FindPropertyRelative($"m_{nameof(StatusFloat.SignProtected)}");
            m_Value = property.FindPropertyRelative($"m_{nameof(StatusFloat.Value)}");

            position.height = m_FieldSize;
            position.y -= k_TopFix;
            
            GUI.color = !m_StatusName.objectReferenceValue && !property.isExpanded ? Color.red : Color.white;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            GUI.color = Color.white;

            EditorGUI.BeginProperty(position, label, property);

            m_Indent = EditorGUI.indentLevel;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel = m_Indent + 1;
                position.y += m_FieldSize + m_Padding;
                GUI.color = !m_StatusName.objectReferenceValue ? Color.red : Color.white;
                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                EditorGUI.PropertyField(position, m_StatusName);
                EditorGUI.EndDisabledGroup();
                GUI.color = Color.white;
                position.y += m_FieldSize + m_Padding;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, m_BaseValue);
                if (EditorGUI.EndChangeCheck() && Application.isPlaying)
                {
                    MethodInfo baseValueUpdate = property.GetPropertyType().GetMethod("BaseValueUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var statusVariable in property.serializedObject.targetObjects)
                        baseValueUpdate.Invoke(m_Value.GetParent(statusVariable), null);
                }
                position.y += m_FieldSize + m_Padding;

                m_PropertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(m_Value.displayName));
                EditorGUI.indentLevel = m_Indent;
                if (m_SignProtected != null && !m_SignProtected.Equals(null))
                {
                    m_Offset = new Rect(m_PropertyPosition.x, m_PropertyPosition.y, k_ToggleSize, m_PropertyPosition.height);
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(m_Offset, m_SignProtected, GUIContent.none);
                    GUI.Label(m_Offset, new GUIContent("", k_SignProtectedTooltip));
                    if (EditorGUI.EndChangeCheck() && Application.isPlaying)
                    {
                        MethodInfo signProtectedUpdate = property.GetPropertyType().GetMethod("SignProtectedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var statusVariable in property.serializedObject.targetObjects)
                            signProtectedUpdate.Invoke(m_Value.GetParent(statusVariable), null);
                    }
                    if (m_SignProtected.boolValue)
                    {
                        m_Offset = new Rect(m_PropertyPosition.x - k_SignSize, m_PropertyPosition.y, k_SignSize, m_PropertyPosition.height);
                        m_Sign = Convert.ToInt32(m_BaseValue.GetParent(m_BaseValue.serializedObject.targetObject).GetValue($"m_{nameof(StatusFloat.BaseValue)}")) >= 0;
                        GUI.color = m_SignProtected.hasMultipleDifferentValues || m_BaseValue.hasMultipleDifferentValues ? Color.white : m_Sign ? Color.green : Color.red;
                        EditorGUI.LabelField(m_Offset, $"({(m_SignProtected.hasMultipleDifferentValues || m_BaseValue.hasMultipleDifferentValues ? "?" : m_Sign ? "+" : "-")})");
                        GUI.color = Color.white;
                    }
                    m_Offset = new Rect(m_PropertyPosition.x + k_ToggleSize + k_HorizontalPadding, m_PropertyPosition.y, m_PropertyPosition.width - k_ToggleSize - k_HorizontalPadding, m_PropertyPosition.height);
                }
                else
                    m_Offset = new Rect(m_PropertyPosition.x, m_PropertyPosition.y, m_PropertyPosition.width, m_PropertyPosition.height);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(m_Offset, Application.isPlaying ? m_Value : m_BaseValue, GUIContent.none);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                m_PropertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(" "));
                EditorGUI.PropertyField(m_PropertyPosition, Application.isPlaying ? m_Value : m_BaseValue, GUIContent.none);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.indentLevel = m_Indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (m_FieldSize + m_Padding) * (property.isExpanded ? k_FieldCount : 1) - m_Padding;
        }
    }
}