using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffect))]
    public class StatusEffectDrawer : PropertyDrawer
    {
        private const float _padding = 3;
        private const float _timingSize = 54;
        private const float _durationSize = 40;
        private const float _stackLabelSize = 38;
        private const float _stackSize = 40;
        private const int _fieldCount = 5;

        private SerializedProperty data;
        private SerializedProperty timing;
        private SerializedProperty duration;
        private SerializedProperty stack;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            data = property.FindPropertyRelative("data");
            timing = property.FindPropertyRelative("timing");
            duration = property.FindPropertyRelative("duration");
            stack = property.FindPropertyRelative("stack");
            
            EditorGUI.BeginProperty(position, label, property);

            position.width -= _timingSize + _durationSize + _stackLabelSize + _stackSize + (_fieldCount - 1) * _padding;

            EditorGUI.PropertyField(position, data, GUIContent.none);

            position.x += position.width + _padding;
            position.width = _timingSize;

            EditorGUI.LabelField(position, $"{timing.enumDisplayNames[timing.enumValueIndex]}:");

            position.x += position.width + _padding;
            position.width = _durationSize;

            EditorGUI.PropertyField(position, duration, GUIContent.none);

            position.x += position.width + _padding;
            position.width = _stackLabelSize;

            EditorGUI.LabelField(position, $"Stack:");

            position.x += position.width + _padding;
            position.width = _stackSize;

            EditorGUI.PropertyField(position, stack, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
