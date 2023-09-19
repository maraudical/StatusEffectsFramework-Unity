using System;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Editor
{
    [CustomPropertyDrawer(typeof(GroupStringAttribute))]
    public class GroupStringDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var list = StatusEffectSettings.GetOrCreateSettings().groups;
            if (property.propertyType == SerializedPropertyType.String)
            {
                int index = Mathf.Max(0, Array.IndexOf(list, property.stringValue));
                index = EditorGUI.Popup(position, property.displayName, index, list);

                property.stringValue = list[index];
            }
            else
            {
                base.OnGUI(position, property, label);
            }
        }
    }
}