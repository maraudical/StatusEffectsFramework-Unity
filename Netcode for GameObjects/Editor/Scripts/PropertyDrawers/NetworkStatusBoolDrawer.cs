#if NETCODE
using StatusEffectFramework.Editor;
using UnityEditor;

namespace StatusEffectFramework.NetCode.Editor
{
    [CustomPropertyDrawer(typeof(NetworkStatusBool))]
    internal class NetworkStatusBoolDrawer : StatusBoolDrawer { }
}
#endif