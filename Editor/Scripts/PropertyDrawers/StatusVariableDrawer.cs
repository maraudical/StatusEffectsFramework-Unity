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
        
        private SerializedProperty _statusName;
        private SerializedProperty _baseValue;
        private SerializedProperty _value;

        private Rect _propertyPosition;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _statusName = property.FindPropertyRelative("statusName");
            _baseValue = property.FindPropertyRelative("baseValue");
            _value = property.FindPropertyRelative("value");

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

                GUI.enabled = !Application.isPlaying;
                EditorGUI.PropertyField(position, _baseValue, new GUIContent(_baseValue.displayName));
                position.y += _fieldSize + _padding;
                GUI.enabled = true;

                GUI.enabled = false;
                // If editiring multiple check bse valesu
                //if not playing just dislpay base
                EditorGUI.PropertyField(position, Application.isPlaying ? _value : _baseValue, new GUIContent(_value.displayName));
                GUI.enabled = true;
            }
            else
            {
                GUI.enabled = false;
                _propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(" "));
                EditorGUI.PropertyField(_propertyPosition, Application.isPlaying ? _value : _baseValue, GUIContent.none);
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