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
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var referenceProperty = property.FindPropertyRelative(nameof(StatusEffect.Data));
            var timingProperty = property.FindPropertyRelative(nameof(StatusEffect.Timing));
            var durationProperty = property.FindPropertyRelative($"m_{nameof(StatusEffect.Duration)}");
            var timeAddedProperty = property.FindPropertyRelative($"m_{nameof(StatusEffect.TimeAdded)}");
            var stacksProperty = property.FindPropertyRelative($"m_{nameof(StatusEffect.Stacks)}");

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow = 1;
            root.style.flexShrink = 1;
            root.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
            root.AddToClassList(StatusEffectsStyleSheet.MaskFieldSizeClassName);

            var reference = new ObjectField();
            reference.style.flexGrow = 1;
            reference.style.flexShrink = 1;
            reference.style.minWidth = 42;
            reference.SetEnabled(false);
            reference.BindProperty(referenceProperty);
            root.Add(reference);

            var durationLabel = new Label();
            durationLabel.style.minWidth = 67;
            durationLabel.style.paddingRight = 0;
            durationLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            durationLabel.text = $"{(timingProperty.hasMultipleDifferentValues ? "—" : timingProperty.enumDisplayNames[timingProperty.enumValueIndex])}:";
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
                        durationFloat.schedule.Execute(() => { durationFloat.value = Mathf.Round(statusEffect.TimeRemaining(Time.timeAsDouble) * 100f) / 100f; }).Every(0);
                        break;
                    default:
                        duration = new PropertyField(durationProperty, string.Empty);
                        break;
                }

            if (hasDifferentDurationValues)
                duration = new TextField(string.Empty) { value = "-" };

            duration.style.flexShrink = 0;
            duration.style.width = 42;
            duration.SetEnabled(false);
            root.Add(duration);

            var stacksLabel = new Label();
            stacksLabel.style.minWidth = 50;
            stacksLabel.style.paddingRight = 0;
            stacksLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            stacksLabel.text = "Stacks:";
            root.Add(stacksLabel);

            var stacks = new PropertyField(stacksProperty, string.Empty);
            stacks.style.flexShrink = 0;
            stacks.style.width = 42;
            stacks.style.marginRight = 5;
            stacks.SetEnabled(false);
            root.Add(stacks);

            return root;
        }
    }
}
