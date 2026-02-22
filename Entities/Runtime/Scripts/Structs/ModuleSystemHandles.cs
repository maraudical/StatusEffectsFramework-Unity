#if ENTITIES
using System;
using Unity.Entities;

namespace StatusEffects.Entities
{
    internal struct ModuleSystemHandles : IBufferElementData, IEquatable<ModuleSystemRequests>, IEquatable<SystemTypeIndex>
    {
        public TypeIndex ModuleTypeIndex;
        public SystemTypeIndex SystemTypeIndex;
        public SystemHandle Value;
        public double TimeUpdated;

        public bool Equals(ModuleSystemRequests other)
        {
            return Equals(other.SystemTypeIndex);
        }

        public bool Equals(SystemTypeIndex other)
        {
            return other.Equals(SystemTypeIndex);
        }
    }
}
#endif