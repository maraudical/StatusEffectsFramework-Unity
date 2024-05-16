using System;
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

        private SerializedProperty _statusName;
        private SerializedProperty _useBaseValue;
        private SerializedProperty _primary;
        private SerializedProperty _secondary;

        private Rect _propertyPosition;
        private Rect _offset;

        private StatusName _statusNameReference;
        private Type _statusNameType;
        private Type _statusNameTypeDummy;
        private int _multiObjectCount;
        private bool _typeDifference;
        private GUIStyle style;
        private Color _color;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            style = GUI.skin.label;
            style.alignment = TextAnchor.MiddleCenter;

            _multiObjectCount = property.serializedObject.targetObjects.Length;
            _typeDifference = false;

            _statusName = property.FindPropertyRelative("statusName");
            _useBaseValue = property.FindPropertyRelative("useBaseValue");

            for (int i = 0; i < _multiObjectCount; i++)
            {
                _statusNameReference = (_statusName.GetParent(property.serializedObject.targetObjects[i]) as Effect).statusName;
                _statusNameTypeDummy = _statusNameReference is StatusNameBool ? typeof(StatusNameBool)
                                     : _statusNameReference is StatusNameInt  ? typeof(StatusNameInt)
                                                                              : typeof(StatusNameFloat);

                if (i > 0 && _statusNameTypeDummy != _statusNameType)
                {
                    _typeDifference = true;
                    break;
                }
                
                _statusNameType = _statusNameTypeDummy;
            }

            _primary = _statusNameType == typeof(StatusNameBool)   ? property.FindPropertyRelative("boolValue")
                      : _statusNameType == typeof(StatusNameInt)   ? property.FindPropertyRelative("intValue")
                                                                   : property.FindPropertyRelative("floatValue");

            _secondary = _statusNameType == typeof(StatusNameBool) ? property.FindPropertyRelative("priority")
                                                                   : property.FindPropertyRelative("valueModifier");

            EditorGUI.BeginProperty(position, label, property);

            position.height = _fieldSize;
            position.y += _padding;

            EditorGUI.PropertyField(position, _statusName, new GUIContent(_statusName.displayName));
            position.y += _fieldSize + _padding;

            if (_typeDifference)
            {
                _color = GUI.color;
                GUI.color = Color.yellow;
                EditorGUI.LabelField(position, "Cannot display information due", style);
            }
            else
            {
                EditorGUI.PropertyField(position, _secondary, new GUIContent(_secondary.displayName));
            }
            position.y += _fieldSize + _padding;

            if (_typeDifference)
            {
                EditorGUI.LabelField(position, "to Status Name type difference.", style);
                GUI.color = _color;
            }
            else
            {
                _propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(_primary.displayName));
                _offset = new Rect(_propertyPosition.x, _propertyPosition.y, _toggleSize, _propertyPosition.height);
                EditorGUI.PropertyField(_offset, _useBaseValue, GUIContent.none);

                _offset = new Rect(_propertyPosition.x + _toggleSize + _horizontalPadding, _propertyPosition.y, _propertyPosition.width - _toggleSize - _horizontalPadding, _propertyPosition.height);
                if (!_useBaseValue.boolValue)
                    EditorGUI.PropertyField(_offset, _primary, GUIContent.none);
                else
                    EditorGUI.LabelField(_offset, "Using Base Value");
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (_fieldSize + _padding) * _fieldCount + _padding;
        }
    }
}
