using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Editor
{
    [CustomPropertyDrawer(typeof(Effect))]
    public class EffectDrawer : PropertyDrawer
    {
        private const int _fieldCount = 4;
        private float _fieldSize = EditorGUIUtility.singleLineHeight;
        private const int _padding = 2;
        private const int _toggleSize = 30;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty statusName = property.FindPropertyRelative("statusName");
            SerializedProperty valueType = property.FindPropertyRelative("valueType");
            SerializedProperty useBaseValue = property.FindPropertyRelative("useBaseValue");

            SerializedProperty primary = valueType.enumValueIndex == (int)ValueType.Bool ?  property.FindPropertyRelative("boolValue") 
                                       : valueType.enumValueIndex == (int)ValueType.Int ? property.FindPropertyRelative("intValue") 
                                       : property.FindPropertyRelative("floatValue");

            SerializedProperty secondary = valueType.enumValueIndex == (int)ValueType.Bool ? property.FindPropertyRelative("priority")
                                         : property.FindPropertyRelative("valueModifier");

            EditorGUI.BeginProperty(position, label, property);

            position.height /= _fieldCount;
            position.height -= _padding;
            position.y += _padding;
            Rect propertyPosition;

            EditorGUI.PropertyField(position, statusName, GUIContent.none);
            position.y += _fieldSize + _padding;

            propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(valueType.displayName));
            EditorGUI.PropertyField(propertyPosition, valueType, GUIContent.none);
            position.y += _fieldSize + _padding;

            propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(secondary.displayName));
            EditorGUI.PropertyField(propertyPosition, secondary, GUIContent.none);
            position.y += _fieldSize + _padding;

            propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(primary.displayName));
            Rect offset = new Rect(propertyPosition.x, propertyPosition.y, _toggleSize, propertyPosition.height);
            EditorGUI.PropertyField(offset, useBaseValue, GUIContent.none);

            offset = new Rect(propertyPosition.x + _toggleSize, propertyPosition.y, propertyPosition.width - _toggleSize, propertyPosition.height);
            if (!useBaseValue.boolValue)
                EditorGUI.PropertyField(offset, primary, GUIContent.none);
            else
                EditorGUI.LabelField(offset, "Using Base Value");
            position.y += _fieldSize + _padding;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (_fieldSize + _padding) * _fieldCount + (_padding * 2);
        }
    }
}
