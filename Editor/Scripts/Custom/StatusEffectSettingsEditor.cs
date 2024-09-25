using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectSettings))]
    [CanEditMultipleObjects]
    public class StatusEffectSettingsEditor : Editor
    {
        private bool m_GroupFoldout = true;

        private SerializedProperty m_Groups;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUIUtility.labelWidth = 215;
            int defaultIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("groupbox");
            m_Groups = serializedObject.FindProperty(nameof(StatusEffectSettings.Groups));
            m_GroupFoldout = EditorGUILayout.Foldout(m_GroupFoldout, m_Groups.displayName);
            if (m_GroupFoldout)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < m_Groups.arraySize; i++)
                {
                    // draw every element of the array
                    EditorGUILayout.PropertyField(m_Groups.GetArrayElementAtIndex(i));
                }
                EditorGUI.indentLevel--;
            }
            GUILayout.EndVertical();
            EditorGUI.indentLevel = defaultIndent;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
