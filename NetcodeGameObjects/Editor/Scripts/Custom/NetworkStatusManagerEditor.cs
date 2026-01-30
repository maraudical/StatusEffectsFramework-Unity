#if NETCODE
using StatusEffects.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace StatusEffects.NetCode.GameObjects.Editor
{
    [CustomEditor(typeof(NetworkStatusManager))]
    [CanEditMultipleObjects]
    internal class NetworkStatusManagerEditor : StatusManagerEditor 
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            root.Bind(new SerializedObject(serializedObject.FindProperty($"m_StatusManager").objectReferenceValue));

            VisualTree.CloneTree(root);

            return root;
        }
    }
}
#endif