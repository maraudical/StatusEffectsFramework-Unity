using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectSettings))]
    [CanEditMultipleObjects]
    public class StatusEffectSettingsEditor : Editor
    {
        private bool _groupFoldout = true;

        private SerializedProperty _property;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUIUtility.labelWidth = 215;
            int defaultIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("groupbox");
            _groupFoldout = EditorGUILayout.Foldout(_groupFoldout, "Groups");
            if (_groupFoldout)
            {
                EditorGUI.indentLevel++;
                _property = serializedObject.FindProperty("groups");
                for (int i = 0; i < _property.arraySize; i++)
                {
                    // draw every element of the array
                    EditorGUILayout.PropertyField(_property.GetArrayElementAtIndex(i));
                }
                EditorGUI.indentLevel--;
            }
            GUILayout.EndVertical();
            EditorGUI.indentLevel = defaultIndent;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
