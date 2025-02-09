using StatusEffects;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(Name), editorForChildClasses: true)]
    public class NameEditor : Editor
    {
        private SerializedProperty m_Id;

        private void OnEnable()
        {
            m_Id = serializedObject.FindProperty($"m_{nameof(Name.Id)}");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(m_Id);
            EditorGUI.EndDisabledGroup();
        }
    }
}
