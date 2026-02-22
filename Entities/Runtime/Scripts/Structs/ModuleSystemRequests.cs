#if ENTITIES
using System;
using Unity.Entities;

namespace StatusEffects.Entities
{
    internal struct ModuleSystemRequests : IBufferElementData, IEquatable<SystemTypeIndex>
    {
        public TypeIndex ModuleTypeIndex;
        public SystemTypeIndex SystemTypeIndex;

        public bool Equals(SystemTypeIndex other)
        {
            return other.Equals(SystemTypeIndex);
        }
    }
}
#endif