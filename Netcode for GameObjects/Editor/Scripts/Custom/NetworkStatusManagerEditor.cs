#if NETCODE
using StatusEffectFramework.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace StatusEffectFramework.NetCode.Editor
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