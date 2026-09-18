#if ENTITIES
using System;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    public struct DynamicBools<T> : IBufferElementData, IComparable<DynamicBools<T>>, IComparable<uint>, IEquatable<uint> where T : unmanaged
    {
        public uint InstanceId;
        public ushort Id;
        public bool PostEvaluate;
        public int Priority;
        public bool Value;

        public T Struct;

        public bool Equals(uint other) => InstanceId.Equals(other);
        public int CompareTo(uint other) => InstanceId.CompareTo(other);
        public int CompareTo(DynamicBools<T> other) => InstanceId.CompareTo(other.InstanceId);
    }
}
#endif