#if ENTITIES
using System;
using Unity.Entities;

namespace StatusEffects.Entities
{
    internal struct ModuleSystems : IBufferElementData, IEquatable<ModuleSystemRequests>
    {
        public ulong StableTypeHash;
        public SystemHandle SystemHandle;
        public double TimeUpdated;
        public uint LastSystemVersion;

        public bool Equals(ModuleSystemRequests other)
        {
            return other.StableTypeHash.Equals(StableTypeHash);
        }
    }
}
#endif