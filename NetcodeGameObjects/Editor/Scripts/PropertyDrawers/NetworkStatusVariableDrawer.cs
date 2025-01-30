#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
using StatusEffects.Inspector;
using UnityEditor;

namespace StatusEffects.NetCode.GameObjects.Inspector
{
    [CustomPropertyDrawer(typeof(NetworkStatusFloat))]
    [CustomPropertyDrawer(typeof(NetworkStatusInt))]
    [CustomPropertyDrawer(typeof(NetworkStatusBool))]
    public class NetworkStatusVariableDrawer : StatusVariableDrawer { }
}
#endif