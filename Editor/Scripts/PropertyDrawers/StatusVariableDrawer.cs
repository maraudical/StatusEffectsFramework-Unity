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
        private float _fieldSize = EditorGUIUtility.singleLineHeight;
        private float _padding = EditorGUIUtility.standardVerticalSpacing;
        private const int _fieldCount = 4;
        private bool _foldout = false;

        private SerializedProperty _monoBehaviour;
        private SerializedProperty _statusName;
        private SerializedProperty _baseValue;
        private SerializedProperty _value;

        private Rect _propertyPosition;
        private object _baseValueObject;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _monoBehaviour = property.FindPropertyRelative("monoBehaviour");
            _statusName = property.FindPropertyRelative("statusName");
            _baseValue = property.FindPropertyRelative("baseValue");
            _value = property.FindPropertyRelative("_value");

            if (!Application.isPlaying &&  !_baseValue.serializedObject.isEditingMultipleObjects)
            {
                _baseValueObject = _baseValue.GetUnderlyingValue();

                if (_value.GetUnderlyingValue() != _baseValueObject)
                    _value.SetUnderlyingValue(_baseValueObject);
            }

            if (_monoBehaviour.objectReferenceValue == null 
             || _monoBehaviour.objectReferenceValue != property.serializedObject.targetObject 
             && !_monoBehaviour.hasMultipleDifferentValues
             && property.serializedObject.targetObject is MonoBehaviour)
            {
                _monoBehaviour.SetUnderlyingValue(property.serializedObject.targetObject);
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            _statusName.serializedObject.Update();
            _baseValue.serializedObject.Update();

            position.height = _fieldSize;
            position.y += _padding;

            GUI.color = !_statusName.objectReferenceValue && !_foldout ? Color.red : Color.white;
            _foldout = EditorGUI.Foldout(position, _foldout, label, true);
            GUI.color = Color.white;

            int indent = EditorGUI.indentLevel;

            EditorGUI.BeginProperty(position, label, property);

            if (_foldout)
            {
                EditorGUI.indentLevel = 2;
                position.x = 0;
                position.y += _fieldSize + _padding;
                GUI.color = !_statusName.objectReferenceValue ? Color.red : Color.white;
                EditorGUI.PropertyField(position, _statusName, new GUIContent(_statusName.displayName));
                GUI.color = Color.white;
                position.y += _fieldSize + _padding;

                EditorGUI.PropertyField(position, _baseValue, new GUIContent(_baseValue.displayName));
                position.y += _fieldSize + _padding;

                GUI.enabled = false;
                EditorGUI.PropertyField(position, _value, new GUIContent(_value.displayName));
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
            return (_fieldSize + _padding) * (_foldout ? _fieldCount : 1) + _padding;
        }
    }
}