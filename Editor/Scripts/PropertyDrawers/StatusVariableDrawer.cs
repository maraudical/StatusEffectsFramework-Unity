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
        private const int _fieldCount = 4;
        private const int _padding = 2;
        private float _fieldSize = EditorGUIUtility.singleLineHeight;
        private bool _foldout = false;

        private SerializedProperty _statusName;
        private SerializedProperty _baseValue;
        private SerializedProperty _value;

        private Rect _propertyPosition;
        private object _baseValueObject;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _statusName = property.FindPropertyRelative("statusName");
            _baseValue = property.FindPropertyRelative("baseValue");
            _value = property.FindPropertyRelative("_value");

            if (!Application.isPlaying &&  !_baseValue.serializedObject.isEditingMultipleObjects)
            {
                _baseValueObject = _baseValue.GetUnderlyingValue();

                if (_value.GetUnderlyingValue() != _baseValueObject)
                    _value.SetUnderlyingValue(_baseValueObject);
            }

            _statusName.serializedObject.Update();
            _baseValue.serializedObject.Update();

            position.height /= (_foldout ? _fieldCount : 1);

            GUI.color = !_statusName.objectReferenceValue && !_foldout ? Color.red : Color.white;
            _foldout = EditorGUI.Foldout(position, _foldout, label);
            GUI.color = Color.white;

            int indent = EditorGUI.indentLevel;

            EditorGUI.BeginProperty(position, label, property);

            if (_foldout)
            {
                EditorGUI.indentLevel = 2;
                position.x = 0;
                position.y += _fieldSize + _padding;
                GUI.color = !_statusName.objectReferenceValue ? Color.red : Color.white;
                _propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(_statusName.displayName));
                GUI.color = Color.white;
                EditorGUI.PropertyField(_propertyPosition, _statusName, GUIContent.none);
                position.y += _fieldSize + _padding;

                _propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(_baseValue.displayName));
                EditorGUI.PropertyField(_propertyPosition, _baseValue, GUIContent.none);
                position.y += _fieldSize + _padding;

                GUI.enabled = false;
                _propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(_value.displayName));
                EditorGUI.PropertyField(_propertyPosition, _value, GUIContent.none);
                GUI.enabled = true;
            }
            else
            {
                GUI.enabled = false;
                _propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(" "));
                EditorGUI.PropertyField(_propertyPosition, _value, GUIContent.none);
                GUI.enabled = true;
            }

            _statusName.serializedObject.ApplyModifiedProperties();
            _baseValue.serializedObject.ApplyModifiedProperties();

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (_fieldSize + _padding) * (_foldout ? _fieldCount : 1);
        }
    }
}