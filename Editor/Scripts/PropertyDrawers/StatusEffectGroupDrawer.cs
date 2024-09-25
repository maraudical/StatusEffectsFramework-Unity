using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffectGroup))]
    public class StatusEffectGroupDrawer : PropertyDrawer
    {
        private SerializedProperty m_Value;

        private bool m_RestoreShowMixedValue;
        private int m_MaskValue;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_Value = property.FindPropertyRelative(nameof(StatusEffectGroup.Value));
            EditorGUI.BeginProperty(position, label, property);
            m_RestoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = m_Value.hasMultipleDifferentValues;
            m_MaskValue = EditorGUI.MaskField(position, label, m_Value.intValue, StatusEffectSettings.GetOrCreateSettings().Groups.Where(g => !string.IsNullOrEmpty(g)).ToArray());
            if (m_MaskValue != m_Value.intValue)
                m_Value.intValue = m_MaskValue;
            EditorGUI.showMixedValue = m_RestoreShowMixedValue;
            EditorGUI.EndProperty();
        }
    }
}
