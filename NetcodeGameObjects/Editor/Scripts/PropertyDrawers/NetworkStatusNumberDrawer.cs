#if NETCODE
using StatusEffects.Editor;
using UnityEditor;

namespace StatusEffects.NetCode.GameObjects.Editor
{
    [CustomPropertyDrawer(typeof(NetworkStatusFloat))]
    [CustomPropertyDrawer(typeof(NetworkStatusInt))]
    internal class NetworkStatusNumberDrawer : StatusNumberDrawer { }
}
#endif