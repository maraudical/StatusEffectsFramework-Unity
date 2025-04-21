#if NETCODE_GAMEOBJECTS && COLLECTIONS
using UnityEditor;
using StatusEffects.Example;
using StatusEffects.Example.Inspector;
using UnityEngine.UIElements;

namespace StatusEffects.NetCode.GameObjects.Example.Inspector
{
    [CustomEditor(typeof(NetworkExamplePlayer))]
    [CanEditMultipleObjects]
    public class NetworkExamplePlayerEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return ExamplePlayerInspector.DrawInspector(serializedObject, target as IExamplePlayer); ;
        }
    }
}
#endif