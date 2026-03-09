using UnityEditor;
using UnityEngine.UIElements;

namespace StatusEffectFramework.Samples.Editor
{
    [CustomEditor(typeof(ExamplePlayer))]
    [CanEditMultipleObjects]
    public class ExamplePlayerEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return ExamplePlayerInspector.DrawInspector(serializedObject, target as IExamplePlayer);
        }
    }
}