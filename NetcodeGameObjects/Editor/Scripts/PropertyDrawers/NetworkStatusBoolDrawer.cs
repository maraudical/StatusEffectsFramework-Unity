#if NETCODE
using StatusEffects.Inspector;
using UnityEditor;

namespace StatusEffects.NetCode.GameObjects.Inspector
{
    [CustomPropertyDrawer(typeof(NetworkStatusBool))]
    internal class NetworkStatusBoolDrawer : StatusBoolDrawer { }
}
#endif