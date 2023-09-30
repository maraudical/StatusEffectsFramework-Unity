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
        private const int _separatorSpace = 10;
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
            
            EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, 1), Color.grey);

            position.height /= _fieldCount;
            position.height -= _padding;
            position.y += _padding;
            Rect propertyPosition;

            EditorGUI.PropertyField(position, statusName, GUIContent.none);
            position.y += _fieldSize + _padding;

            propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(valueType.displayName));
            EditorGUI.PropertyField(propertyPosition, valueType, GUIContent.none);
            position.y += _fieldSize + _separatorSpace / 2;
                
            EditorGUI.DrawRect(new Rect(position.x + position.width / 4, position.y, position.width / 2, 1), Color.grey);
            position.y += _separatorSpace / 2;

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
            position.y += _fieldSize + 2.5f * _padding;

            EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, 1), Color.grey);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (_fieldSize + _padding) * _fieldCount + _separatorSpace + (_padding * 2);
        }
    }
}
