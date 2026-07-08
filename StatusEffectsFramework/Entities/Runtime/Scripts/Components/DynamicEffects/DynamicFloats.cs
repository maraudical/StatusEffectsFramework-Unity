#if ENTITIES
using System;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    public struct DynamicFloats<T> : IBufferElementData, IComparable<DynamicFloats<T>>, IComparable<uint>, IEquatable<uint> where T : unmanaged
    {
        public uint Id;
        public Hash128 StatusName;
        public ValueModifier ValueModifier;
        public bool PostEvaluate;
        public int Priority;
        public float Value;

        public T Struct;

        public bool Equals(uint other) => Id.Equals(other);
        public int CompareTo(uint other) => Id.CompareTo(other);
        public int CompareTo(DynamicFloats<T> other) => Id.CompareTo(other.Id);
    }
}
#endif