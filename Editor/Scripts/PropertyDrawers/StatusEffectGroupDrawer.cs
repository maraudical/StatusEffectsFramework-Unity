using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffectGroup))]
    public class StatusEffectGroupDrawer : PropertyDrawer
    {
        private SerializedProperty _property;

        private bool _restoreShowMixedValue;
        private int _value;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _property = property.FindPropertyRelative("value");
            EditorGUI.BeginProperty(position, label, property);
            _restoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = _property.hasMultipleDifferentValues;
            _value = EditorGUI.MaskField(position, label, _property.intValue, StatusEffectSettings.GetOrCreateSettings().groups.Where(g => !string.IsNullOrEmpty(g)).ToArray());
            if (_value != _property.intValue)
                _property.intValue = _value;
            EditorGUI.showMixedValue = _restoreShowMixedValue;
            EditorGUI.EndProperty();
        }
    }
}
