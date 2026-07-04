#if NETCODE
using StatusEffectsFramework.Editor;
using UnityEditor;

namespace StatusEffectsFramework.NetCode.Editor
{
    [CustomPropertyDrawer(typeof(NetworkStatusFloat))]
    [CustomPropertyDrawer(typeof(NetworkStatusInt))]
    internal class NetworkStatusNumberDrawer : StatusNumberDrawer { }
}
#endif