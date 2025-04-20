using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectSettings))]
    [CanEditMultipleObjects]
    internal class StatusEffectSettingsEditor : Editor
    {
        public VisualTreeAsset VisualTree;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);
            
            var groupList = root.Q<ListView>("group-list");

            var groupsProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.Groups));

            groupList.headerTitle = groupsProperty.displayName;
            groupList.viewDataKey = groupsProperty.displayName;
            groupList.makeItem = () => 
            {
                return new PropertyField();
            };
            groupList.bindItem = (existingElement, index) =>
            {
                var propertyField = existingElement as PropertyField;
                propertyField.label = $"Group {index}";
                propertyField.BindProperty(groupsProperty.FindPropertyRelative($"Array.data[{index}]"));
            };
            groupList.BindProperty(groupsProperty);
            

            return root;
        }

        [SettingsProvider]
        public static SettingsProvider CreateStatusEffectSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/StatusEffectSettings", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "Status Effect Settings",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = (searchContext, root) =>
                {
                    var settings = CreateEditor(StatusEffectSettings.GetOrCreateSettings()).CreateInspectorGUI();
                    settings.style.marginLeft = 10;
                    root.Add(settings);
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Status", "Effect", "Group", "Stack" })
            };

            return provider;
        }
    }
}
