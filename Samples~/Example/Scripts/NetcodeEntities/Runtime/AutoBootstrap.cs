#if NETCODE_ENTITIES
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class AutoBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        AutoConnectPort = 7979;
        return base.Initialize(defaultWorldName);
    }
}
#endif