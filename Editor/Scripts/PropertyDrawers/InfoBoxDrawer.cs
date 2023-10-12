using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(InfoBoxAttribute))]
    public class InfoBoxDrawer : PropertyDrawer
    {
        GUIContent content;
        GUIStyle style;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                InfoBoxAttribute infoBox = (attribute as InfoBoxAttribute);
                content = new GUIContent(property.stringValue);
                style = new GUIStyle();
                style.wordWrap = true;
                style.stretchHeight = true;
                style.fixedWidth = position.width;

                EditorGUIUtility.SetIconSize(new Vector2(20, 20));

                if (!string.IsNullOrWhiteSpace(property.stringValue))
                    EditorGUI.HelpBox(position, property.stringValue, (MessageType)infoBox.messageType);
            }
            else
            {
                base.OnGUI(position, property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String && style != null && content != null)
                return string.IsNullOrWhiteSpace(content.text) ? 0 : Mathf.Max(style.CalcHeight(content, style.fixedWidth - 20), 30);
            else
                return base.GetPropertyHeight(property, label);
        }
    }
}
