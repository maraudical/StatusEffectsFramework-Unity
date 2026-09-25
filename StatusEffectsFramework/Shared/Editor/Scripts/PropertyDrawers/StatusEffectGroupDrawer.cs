using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffectsFramework.Editor
{
    [CustomPropertyDrawer(typeof(StatusEffectGroup))]
    internal class StatusEffectGroupDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var valueProperty = property.FindPropertyRelative(nameof(StatusEffectGroup.Value));

            Dictionary<int, string> choices = StatusSettings.GetOrCreateSettings().Groups.Select((g, index) => new KeyValuePair<int, string>(index, g))
                                                                                               .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                                                                                               .ToDictionary(kvp => 1 << kvp.Key, kvp => kvp.Value);
            

            var maskField = new MaskField() { name = $"unity-input-{property.name}" };
            maskField.label = property.displayName;
            maskField.choices = choices.Values.ToList();
            maskField.choicesMasks = choices.Keys.ToList();
            maskField.AddToClassList(BaseField<Enum>.alignedFieldUssClassName);
            maskField.BindProperty(valueProperty);
            maskField.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
            maskField.AddToClassList(StatusEffectsStyleSheet.MaskFieldSizeClassName);

            var settingsButton = new Button();
            settingsButton.style.marginTop = 0;
            settingsButton.style.marginBottom = 0;
            settingsButton.style.marginRight = 0;
            settingsButton.style.marginLeft = 1;
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
            maskField.Add(settingsButton);

            var maskLabel = maskField.Q<Label>();

            maskField.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            settingsButton.schedule.Execute(() =>
            {
                var color = maskLabel.style.color;

                if (color.keyword is not StyleKeyword.Null)
                {
                    settingsButton.Q<Image>(className: Button.imageUSSClassName).tintColor = color.value;
                }
            }).StartingIn(50);

            return maskField;

            void Clicked()
            {
                Selection.activeObject = StatusSettings.GetOrCreateSettings();
            }

            void OnGeometryChanged(GeometryChangedEvent evt)
            {
                maskField.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

                if (maskField.parent is PropertyField propertyField)
                    if (propertyField.label == string.Empty)
                        maskLabel.RemoveFromHierarchy();
                    else if (propertyField.label != null)
                        maskField.label = propertyField.label;
            }
        }

        private SerializedProperty m_Value;
        private StatusSettings m_Settings;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_Settings)
                m_Settings = StatusSettings.GetOrCreateSettings();

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
