#if NETCODE
using StatusEffectFramework.Editor;
using UnityEditor;

namespace StatusEffectFramework.NetCode.Editor
{
    [CustomPropertyDrawer(typeof(NetworkStatusFloat))]
    [CustomPropertyDrawer(typeof(NetworkStatusInt))]
    internal class NetworkStatusNumberDrawer : StatusNumberDrawer { }
}
#endif