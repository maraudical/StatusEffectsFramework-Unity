using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectDatabase))]
    [CanEditMultipleObjects]
    public class StatusEffectDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUIUtility.labelWidth = 215;
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("groupbox");
            EditorGUILayout.HelpBox("Do not reset this object using the context menu! It may break status effects!", MessageType.Warning);
            int defaultIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;
            EditorGUI.BeginDisabledGroup(true);
            DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel = defaultIndent;
        }
    }
}
