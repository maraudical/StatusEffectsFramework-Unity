using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffect))]
    public class StatusEffectDrawer : PropertyDrawer
    {
        private const float m_Padding = 3;
        private const float m_TimingSize = 60;
        private const float m_DurationSize = 40;
        private const float m_StackLabelSize = 38;
        private const float m_StackSize = 40;
        private const int m_FieldCount = 5;

        private SerializedProperty m_Data;
        private SerializedProperty m_Timing;
        private SerializedProperty m_Duration;
        private SerializedProperty m_Stack;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_Data = property.FindPropertyRelative(nameof(StatusEffect.Data));
            m_Timing = property.FindPropertyRelative(nameof(StatusEffect.Timing));
            m_Duration = property.FindPropertyRelative($"m_{nameof(StatusEffect.Duration)}");
            m_Stack = property.FindPropertyRelative($"m_{nameof(StatusEffect.Stacks)}");
            
            EditorGUI.BeginProperty(position, label, property);

            position.width -= m_TimingSize + m_DurationSize + m_StackLabelSize + m_StackSize + (m_FieldCount - 1) * m_Padding;

            EditorGUI.PropertyField(position, m_Data, GUIContent.none);

            position.x += position.width + m_Padding;
            position.width = m_TimingSize;

            EditorGUI.LabelField(position, $"{m_Timing.enumDisplayNames[m_Timing.enumValueIndex]}:");

            position.x += position.width + m_Padding;
            position.width = m_DurationSize;

            EditorGUI.PropertyField(position, m_Duration, GUIContent.none);

            position.x += position.width + m_Padding;
            position.width = m_StackLabelSize;

            EditorGUI.LabelField(position, $"{m_Stack.displayName}:");

            position.x += position.width + m_Padding;
            position.width = m_StackSize;

            EditorGUI.PropertyField(position, m_Stack, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
