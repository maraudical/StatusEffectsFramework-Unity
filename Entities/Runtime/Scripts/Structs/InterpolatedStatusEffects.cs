#if ENTITIES && NETCODE
using System;
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    [GhostComponent(PrefabType = GhostPrefabType.Client)]
    internal struct InterpolatedStatusEffects : IBufferElementData, IComparable<InterpolatedStatusEffects>
    {
        public uint Id;
        public int Stacks;

        public int CompareTo(InterpolatedStatusEffects other)
        {
            return Id.CompareTo(other.Id);
        }
    }
}
#endif