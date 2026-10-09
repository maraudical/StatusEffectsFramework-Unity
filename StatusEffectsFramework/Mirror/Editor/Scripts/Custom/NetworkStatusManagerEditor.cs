using StatusEffectsFramework.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffectsFramework.Mirror.Editor
{
    [CustomEditor(typeof(NetworkStatusManager))]
    [CanEditMultipleObjects]
    internal class NetworkStatusManagerEditor : StatusManagerEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            if (((Component)target).TryGetComponent(out StatusManager statusManager))
                root.Bind(new SerializedObject(statusManager));

            VisualTree.CloneTree(root);

            return root;
        }
    }
}
