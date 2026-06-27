#if ENTITIES
using System;
using Unity.Burst;
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    [BurstCompile]
    public struct Modules<T> : IBufferElementData, IComparable<Modules<T>>, IComparable<uint>, IEquatable<uint> where T : unmanaged
    {
        public uint Id;
        public T Value;

        public bool Equals(uint other) => Id.Equals(other);
        public int CompareTo(uint other) => Id.CompareTo(other);
        public int CompareTo(Modules<T> other) => Id.CompareTo(other.Id);
    }
}
#endif