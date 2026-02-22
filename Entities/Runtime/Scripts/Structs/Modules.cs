#if ENTITIES
using Unity.Burst;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [BurstCompile]
    public struct Modules<T> : IBufferElementData where T : unmanaged
    {
        public uint Id;
        public T Value;
    }
}
#endif