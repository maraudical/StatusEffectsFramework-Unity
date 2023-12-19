using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectSettings))]
    [CanEditMultipleObjects]
    public class StatusEffectSettingsEditor : Editor
    {
        bool groupFoldout = true;

        SerializedProperty property;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUIUtility.labelWidth = 215;
            int defaultIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("groupbox");
            groupFoldout = EditorGUILayout.Foldout(groupFoldout, "Groups");
            if (groupFoldout)
            {
                EditorGUI.indentLevel++;
                property = serializedObject.FindProperty("groups");
                for (int i = 0; i < property.arraySize; i++)
                {
                    // draw every element of the array
                    EditorGUILayout.PropertyField(property.GetArrayElementAtIndex(i));
                }
                EditorGUI.indentLevel--;
            }
            GUILayout.EndVertical();
            EditorGUI.indentLevel = defaultIndent;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
