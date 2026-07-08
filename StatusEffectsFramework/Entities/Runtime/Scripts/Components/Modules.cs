#if ENTITIES
using System;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Modules<T> : IBufferElementData, IComparable<Modules<T>>, IComparable<uint>, IEquatable<uint> where T : unmanaged
    {
        public uint Id;
        public T Struct;

        public bool Equals(uint other) => Id.Equals(other);
        public int CompareTo(uint other) => Id.CompareTo(other);
        public int CompareTo(Modules<T> other) => Id.CompareTo(other.Id);
    }
}
#endif