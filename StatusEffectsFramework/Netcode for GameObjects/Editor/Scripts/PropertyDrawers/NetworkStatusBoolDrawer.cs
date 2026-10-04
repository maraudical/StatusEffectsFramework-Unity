using StatusEffectsFramework.Editor;
using UnityEditor;

namespace StatusEffectsFramework.Netcode.Editor
{
    [CustomPropertyDrawer(typeof(NetworkStatusBool))]
    internal class NetworkStatusBoolDrawer : StatusBoolDrawer { }
}