using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{

    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class StatusEffectSettingsIMGUIRegister
    {
        private static SerializedObject s_Settings;
        private static SerializedProperty s_Groups;
        private static bool s_GroupFoldout = true;
        private static int s_DefaultIndent;

        [SettingsProvider]
        public static SettingsProvider CreateStatusEffectSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/StatusEffectSettings", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "Status Effect Settings",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    s_Settings = StatusEffectSettings.GetSerializedSettings();
                    s_Settings.Update();
                    EditorGUIUtility.labelWidth = 215;
                    s_DefaultIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    EditorGUIUtility.labelWidth = 0;
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical("groupbox");
                    s_Groups = s_Settings.FindProperty(nameof(StatusEffectSettings.Groups));
                    s_GroupFoldout = EditorGUILayout.Foldout(s_GroupFoldout, s_Groups.displayName);
                    if (s_GroupFoldout)
                    {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < s_Groups.arraySize; i++)
                        {
                            // draw every element of the array
                            EditorGUILayout.PropertyField(s_Groups.GetArrayElementAtIndex(i));
                        }
                        EditorGUI.indentLevel--;
                    }
                    GUILayout.EndVertical();
                    EditorGUI.indentLevel = s_DefaultIndent;
                    s_Settings.ApplyModifiedPropertiesWithoutUndo();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Status", "Effect", "Group", "Stack" })
            };

            return provider;
        }
    }
}
