using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Editor
{

    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class StatusEffectSettingsIMGUIRegister
    {
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
                    EditorGUI.indentLevel = 1;
                    SerializedProperty property;
#if LOCALIZATION_SUPPORT
                    property = settings.FindProperty("disableUnityLocalizationSupport");
                    EditorGUILayout.PropertyField(property, new GUIContent(property.displayName));
#endif
                    EditorGUIUtility.labelWidth = 0;
                    EditorGUI.indentLevel = defaultIndent;
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical("groupbox");
                    property = settings.FindProperty("groups");
                    EditorGUILayout.PropertyField(property, new GUIContent(property.displayName));
                    EditorGUILayout.Space();
                    property = settings.FindProperty("statuses");
                    EditorGUILayout.PropertyField(property, new GUIContent(property.displayName));
                    GUILayout.EndVertical();
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Status", "Effect", "Group", "Stacking", "Localization" })
            };

            return provider;
        }
    }
}
