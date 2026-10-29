using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffectsFramework.Editor
{
    [CustomPropertyDrawer(typeof(StatusEffect))]
    internal class StatusEffectDrawer : PropertyDrawer
    {
        /// <summary>
        /// Main USS class name of the Status Effect.
        /// </summary>
        public const string ussClassName = "sef-status-effect";

        /// <summary>
        /// USS class name of the Status Effect's data field.
        /// </summary>
        public const string dataUssClassName = ussClassName + "__data";

        /// <summary>
        /// USS class name of the Status Effect's duration field.
        /// </summary>
        public const string durationUssClassName = ussClassName + "__duration";

        /// <summary>
        /// USS class name of the Status Effect's duration label.
        /// </summary>
        public const string durationLabelUssClassName = ussClassName + "__duration-label";

        /// <summary>
        /// USS class name of the Status Effect's stacks field.
        /// </summary>
        public const string stacksUssClassName = ussClassName + "__stacks";

        /// <summary>
        /// USS class name of the Status Effect's stacks label.
        /// </summary>
        public const string stacksLabelUssClassName = ussClassName + "__stacks-label";

        /// <summary>
        /// USS class name of the Status Effect's remove button.
        /// </summary>
        public const string removeUssClassName = ussClassName + "__remove";

        /// <summary>
        /// USS class name of the icon inside the Status Effect's remove button.
        /// </summary>
        public const string removeIconUssClassName = ussClassName + "__remove-icon";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var dataProperty = property.FindPropertyRelative(nameof(StatusEffect.Data));
            var durationProperty = property.FindPropertyRelative($"m_{nameof(StatusEffect.Duration)}");
            var stacksProperty = property.FindPropertyRelative($"m_{nameof(StatusEffect.Stacks)}");
            // Timing comes from the timer, which isn't a serialized value, so it's read from the instance.
            // Different targets can't be shown together.
            bool multipleTargets = property.serializedObject.isEditingMultipleObjects;
            var statusEffect = multipleTargets ? null : (StatusEffect)durationProperty.GetParent(property.serializedObject.targetObject);

            var root = new VisualElement
            {
                name = ussClassName
            };
            root.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
            root.AddToClassList(ussClassName);

            var data = new ObjectField
            {
                name = dataUssClassName
            };
            data.AddToClassList(dataUssClassName);
            data.SetEnabled(false);
            data.BindProperty(dataProperty);
            root.Add(data);

            var durationLabel = new Label
            {
                name = durationLabelUssClassName,
                text = $"{(statusEffect == null ? "—" : ObjectNames.NicifyVariableName(statusEffect.Timing.ToString()))}:"
            };
            durationLabel.AddToClassList(durationLabelUssClassName);
            root.Add(durationLabel);

            VisualElement duration = default;
            bool hasDifferentDurationValues = false;

            if (statusEffect == null)
                hasDifferentDurationValues = true;
            else
                switch (statusEffect.Timing)
                {
                    case StatusEffectTiming.Duration:
                        var durationFloat = new FloatField(string.Empty);
                        duration = durationFloat;
                        durationFloat.schedule.Execute(() => { durationFloat.value = Mathf.Round(statusEffect.TimeRemaining() * 100f) / 100f; }).Every(0);
                        break;
                    default:
                        duration = new PropertyField(durationProperty, string.Empty);
                        break;
                }

            if (hasDifferentDurationValues)
                duration = new TextField(string.Empty) { value = "-" };

            duration.name = durationUssClassName;
            duration.AddToClassList(durationUssClassName);
            duration.SetEnabled(false);
            root.Add(duration);

            var stacksLabel = new Label
            {
                name = stacksLabelUssClassName,
                text = "Stacks:"
            };
            stacksLabel.AddToClassList(stacksLabelUssClassName);
            root.Add(stacksLabel);

            var stacks = new PropertyField(stacksProperty, string.Empty)
            {
                name = stacksUssClassName
            };
            stacks.AddToClassList(stacksUssClassName);
            stacks.SetEnabled(false);
            root.Add(stacks);

            // The effect in this row on each selected Status Manager. With several selected, each manager's
            // effect at this position is removed, which can be a different effect on each one.
            var removals = new List<(StatusManager statusManager, StatusEffect statusEffect)>();

            foreach (var target in property.serializedObject.targetObjects)
            {
                if (target is not StatusManager targetManager)
                    continue;

                try
                {
                    if (durationProperty.GetParent(targetManager) is StatusEffect targetEffect)
                        removals.Add((targetManager, targetEffect));
                }
                catch (InvalidOperationException)
                {
                    // This manager has fewer effects than the row's position.
                }
            }

            if (removals.Count > 0)
            {
                var remove = new Button(() =>
                {
                    // Does nothing for an effect that was already removed, such as when this row is out of date.
                    foreach (var (targetManager, targetEffect) in removals)
                        if (targetManager)
                            targetManager.RemoveStatusEffect(targetEffect);
                })
                {
                    name = removeUssClassName,
                    tooltip = multipleTargets ? "Remove the status effect in this row from each selected Status Manager." : "Remove this status effect."
                };
                remove.AddToClassList(removeUssClassName);

                // An icon centers exactly, where a text glyph sits wherever the font's metrics put it.
                var crossIcon = EditorGUIUtility.FindTexture("CrossIcon");

                if (crossIcon)
                {
                    var icon = new VisualElement
                    {
                        name = removeIconUssClassName
                    };
                    icon.AddToClassList(removeIconUssClassName);
                    icon.style.backgroundImage = crossIcon;
                    remove.Add(icon);
                }
                else
                    remove.text = "×";

                // Status effects only run in play mode.
                remove.SetEnabled(Application.isPlaying);
                root.Add(remove);
            }

            return root;
        }
    }
}
