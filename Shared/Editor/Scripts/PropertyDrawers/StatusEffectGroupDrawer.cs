using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffectFramework.Editor
{
    [CustomPropertyDrawer(typeof(StatusEffectGroup))]
    internal class StatusEffectGroupDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var valueProperty = property.FindPropertyRelative(nameof(StatusEffectGroup.Value));

            Dictionary<int, string> choices = StatusEffectSettings.GetOrCreateSettings().Groups.Select((g, index) => new KeyValuePair<int, string>(index, g))
                                                                                               .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                                                                                               .ToDictionary(kvp => 1 << kvp.Key, kvp => kvp.Value);

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
            root.AddToClassList(StatusEffectsStyleSheet.MaskFieldSizeClassName);

            var maskField = new MaskField();
            maskField.style.flexGrow = 1;
            maskField.style.flexShrink = 1;
            maskField.label = property.displayName;
            maskField.choices = choices.Values.ToList();
            maskField.choicesMasks = choices.Keys.ToList();
            maskField.AddToClassList(BaseField<Enum>.alignedFieldUssClassName);
            maskField.BindProperty(valueProperty);
            root.Add(maskField);

            var settingsButton = new Button();
            settingsButton.style.marginRight = -2;
            settingsButton.style.paddingLeft = 0;
            settingsButton.style.paddingRight = 0;
            settingsButton.style.paddingTop = 0;
            settingsButton.style.paddingBottom = 0;
            settingsButton.focusable = false;
#if UNITY_2023_1_OR_NEWER
            settingsButton.iconImage = new Background { texture = EditorGUIUtility.IconContent("_Popup").image as Texture2D };
#else
            settingsButton.style.backgroundImage = EditorGUIUtility.IconContent("_Popup").image as Texture2D;
            settingsButton.RegisterCallback<GeometryChangedEvent>(GeometryChanged);

            void GeometryChanged(GeometryChangedEvent changeEvent)
            {
                float size = settingsButton.resolvedStyle.height;
                settingsButton.style.width = size;
                settingsButton.style.backgroundSize = new BackgroundSize(size - 3, size - 3);
            }
#endif
            settingsButton.clicked += Clicked;
            root.Add(settingsButton);

            var maskLabel = maskField.Q<Label>();

            settingsButton.schedule.Execute(() =>
            {
                var color = maskLabel.style.color;

                if (color.keyword is not StyleKeyword.Null)
                {
                    settingsButton.Q<Image>(className: Button.imageUSSClassName).tintColor = color.value;
                }
            }).StartingIn(50);

            return root;

            void Clicked()
            {
                Selection.activeObject = StatusEffectSettings.GetOrCreateSettings();
            }
        }

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
