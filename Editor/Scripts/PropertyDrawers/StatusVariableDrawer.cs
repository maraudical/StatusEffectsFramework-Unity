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

        SerializedProperty statusName;
        SerializedProperty baseValue;
        SerializedProperty value;

        Rect propertyPosition;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.isEditingMultipleObjects)
            {
                EditorGUI.LabelField(position, "Cannot multi-edit Status Variables.");
                return;
            }

            statusName = property.FindPropertyRelative("statusName");
            baseValue = property.FindPropertyRelative("baseValue");
            value = property.FindPropertyRelative("_value");
            
            if (!Application.isPlaying)
                value.SetUnderlyingValue(baseValue.GetUnderlyingValue());

            EditorGUI.BeginProperty(position, label, property);

            statusName.serializedObject.Update();
            baseValue.serializedObject.Update();

            position.height /= (foldout ? fieldCount : 1);

            GUI.color = !statusName.objectReferenceValue && !foldout ? Color.red : Color.white;
            foldout = EditorGUI.Foldout(position, foldout, label);
            GUI.color = Color.white;

            int indent = EditorGUI.indentLevel;

            if (foldout)
            {
                EditorGUI.indentLevel = 2;
                position.x = 0;
                position.y += fieldSize + padding;
                GUI.color = !statusName.objectReferenceValue ? Color.red : Color.white;
                propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(statusName.displayName));
                GUI.color = Color.white;
                EditorGUI.PropertyField(propertyPosition, statusName, GUIContent.none);
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

            statusName.serializedObject.ApplyModifiedProperties();
            baseValue.serializedObject.ApplyModifiedProperties();

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (fieldSize + padding) * (foldout && !property.serializedObject.isEditingMultipleObjects ? fieldCount : 1);
        }
    }
}