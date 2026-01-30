#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace StatusEffects.Example.Editor
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
#endif