using System;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(Effect))]
    public class EffectDrawer : PropertyDrawer
    {
        private SerializedProperty m_StatusName;
        private SerializedProperty m_UseBaseValue;
        private SerializedProperty m_Primary;
        private SerializedProperty m_Secondary;
        private SerializedProperty m_Tertiary;

        private Rect m_PropertyPosition;
        private Rect m_Offset;

        private StatusName m_StatusNameReference;
        private Type m_StatusNameType;
        private Type m_StatusNameTypeDummy;
        private int m_MultiObjectCount;
        private bool m_TypeDifference;
        private GUIStyle m_Style;
        private Color m_Color;

        private readonly float m_FieldSize = EditorGUIUtility.singleLineHeight;
        private readonly float m_Padding = EditorGUIUtility.standardVerticalSpacing;
        private const float k_HorizontalPadding = 3;
        private const int k_FieldCount = 3;
        private const int k_ToggleSize = 15;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_Style = GUI.skin.label;
            m_Style.alignment = TextAnchor.MiddleCenter;

            m_MultiObjectCount = property.serializedObject.targetObjects.Length;
            m_TypeDifference = false;

            m_StatusName = property.FindPropertyRelative(nameof(Effect.StatusName));
            m_UseBaseValue = property.FindPropertyRelative(nameof(Effect.UseBaseValue));

            for (int i = 0; i < m_MultiObjectCount; i++)
            {
                m_StatusNameReference = (m_StatusName.GetParent(property.serializedObject.targetObjects[i]) as Effect).StatusName;
                m_StatusNameTypeDummy = m_StatusNameReference is StatusNameBool ? typeof(StatusNameBool)
                                      : m_StatusNameReference is StatusNameInt  ? typeof(StatusNameInt)
                                                                                : typeof(StatusNameFloat);

                if (i > 0 && m_StatusNameTypeDummy != m_StatusNameType)
                {
                    m_TypeDifference = true;
                    break;
                }
                
                m_StatusNameType = m_StatusNameTypeDummy;
            }

            m_Primary = m_StatusNameType == typeof(StatusNameBool) ? property.FindPropertyRelative(nameof(Effect.BoolValue))
                      : m_StatusNameType == typeof(StatusNameInt)  ? property.FindPropertyRelative(nameof(Effect.IntValue))
                                                                   : property.FindPropertyRelative(nameof(Effect.FloatValue));

            m_Secondary = m_StatusNameType == typeof(StatusNameBool) ? property.FindPropertyRelative(nameof(Effect.Priority))
                                                                     : property.FindPropertyRelative(nameof(Effect.ValueModifier));
            
            m_Tertiary = m_StatusNameType == typeof(StatusNameBool) ||(m_Secondary.enumValueFlag & (int)(ValueModifier.Overwrite | ValueModifier.Minimum | ValueModifier.Maximum)) == 0 ? null
                                                                    : property.FindPropertyRelative(nameof(Effect.Priority));

            EditorGUI.BeginProperty(position, label, property);

            position.height = m_FieldSize;
            position.y += m_Padding;

            EditorGUI.PropertyField(position, m_StatusName, new GUIContent(m_StatusName.displayName));
            position.y += m_FieldSize + m_Padding;

            if (m_TypeDifference)
            {
                m_Color = GUI.color;
                GUI.color = Color.yellow;
                EditorGUI.LabelField(position, "Cannot display information due", m_Style);
            }
            else
            {
                EditorGUI.PropertyField(position, m_Secondary, new GUIContent(m_Secondary.displayName));
            }
            position.y += m_FieldSize + m_Padding;

            if (m_Tertiary != null && !m_TypeDifference)
            {
                EditorGUI.PropertyField(position, m_Tertiary, new GUIContent(m_Tertiary.displayName));
                position.y += m_FieldSize + m_Padding;
            }

            if (m_TypeDifference)
            {
                EditorGUI.LabelField(position, "to Status Name type difference.", m_Style);
                GUI.color = m_Color;
            }
            else
            {
                m_PropertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(m_Primary.displayName));
                m_Offset = new Rect(m_PropertyPosition.x, m_PropertyPosition.y, k_ToggleSize, m_PropertyPosition.height);
                EditorGUI.PropertyField(m_Offset, m_UseBaseValue, GUIContent.none);

                m_Offset = new Rect(m_PropertyPosition.x + k_ToggleSize + k_HorizontalPadding, m_PropertyPosition.y, m_PropertyPosition.width - k_ToggleSize - k_HorizontalPadding, m_PropertyPosition.height);
                if (!m_UseBaseValue.boolValue)
                    EditorGUI.PropertyField(m_Offset, m_Primary, GUIContent.none);
                else
                    EditorGUI.LabelField(m_Offset, "Using Base Value");
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            m_TypeDifference = false;

            m_StatusName = property.FindPropertyRelative(nameof(Effect.StatusName));

            for (int i = 0; i < m_MultiObjectCount; i++)
            {
                m_StatusNameReference = (m_StatusName.GetParent(property.serializedObject.targetObjects[i]) as Effect).StatusName;
                m_StatusNameTypeDummy = m_StatusNameReference is StatusNameBool ? typeof(StatusNameBool)
                                      : m_StatusNameReference is StatusNameInt ? typeof(StatusNameInt)
                                                                                : typeof(StatusNameFloat);

                if (i > 0 && m_StatusNameTypeDummy != m_StatusNameType)
                {
                    m_TypeDifference = true;
                    break;
                }

                m_StatusNameType = m_StatusNameTypeDummy;
            }

            bool extraField = !m_TypeDifference && m_StatusNameType != typeof(StatusNameBool) && ((ValueModifier)property.FindPropertyRelative(nameof(Effect.ValueModifier)).enumValueFlag & (ValueModifier.Overwrite | ValueModifier.Minimum | ValueModifier.Maximum)) != 0;

            return (m_FieldSize + m_Padding) * (k_FieldCount + (extraField ? 1 : 0)) + m_Padding;
        }
    }
}
