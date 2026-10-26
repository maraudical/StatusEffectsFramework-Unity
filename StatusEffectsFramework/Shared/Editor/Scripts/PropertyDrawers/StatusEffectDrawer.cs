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

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var dataProperty = property.FindPropertyRelative(nameof(StatusEffect.Data));
            var timingProperty = property.FindPropertyRelative(nameof(StatusEffect.Timing));
            var durationProperty = property.FindPropertyRelative($"m_{nameof(StatusEffect.Duration)}");
            var timeAddedProperty = property.FindPropertyRelative($"m_{nameof(StatusEffect.TimeAdded)}");
            var stacksProperty = property.FindPropertyRelative($"m_{nameof(StatusEffect.Stacks)}");

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
                text = $"{(timingProperty.hasMultipleDifferentValues ? "—" : timingProperty.enumDisplayNames[timingProperty.enumValueIndex])}:"
            };
            durationLabel.AddToClassList(durationLabelUssClassName);
            root.Add(durationLabel);

            VisualElement duration = default;
            bool hasDifferentDurationValues = false;

            if (timingProperty.hasMultipleDifferentValues)
                hasDifferentDurationValues = true;
            else
                switch (timingProperty.enumValueIndex)
                {
                    case (int)StatusEffectTiming.Duration:
                        if (timeAddedProperty.hasMultipleDifferentValues || durationProperty.hasMultipleDifferentValues)
                        {
                            hasDifferentDurationValues = true;
                            break;
                        }

                        var durationFloat = new FloatField(string.Empty);
                        duration = durationFloat;
                        StatusEffect statusEffect = (StatusEffect)durationProperty.GetParent(property.serializedObject.targetObject);
                        var statusManager = property.serializedObject.targetObject as StatusManager;
                        durationFloat.schedule.Execute(() => { durationFloat.value = Mathf.Round(statusEffect.TimeRemaining(statusManager ? statusManager.CurrentTime : Time.timeAsDouble) * 100f) / 100f; }).Every(0);
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

            return root;
        }
    }
}
