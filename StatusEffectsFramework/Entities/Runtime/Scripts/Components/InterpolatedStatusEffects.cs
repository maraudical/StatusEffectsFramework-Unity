#if ENTITIES && NETCODE
using System;
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffectsFramework.Entities
{
    [GhostComponent(PrefabType = GhostPrefabType.Client)]
    internal struct InterpolatedStatusEffects : IBufferElementData, IComparable<InterpolatedStatusEffects>
    {
        public uint InstanceId;
        public ushort Id;
        public int Stacks;

        public int CompareTo(InterpolatedStatusEffects other)
        {
            return InstanceId.CompareTo(other.InstanceId);
        }
    }
}
#endif