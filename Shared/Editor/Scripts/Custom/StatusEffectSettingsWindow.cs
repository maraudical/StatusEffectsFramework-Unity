using UnityEditor;
using UnityEngine;

namespace StatusEffects.Editor
{
    public class StatusEffectSettingsWindow : EditorWindow
    {
        [MenuItem("Tools/Status Effect Framework/Settings")]
        public static void OpenStatusEffectSettingsWindow()
        {
            EditorWindow window = GetWindow<StatusEffectSettingsWindow>();
            window.titleContent = new GUIContent("Status Effect Settings");
        }

        public void CreateGUI()
        {
            rootVisualElement.Add(UnityEditor.Editor.CreateEditor(StatusEffectSettings.GetOrCreateSettings()).CreateInspectorGUI());
            rootVisualElement.style.paddingLeft = 10;
            rootVisualElement.style.paddingRight = 8;
            rootVisualElement.style.paddingBottom = 8;
        }
    }
}
