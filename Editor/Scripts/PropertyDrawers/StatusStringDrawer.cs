using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffects.Editor
{
    [CustomPropertyDrawer(typeof(StatusStringAttribute))]
    public class StatusStringDrawer : PropertyDrawer
    {
        private bool _useDropdown = false;
        private bool _initialized = false;

        private const int _toggleSize = 18;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                EditorGUI.BeginProperty(position, label, property);
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(property.displayName));

                Rect offset = new Rect(position.x, position.y, _toggleSize * (EditorGUI.indentLevel + 1), position.height);

                var list = StatusEffectSettings.GetOrCreateSettings().statuses;

                if (!_initialized && (list.Contains(property.stringValue) || property.stringValue == string.Empty))
                    _useDropdown = true;
                
                _initialized = true;
                _useDropdown = EditorGUI.Toggle(offset, _useDropdown);

                offset = new Rect(position.x + _toggleSize, position.y, position.width - _toggleSize, position.height);

                if (_useDropdown)
                {
                    int index = Mathf.Max(0, Array.IndexOf(list, property.stringValue));
                    index = EditorGUI.Popup(offset, string.Empty, index, list);

                    property.stringValue = list[index];
                }
                else
                {
                    EditorGUI.PropertyField(offset, property, GUIContent.none);

                }

                EditorGUI.EndProperty();
            }
            else
            {
                base.OnGUI(position, property, label);
            }
        }
    }
}
