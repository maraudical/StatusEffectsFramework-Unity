using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(Effect))]
    public class EffectDrawer : PropertyDrawer
    {
        private float _fieldSize = EditorGUIUtility.singleLineHeight;
        private float _padding = EditorGUIUtility.standardVerticalSpacing;
        private const float _horizontalPadding = 3;
        private const int _fieldCount = 3;
        private const int _toggleSize = 15;

        SerializedProperty statusName;
        SerializedProperty useBaseValue;
        SerializedProperty primary;
        SerializedProperty secondary;

        Rect propertyPosition;

        StatusName statusNameReference;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.isEditingMultipleObjects)
            {
                EditorGUI.LabelField(position, "Cannot multi-edit Effects.");
                return;
            }

            statusNameReference = (property.GetUnderlyingValue() as Effect).statusName;

            statusName = property.FindPropertyRelative("statusName");
            useBaseValue = property.FindPropertyRelative("useBaseValue");

            primary   = statusNameReference is StatusNameBool ? property.FindPropertyRelative("boolValue") 
                      : statusNameReference is StatusNameInt  ? property.FindPropertyRelative("intValue") 
                                                              : property.FindPropertyRelative("floatValue");

            secondary = statusNameReference is StatusNameBool ? property.FindPropertyRelative("priority")
                                                              : property.FindPropertyRelative("valueModifier");

            EditorGUI.BeginProperty(position, label, property);

            position.height /= _fieldCount;
            position.height -= _padding;
            position.y += _padding;

            propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(statusName.displayName));
            EditorGUI.PropertyField(propertyPosition, statusName, GUIContent.none);
            position.y += _fieldSize + _padding;

            propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(secondary.displayName));
            EditorGUI.PropertyField(propertyPosition, secondary, GUIContent.none);
            position.y += _fieldSize + _padding;

            propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(primary.displayName));
            Rect offset = new Rect(propertyPosition.x, propertyPosition.y, _toggleSize, propertyPosition.height);
            EditorGUI.PropertyField(offset, useBaseValue, GUIContent.none);

            offset = new Rect(propertyPosition.x + _toggleSize + _horizontalPadding, propertyPosition.y, propertyPosition.width - _toggleSize - _horizontalPadding, propertyPosition.height);
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
