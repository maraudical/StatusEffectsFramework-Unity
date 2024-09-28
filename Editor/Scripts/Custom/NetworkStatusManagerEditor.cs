#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
using UnityEditor;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(NetworkStatusManager))]
    [CanEditMultipleObjects]
    public class NetworkStatusManagerEditor : Editor
    {
        private SerializedProperty m_StatusManager;

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        private void OnEnable()
        {
            m_StatusManager = serializedObject.FindProperty("m_StatusManager");
        }

        public override void OnInspectorGUI()
        {
            CreateEditor(m_StatusManager.objectReferenceValue).OnInspectorGUI();
        }
    }
}
#endif