using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffectGroup))]
    public class StatusEffectGroupDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty value = property.FindPropertyRelative("value");
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            value.intValue = EditorGUI.MaskField(position, value.intValue, StatusEffectSettings.GetOrCreateSettings().groups.Where(g => !string.IsNullOrEmpty(g)).ToArray());
            EditorGUI.EndProperty();
        }
    }
}
