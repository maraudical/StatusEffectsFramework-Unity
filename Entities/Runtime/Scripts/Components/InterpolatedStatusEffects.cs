#if ENTITIES && NETCODE
using System;
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffectFramework.Entities
{
    [GhostComponent(PrefabType = GhostPrefabType.Client)]
    internal struct InterpolatedStatusEffects : IBufferElementData, IComparable<InterpolatedStatusEffects>
    {
        public uint Id;
        public Hash128 StatusEffectDataId;
        public int Stacks;

        public int CompareTo(InterpolatedStatusEffects other)
        {
            return Id.CompareTo(other.Id);
        }
    }
}
#endif