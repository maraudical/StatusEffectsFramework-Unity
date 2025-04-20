using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(Name), editorForChildClasses: true)]
    internal class NameEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();

            root.Add(new PropertyField() { bindingPath = $"m_{nameof(Name.Id)}", enabledSelf = false });

            return root;
        }
    }
}
