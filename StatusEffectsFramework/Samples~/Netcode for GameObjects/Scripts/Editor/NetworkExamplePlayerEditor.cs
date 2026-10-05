using UnityEditor;
using UnityEngine.UIElements;

namespace StatusEffectsFramework.Samples
{
    [CustomEditor(typeof(NetworkExamplePlayer))]
    [CanEditMultipleObjects]
    public class NetworkExamplePlayerEditor : ExamplePlayerEditor { }
}