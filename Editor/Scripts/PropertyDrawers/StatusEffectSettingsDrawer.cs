using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectSettings))]
    [CanEditMultipleObjects]
    public class StatusEffectSettingsDrawer : Editor
    {
        bool groupFoldout = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var settings = StatusEffectSettings.GetSerializedSettings();
            EditorGUIUtility.labelWidth = 215;
            int defaultIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;
            SerializedProperty property;
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("groupbox");
            property = settings.FindProperty("statuses");
            EditorGUILayout.PropertyField(property, new GUIContent(property.displayName));
            EditorGUILayout.Space();
            groupFoldout = EditorGUILayout.Foldout(groupFoldout, "Groups");
            if (groupFoldout)
            {
                EditorGUI.indentLevel++;
                property = settings.FindProperty("groups");
                for (int i = 0; i < property.arraySize; i++)
                {
                    // draw every element of the array
                    EditorGUILayout.PropertyField(property.GetArrayElementAtIndex(i));
                }
                EditorGUI.indentLevel--;
            }
            GUILayout.EndVertical();
            EditorGUI.indentLevel = defaultIndent;
            settings.ApplyModifiedProperties();
        }
    }
}
