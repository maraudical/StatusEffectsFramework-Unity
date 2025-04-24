#if UNITY_2023_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine;
#endif
using UnityEditor;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffect))]
    internal class StatusEffectDrawer : PropertyDrawer
    {
#if UNITY_2023_1_OR_NEWER
        public VisualTreeAsset VisualTree;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var durationLabel = root.Q<Label>("duration-label");

            var timingProperty = property.FindPropertyRelative(nameof(StatusEffect.Timing));

            durationLabel.text = $"{timingProperty.enumDisplayNames[timingProperty.enumValueIndex]}:";

            return root;
        }
#else
        private const float k_Padding = 3;
        private const float k_TimingSize = 60;
        private const float k_DurationSize = 40;
        private const float k_StackLabelSize = 38;
        private const float k_StackSize = 40;
        private const float k_HorizontalFix = 5;
        private const int k_FieldCount = 5;

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

            position.width -= k_TimingSize + k_DurationSize + k_StackLabelSize + k_StackSize + (k_FieldCount - 1) * k_Padding + k_HorizontalFix;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, m_Data, GUIContent.none);
            EditorGUI.EndDisabledGroup();

            position.x += position.width + k_Padding;
            position.width = k_TimingSize;

            EditorGUI.LabelField(position, $"{m_Timing.enumDisplayNames[m_Timing.enumValueIndex]}:");

            position.x += position.width + k_Padding;
            position.width = k_DurationSize;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, m_Duration, GUIContent.none);
            EditorGUI.EndDisabledGroup();

            position.x += position.width + k_Padding;
            position.width = k_StackLabelSize;

            EditorGUI.LabelField(position, $"{m_Stack.displayName}:");

            position.x += position.width + k_Padding;
            position.width = k_StackSize;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, m_Stack, GUIContent.none);
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndProperty();
        }
#endif
    }
}
