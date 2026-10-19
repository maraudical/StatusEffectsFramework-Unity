using UnityEditor;
using UnityEngine;

namespace StatusEffectsFramework.Editor
{
    public class StatusSettingsWindow : EditorWindow
    {
        [MenuItem("Tools/Status Effects Framework/Settings")]
        public static void OpenStatusSettingsWindow()
        {
            EditorWindow window = GetWindow<StatusSettingsWindow>();
            window.titleContent = new GUIContent("Status Effect Settings");
        }

        private UnityEditor.Editor m_Editor;

        public void CreateGUI()
        {
            m_Editor = UnityEditor.Editor.CreateEditor(StatusSettings.Instance);
            rootVisualElement.Add(m_Editor.CreateInspectorGUI());
            rootVisualElement.style.paddingLeft = 10;
            rootVisualElement.style.paddingRight = 8;
            rootVisualElement.style.paddingBottom = 8;
        }

        private void OnDisable()
        {
            if (m_Editor != null)
                DestroyImmediate(m_Editor);
        }
    }
}
