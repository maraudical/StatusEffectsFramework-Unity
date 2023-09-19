using UnityEditor;
using UnityEngine;

namespace StatusEffects.Editor
{
    [CustomPropertyDrawer(typeof(Effect))]
    public class EffectDrawer : PropertyDrawer
    {
        private const int fieldCount = 4;
        private float fieldSize = EditorGUIUtility.singleLineHeight;
        private const int padding = 2;
        private const int separatorSpace = 10;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty statusName = property.FindPropertyRelative("statusName");
            SerializedProperty valueType = property.FindPropertyRelative("valueType");

            SerializedProperty primary = valueType.enumValueIndex == (int)ValueType.Bool ?  property.FindPropertyRelative("boolValue") 
                                       : valueType.enumValueIndex == (int)ValueType.Int ? property.FindPropertyRelative("intValue") 
                                       : property.FindPropertyRelative("floatValue");

            SerializedProperty secondary = valueType.enumValueIndex == (int)ValueType.Bool ? property.FindPropertyRelative("priority")
                                         : property.FindPropertyRelative("valueModifier");

            EditorGUI.BeginProperty(position, label, property);
            
            EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, 1), Color.grey);

            position.height /= fieldCount;
            position.height -= padding;
            position.y += padding;
            Rect propertyPosition;

            propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(statusName.displayName));
            EditorGUI.PropertyField(propertyPosition, statusName, GUIContent.none);
            position.y += fieldSize + padding;

            propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(valueType.displayName));
            EditorGUI.PropertyField(propertyPosition, valueType, GUIContent.none);
            position.y += fieldSize + separatorSpace / 2;
                
            EditorGUI.DrawRect(new Rect(position.x + position.width / 4, position.y, position.width / 2, 1), Color.grey);
            position.y += separatorSpace / 2;

            propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(secondary.displayName));
            EditorGUI.PropertyField(propertyPosition, secondary, GUIContent.none);
            position.y += fieldSize + padding;

            propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(primary.displayName));
            EditorGUI.PropertyField(propertyPosition, primary, GUIContent.none);
            position.y += fieldSize + 3 * padding;

            EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, 1), Color.grey);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (fieldSize + padding) * fieldCount + separatorSpace + (padding * 2);
        }
    }
}
