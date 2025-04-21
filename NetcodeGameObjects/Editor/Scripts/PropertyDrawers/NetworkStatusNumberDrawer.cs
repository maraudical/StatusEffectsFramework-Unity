#if NETCODE
using StatusEffects.Inspector;
using UnityEditor;

namespace StatusEffects.NetCode.GameObjects.Inspector
{
    [CustomPropertyDrawer(typeof(NetworkStatusFloat))]
    [CustomPropertyDrawer(typeof(NetworkStatusInt))]
    internal class NetworkStatusNumberDrawer : StatusNumberDrawer { }
}
#endif