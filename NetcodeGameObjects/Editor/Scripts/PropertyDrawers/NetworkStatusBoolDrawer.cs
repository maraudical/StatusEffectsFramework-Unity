#if NETCODE
using StatusEffects.Editor;
using UnityEditor;

namespace StatusEffects.NetCode.GameObjects.Editor
{
    [CustomPropertyDrawer(typeof(NetworkStatusBool))]
    internal class NetworkStatusBoolDrawer : StatusBoolDrawer { }
}
#endif