using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusEffectGroup))]
    public class StatusEffectGroupDrawer : PropertyDrawer
    {
        public VisualTreeAsset VisualTree;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var maskField = root.Q<MaskField>("mask-field");
            var settingsButton = root.Q<Button>("settings-button");

            var valueProperty = property.FindPropertyRelative(nameof(StatusEffectGroup.Value));

            Dictionary<int, string> choices = StatusEffectSettings.GetOrCreateSettings().Groups.Select((g, index)=> new KeyValuePair<int, string>(index, g))
                                                                                               .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                                                                                               .ToDictionary(kvp => 1 << kvp.Key, kvp => kvp.Value);
            maskField.label = property.displayName;
            maskField.choices = choices.Values.ToList();
            maskField.choicesMasks = choices.Keys.ToList();
            maskField.AddToClassList("unity-base-field__aligned");
            maskField.BindProperty(valueProperty);

            settingsButton.iconImage = new Background { texture = EditorGUIUtility.IconContent("_Popup").image as Texture2D };
            settingsButton.clicked += Clicked;

            return root;

            void Clicked()
            {
                Selection.activeObject = StatusEffectSettings.GetOrCreateSettings();
            }
        }
    }
}
