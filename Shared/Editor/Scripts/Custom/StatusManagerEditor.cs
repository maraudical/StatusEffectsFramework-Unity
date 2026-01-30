using UnityEditor;
using UnityEngine.UIElements;

namespace StatusEffects.Editor
{
    [CustomEditor(typeof(StatusManager))]
    [CanEditMultipleObjects]
    internal class StatusManagerEditor : UnityEditor.Editor
    {
        public VisualTreeAsset VisualTree;
        
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);

            return root;
        }
    }
}
