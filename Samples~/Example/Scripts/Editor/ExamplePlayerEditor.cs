#if UNITY_EDITOR
using UnityEditor;

namespace StatusEffects.Example.Inspector
{
    [CustomEditor(typeof(ExamplePlayer))]
    [CanEditMultipleObjects]
    public class ExamplePlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ExamplePlayerInspector.DrawInspector(base.OnInspectorGUI, target as IExamplePlayer);
        }
    }
}
#endif