#if ENTITIES
using System;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    public struct DynamicFloats<T> : IBufferElementData, IComparable<DynamicFloats<T>>, IComparable<uint>, IEquatable<uint> where T : unmanaged
    {
        public uint InstanceId;
        public ushort Id;
        public ValueModifier ValueModifier;
        public bool PostEvaluate;
        public int Priority;
        public float Value;

        public T Struct;

        public bool Equals(uint other) => InstanceId.Equals(other);
        public int CompareTo(uint other) => InstanceId.CompareTo(other);
        public int CompareTo(DynamicFloats<T> other) => InstanceId.CompareTo(other.InstanceId);
    }
}
#endif