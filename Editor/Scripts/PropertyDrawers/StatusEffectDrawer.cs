using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffect))]
    public class StatusEffectDrawer : PropertyDrawer
    {
        private const float _padding = 3;
        private const float _timingSize = 60;
        private const float _durationSize = 40;
        private const float _stackLabelSize = 38;
        private const float _stackSize = 40;
        private const int _fieldCount = 5;

        private SerializedProperty _data;
        private SerializedProperty _timing;
        private SerializedProperty _duration;
        private SerializedProperty _stack;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _data = property.FindPropertyRelative("data");
            _timing = property.FindPropertyRelative("timing");
            _duration = property.FindPropertyRelative("duration");
            _stack = property.FindPropertyRelative("stack");
            
            EditorGUI.BeginProperty(position, label, property);

            position.width -= _timingSize + _durationSize + _stackLabelSize + _stackSize + (_fieldCount - 1) * _padding;

            EditorGUI.PropertyField(position, _data, GUIContent.none);

            position.x += position.width + _padding;
            position.width = _timingSize;

            EditorGUI.LabelField(position, $"{_timing.enumDisplayNames[_timing.enumValueIndex]}:");

            position.x += position.width + _padding;
            position.width = _durationSize;

            EditorGUI.PropertyField(position, _duration, GUIContent.none);

            position.x += position.width + _padding;
            position.width = _stackLabelSize;

            EditorGUI.LabelField(position, $"Stack:");

            position.x += position.width + _padding;
            position.width = _stackSize;

            EditorGUI.PropertyField(position, _stack, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
