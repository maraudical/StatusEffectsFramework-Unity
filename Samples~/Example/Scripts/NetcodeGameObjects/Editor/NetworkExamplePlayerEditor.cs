#if NETCODE_GAMEOBJECTS && COLLECTIONS
using UnityEditor;
using StatusEffects.Example;
using StatusEffects.Example.Inspector;

namespace StatusEffects.NetCode.GameObjects.Example.Inspector
{
    [CustomEditor(typeof(NetworkExamplePlayer))]
    [CanEditMultipleObjects]
    public class NetworkExamplePlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ExamplePlayerInspector.DrawInspector(base.OnInspectorGUI, target as IExamplePlayer);
        }
    }
}
#endif