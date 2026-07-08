using UnityEditor;
using StatusEffectFramework.Samples;
using StatusEffectFramework.Samples.Editor;
using UnityEngine.UIElements;

namespace StatusEffectFramework.NetCode.Example.Editor
{
    [CustomEditor(typeof(NetworkExamplePlayer))]
    [CanEditMultipleObjects]
    public class NetworkExamplePlayerEditor : ExamplePlayerEditor { }
}