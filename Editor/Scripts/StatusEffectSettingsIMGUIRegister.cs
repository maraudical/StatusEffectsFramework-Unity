using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{

    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class StatusEffectSettingsIMGUIRegister
    {
        static bool groupFoldout = true;

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
                    var settings = StatusEffectSettings.GetSerializedSettings();
                    EditorGUIUtility.labelWidth = 215;
                    int defaultIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
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
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Status", "Effect", "Group", "Stacking", "Localization" })
            };

            return provider;
        }
    }
}
