#if ENTITIES
using System;
using Unity.Burst;
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    [BurstCompile]
    public struct ModuleEvents<T> : IBufferElementData, IEquatable<uint> where T : unmanaged
    {
        public uint Id;
        public StatusEffectEvent Event;

        public bool Equals(uint other) => Id.Equals(other);
    }
}
#endif