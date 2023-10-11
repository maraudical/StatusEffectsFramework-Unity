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
                style.fontStyle = FontStyle.Normal;
                if (ColorUtility.TryParseHtmlString(infoBox.hexCode, out Color color))
                    style.normal.textColor = color;
                style.fontStyle = infoBox.style;
                style.wordWrap = true;
                style.alignment = TextAnchor.MiddleCenter;
                style.stretchHeight = true;
                style.fixedWidth = position.width;

                EditorGUI.DrawRect(position, new Color(0, 0, 0, 0.15f));

                EditorGUI.LabelField(position, content, style);
            }
            else
            {
                base.OnGUI(position, property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String && style != null && content != null)
                return string.IsNullOrEmpty(content.text) ? 0 : style.CalcHeight(content, style.fixedWidth);
            else
                return base.GetPropertyHeight(property, label);
        }
    }
}
