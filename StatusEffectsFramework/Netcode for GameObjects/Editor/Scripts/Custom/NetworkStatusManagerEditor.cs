using Unity.Netcode.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace StatusEffectsFramework.Netcode.Editor
{
    [CustomEditor(typeof(NetworkStatusManager))]
    [CanEditMultipleObjects]
    internal class NetworkStatusManagerEditor : NetworkBehaviourEditor
    {
        private const string k_Info = "Syncs Status Effects in the Status Manager over the network. Only add and remove status effects through this component.";

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            root.Add(new HelpBox(k_Info, HelpBoxMessageType.Info));

            return root;
        }
    }
}
