#if ENTITIES
using System;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    public struct DynamicBools<T> : IBufferElementData, IComparable<DynamicBools<T>>, IComparable<uint>, IEquatable<uint> where T : unmanaged
    {
        public uint Id;
        public Hash128 StatusName;
        public bool PostEvaluate;
        public int Priority;
        public bool Value;

        public T Struct;

        public bool Equals(uint other) => Id.Equals(other);
        public int CompareTo(uint other) => Id.CompareTo(other);
        public int CompareTo(DynamicBools<T> other) => Id.CompareTo(other.Id);
    }
}
#endif