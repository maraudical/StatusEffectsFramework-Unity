#if NETCODE
using StatusEffectsFramework.Editor;
using UnityEditor;

namespace StatusEffectsFramework.NetCode.Editor
{
    [CustomPropertyDrawer(typeof(NetworkStatusBool))]
    internal class NetworkStatusBoolDrawer : StatusBoolDrawer { }
}
#endif