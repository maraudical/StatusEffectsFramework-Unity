using StatusEffectsFramework.Editor;
using UnityEditor;

namespace StatusEffectsFramework.Netcode.Editor
{
    [CustomPropertyDrawer(typeof(NetworkStatusFloat))]
    [CustomPropertyDrawer(typeof(NetworkStatusInt))]
    internal class NetworkStatusNumberDrawer : StatusNumericDrawer { }
}