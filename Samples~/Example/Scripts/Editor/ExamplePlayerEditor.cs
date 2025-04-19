#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace StatusEffects.Example.Inspector
{
    [CustomEditor(typeof(ExamplePlayer))]
    [CanEditMultipleObjects]
    public class ExamplePlayerEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return ExamplePlayerInspector.DrawInspector(serializedObject, target as IExamplePlayer);
        }
    }
}
#endif