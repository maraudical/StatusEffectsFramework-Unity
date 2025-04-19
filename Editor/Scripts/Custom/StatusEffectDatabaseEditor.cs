using UnityEditor;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectDatabase))]
    public class StatusEffectDatabaseEditor : Editor
    {
        public VisualTreeAsset VisualTree;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var helpBox = new HelpBox() { text = "Do not reset this object using the context menu! It may break status effects!", messageType = HelpBoxMessageType.Warning };

            root.Q("warning-container").Add(helpBox);

            return root;
        }
    }
}
