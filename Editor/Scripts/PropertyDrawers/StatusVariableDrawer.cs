using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusFloat))]
    [CustomPropertyDrawer(typeof(StatusInt))]
    [CustomPropertyDrawer(typeof(StatusBool))]
    public class StatusVariableDrawer : PropertyDrawer
    {
        private const int fieldCount = 4;
        private float fieldSize = EditorGUIUtility.singleLineHeight;
        private const int padding = 2;
        private bool foldout = false;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty statusName = property.FindPropertyRelative("statusName");
            SerializedProperty baseValue = property.FindPropertyRelative("baseValue");
            SerializedProperty value = property.FindPropertyRelative("_value");
            SerializedProperty initialized = property.FindPropertyRelative("_initialized");

            if (!initialized.boolValue)
                value.SetUnderlyingValue(baseValue.GetUnderlyingValue());

            EditorGUI.BeginProperty(position, label, property);
            
            position.height /= (foldout ? fieldCount : 1);

            foldout = EditorGUI.Foldout(position, foldout, label);

            int indent = EditorGUI.indentLevel;
            Rect propertyPosition;

            if (foldout)
            {
                EditorGUI.indentLevel = 2;
                position.x = 0;
                position.y += fieldSize + padding;
                EditorGUI.PropertyField(position, statusName, GUIContent.none);
                position.y += fieldSize + padding;

                propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(baseValue.displayName));
                EditorGUI.PropertyField(propertyPosition, baseValue, GUIContent.none);
                position.y += fieldSize + padding;

                GUI.enabled = false;
                propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(value.displayName));
                EditorGUI.PropertyField(propertyPosition, value, GUIContent.none);
                GUI.enabled = true;
            }
            else
            {
                GUI.enabled = false;
                propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(" "));
                EditorGUI.PropertyField(propertyPosition, value, GUIContent.none);
                GUI.enabled = true;
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (fieldSize + padding) * (foldout ? fieldCount : 1);
        }
    }
}