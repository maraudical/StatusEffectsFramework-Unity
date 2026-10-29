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
        /// <summary>
        /// Main USS class name of the StatusEffectGroup.
        /// </summary>
        public const string ussClassName = "sef-status-effect-group";

        /// <summary>
        /// USS class name of the StatusEffectGroup's button.
        /// </summary>
        public const string buttonUssClassName = ussClassName + "__button";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var valueProperty = property.FindPropertyRelative(nameof(StatusEffectGroup.Value));

            Dictionary<int, string> choices = StatusSettings.Instance.Groups.Select((g, index) => new KeyValuePair<int, string>(index, g))
                                                                                               .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                                                                                               .ToDictionary(kvp => 1 << kvp.Key, kvp => kvp.Value);

            var maskField = new MaskField
            {
                name = ussClassName,
                label = property.displayName,
                tooltip = this.GetTooltip(),
                choices = choices.Values.ToList(),
                choicesMasks = choices.Keys.ToList()
            };
            maskField.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
            maskField.AddToClassList(ussClassName);
            maskField.AddToClassList(StatusEffectsStyleSheet.MaskFieldSizeClassName);
            maskField.AddToClassList(BaseField<Enum>.alignedFieldUssClassName);
            maskField.BindProperty(valueProperty);

            var settingsButton = new Button
            {
                name = buttonUssClassName,
                focusable = false,
                iconImage = new Background { texture = EditorGUIUtility.IconContent("_Popup").image as Texture2D }
            };
            settingsButton.AddToClassList(buttonUssClassName);
            settingsButton.clicked += Clicked;
            maskField.Add(settingsButton);

            var maskLabel = maskField.Q<Label>(className: BaseField<int>.labelUssClassName);

            maskField.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            // Packages such as EditorAttributes color the label inline after a delay but skip the button's icon.
            settingsButton.schedule.Execute(() =>
            {
                var color = maskLabel.style.color;

                if (color.keyword is not StyleKeyword.Null)
                {
                    settingsButton.Q<Image>(className: Button.imageUSSClassName).tintColor = color.value;
                }
            }).StartingIn(StatusEffectsStyleSheet.ExternalColorDelay);

            return maskField;

            void Clicked()
            {
                Selection.activeObject = StatusSettings.Instance;
            }

            void OnGeometryChanged(GeometryChangedEvent evt)
            {
                maskField.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

                if (maskField.parent is PropertyField propertyField)
                {
                    if (propertyField.label == string.Empty)
                        maskLabel.RemoveFromHierarchy();
                    else if (propertyField.label != null)
                        maskField.label = propertyField.label;
                }
            }
        }

        private SerializedProperty m_Value;
        private StatusSettings m_Settings;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_Settings)
                m_Settings = StatusSettings.Instance;

            label = this.WithTooltip(label);

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
