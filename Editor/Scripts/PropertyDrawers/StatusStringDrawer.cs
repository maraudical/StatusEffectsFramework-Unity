using System;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Editor
{
    [CustomPropertyDrawer(typeof(StatusStringAttribute))]
    public class StatusStringDrawer : PropertyDrawer
    {
        private const int _toggleSize = 18;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                EditorGUI.BeginProperty(position, label, property);

                Rect offset = new Rect(position.x, position.y, _toggleSize, position.height);

                StatusStringAttribute stringAttribute = (attribute as StatusStringAttribute);
                stringAttribute.useDropdown = EditorGUI.Toggle(offset, stringAttribute.useDropdown);

                offset = new Rect(position.x + _toggleSize, position.y, position.width - _toggleSize, position.height);

                if (stringAttribute.useDropdown)
                {
                    var list = Resources.Load<StatusEffectSettings>("StatusEffectsFramework/StatusEffectSettings").statuses;

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
