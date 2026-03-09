using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace StatusEffectFramework.Editor
{
    [CustomPropertyDrawer(typeof(StatusEffect))]
    internal class StatusEffectDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var referenceProperty = property.FindPropertyRelative(nameof(StatusEffect.Data));
            var timingProperty = property.FindPropertyRelative(nameof(StatusEffect.Timing));
            var durationProperty = property.FindPropertyRelative($"m_{nameof(StatusEffect.Duration)}");
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
            durationLabel.text = $"{timingProperty.enumDisplayNames[timingProperty.enumValueIndex]}:";
            root.Add(durationLabel);

            var duration = new PropertyField(durationProperty, string.Empty);
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
            stacks.SetEnabled(false);
            root.Add(stacks);

            return root;
        }
    }
}
