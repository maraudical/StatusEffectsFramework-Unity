using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffectGroup))]
    public class StatusEffectGroupDrawer : PropertyDrawer
    {
        private SerializedProperty value;

        private bool _restoreShowMixedValue;
        private int _value;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            value = property.FindPropertyRelative("value");
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            _restoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = value.hasMultipleDifferentValues;
            _value = EditorGUI.MaskField(position, value.intValue, StatusEffectSettings.GetOrCreateSettings().groups.Where(g => !string.IsNullOrEmpty(g)).ToArray());
            if (_value != value.intValue)
                value.intValue = _value;
            EditorGUI.showMixedValue = _restoreShowMixedValue;
            EditorGUI.EndProperty();
        }
    }
}
