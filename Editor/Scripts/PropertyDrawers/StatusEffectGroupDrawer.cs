#if UNITY_2023_1_OR_NEWER
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffectGroup))]
    internal class StatusEffectGroupDrawer : PropertyDrawer
    {
#if UNITY_2023_1_OR_NEWER
        public VisualTreeAsset VisualTree;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var maskField = root.Q<MaskField>("mask-field");
            var settingsButton = root.Q<Button>("settings-button");

            var valueProperty = property.FindPropertyRelative(nameof(StatusEffectGroup.Value));

            Dictionary<int, string> choices = StatusEffectSettings.GetOrCreateSettings().Groups.Select((g, index)=> new KeyValuePair<int, string>(index, g))
                                                                                               .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                                                                                               .ToDictionary(kvp => 1 << kvp.Key, kvp => kvp.Value);
            maskField.label = property.displayName;
            maskField.choices = choices.Values.ToList();
            maskField.choicesMasks = choices.Keys.ToList();
            maskField.AddToClassList("unity-base-field__aligned");
            maskField.BindProperty(valueProperty);

            settingsButton.iconImage = new Background { texture = EditorGUIUtility.IconContent("_Popup").image as Texture2D };
            settingsButton.clicked += Clicked;

            return root;

            void Clicked()
            {
                Selection.activeObject = StatusEffectSettings.GetOrCreateSettings();
            }
        }

#endif
        private SerializedProperty m_Value;
        private StatusEffectSettings m_Settings;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_Settings)
                m_Settings = StatusEffectSettings.GetOrCreateSettings();

            m_Value = property.FindPropertyRelative(nameof(StatusEffectGroup.Value));
            EditorGUI.BeginProperty(position, label, property);
            bool restoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = m_Value.hasMultipleDifferentValues;
            int maskValue = EditorGUI.MaskField(position, label, m_Value.intValue, m_Settings.Groups.Where(g => !string.IsNullOrEmpty(g)).ToArray());
            if (maskValue != m_Value.intValue)
                m_Value.intValue = maskValue;
            EditorGUI.showMixedValue = restoreShowMixedValue;
            EditorGUI.EndProperty();
        }
    }
}
