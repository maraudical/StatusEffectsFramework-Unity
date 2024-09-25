using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(NetworkStatusManager))]
    [CanEditMultipleObjects]
    public class NetworkStatusManagerEditor : Editor
    {
        private SerializedProperty m_StatusManager;

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
