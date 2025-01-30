#if NETCODE_GAMEOBJECTS && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
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