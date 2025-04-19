using UnityEditor;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffect))]
    public class StatusEffectDrawer : PropertyDrawer
    {
        public VisualTreeAsset VisualTree;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var durationLabel = root.Q<Label>("duration-label");

            var timingProperty = property.FindPropertyRelative(nameof(StatusEffect.Timing));

            durationLabel.text = $"{timingProperty.enumDisplayNames[timingProperty.enumValueIndex]}:";

            return root;
        }
    }
}
