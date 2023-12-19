using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffect))]
    public class StatusEffectDrawer : PropertyDrawer
    {
        private const float _padding = 3;
        private const float _durationSize = 48;

        private SerializedProperty data;
        private SerializedProperty duration;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            data = property.FindPropertyRelative("data");
            duration = property.FindPropertyRelative("duration");
            
            EditorGUI.BeginProperty(position, label, property);

            position.width -= _durationSize + _padding;

            EditorGUI.PropertyField(position, data, GUIContent.none);

            position.x += position.width + _padding;
            position.width = _durationSize;

            EditorGUI.PropertyField(position, duration, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
