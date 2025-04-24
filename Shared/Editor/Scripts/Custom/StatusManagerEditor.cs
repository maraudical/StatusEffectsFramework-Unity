using UnityEditor;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusManager))]
    [CanEditMultipleObjects]
    internal class StatusManagerEditor : Editor
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
