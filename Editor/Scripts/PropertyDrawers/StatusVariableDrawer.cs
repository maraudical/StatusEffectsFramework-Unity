using System;
using System.Reflection;
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
        private const float _horizontalPadding = 3;
        private const int _fieldCount = 4;
        private const int _toggleSize = 15;
        private const int _signSize = 20;

        private SerializedProperty _statusName;
        private SerializedProperty _baseValue;
        private SerializedProperty _signProtected;
        private SerializedProperty _value;

        private Rect _propertyPosition;
        private Rect _offset;

        private int _indent;
        private bool _sign;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _statusName = property.FindPropertyRelative("statusName");
            _baseValue = property.FindPropertyRelative("_baseValue");
            _signProtected = property.FindPropertyRelative("_signProtected");
            _value = property.FindPropertyRelative("value");

            position.height = _fieldSize;
            position.y += _padding;
            
            GUI.color = !_statusName.objectReferenceValue && !property.isExpanded ? Color.red : Color.white;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            GUI.color = Color.white;

            EditorGUI.BeginProperty(position, label, property);

            _indent = EditorGUI.indentLevel;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel = _indent + 1;
                position.y += _fieldSize + _padding;
                GUI.color = !_statusName.objectReferenceValue ? Color.red : Color.white;
                EditorGUI.PropertyField(position, _statusName, new GUIContent(_statusName.displayName));
                GUI.color = Color.white;
                position.y += _fieldSize + _padding;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, _baseValue, new GUIContent(_baseValue.displayName));
                if (EditorGUI.EndChangeCheck() && Application.isPlaying)
                {
                    MethodInfo baseValueUpdate = typeof(StatusVariable).Assembly.GetType($"{typeof(StatusVariable).Namespace}.{property.type}").GetMethod("BaseValueUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var statusVariable in property.serializedObject.targetObjects)
                        baseValueUpdate.Invoke(_baseValue.GetParent(statusVariable), null);
                }
                position.y += _fieldSize + _padding;

                _propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(_value.displayName));
                EditorGUI.indentLevel = _indent;
                if (_signProtected != null && !_signProtected.Equals(null))
                {
                    _offset = new Rect(_propertyPosition.x, _propertyPosition.y, _toggleSize, _propertyPosition.height);
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(_offset, _signProtected, GUIContent.none);
                    if (EditorGUI.EndChangeCheck() && Application.isPlaying)
                    {
                        MethodInfo signProtectedUpdate = typeof(StatusVariable).Assembly.GetType($"{typeof(StatusVariable).Namespace}.{property.type}").GetMethod("SignProtectedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var statusVariable in property.serializedObject.targetObjects)
                            signProtectedUpdate.Invoke(_signProtected.GetParent(statusVariable), null);
                    }
                    if (_signProtected.boolValue)
                    {
                        _offset = new Rect(_propertyPosition.x - _signSize, _propertyPosition.y, _signSize, _propertyPosition.height);
                        _sign = Convert.ToInt32(_baseValue.GetParent(_baseValue.serializedObject.targetObject).GetValue("_baseValue")) >= 0;
                        GUI.color = _signProtected.hasMultipleDifferentValues || _baseValue.hasMultipleDifferentValues ? Color.white : _sign ? Color.green : Color.red;
                        EditorGUI.LabelField(_offset, $"({(_signProtected.hasMultipleDifferentValues || _baseValue.hasMultipleDifferentValues ? "?" : _sign ? "+" : "-")})");
                        GUI.color = Color.white;
                    }
                    _offset = new Rect(_propertyPosition.x + _toggleSize + _horizontalPadding, _propertyPosition.y, _propertyPosition.width - _toggleSize - _horizontalPadding, _propertyPosition.height);
                }
                else
                    _offset = new Rect(_propertyPosition.x, _propertyPosition.y, _propertyPosition.width, _propertyPosition.height);

                GUI.enabled = false;
                EditorGUI.PropertyField(_offset, Application.isPlaying ? _value : _baseValue, GUIContent.none);
                GUI.enabled = true;
            }
            else
            {
                GUI.enabled = false;
                _propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(" "));
                EditorGUI.PropertyField(_propertyPosition, Application.isPlaying ? _value : _baseValue, GUIContent.none);
                GUI.enabled = true;
            }

            EditorGUI.indentLevel = _indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (_fieldSize + _padding) * (property.isExpanded ? _fieldCount : 1) + _padding;
        }
    }
}