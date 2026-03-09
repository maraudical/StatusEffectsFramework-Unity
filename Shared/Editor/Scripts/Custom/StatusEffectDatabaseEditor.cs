using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace StatusEffectFramework.Editor
{
    [CustomEditor(typeof(StatusEffectDatabase))]
    internal class StatusEffectDatabaseEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var statusNameProperty = serializedObject.FindProperty(nameof(StatusEffectDatabase.Values));

            var root = new VisualElement();
            
            var helpBox = new HelpBox() { text = "Do not reset this object using the context menu! It may break status effects!", messageType = HelpBoxMessageType.Warning };
            root.Add(helpBox);

            var values = new PropertyField(serializedObject.FindProperty("Values"));
            values.SetEnabled(false);
            root.Add(values);

            return root;
        }
    }
}
