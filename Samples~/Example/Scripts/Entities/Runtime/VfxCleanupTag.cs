using Unity.Entities;

namespace StatusEffects.Entities.Example
{
    public struct VfxCleanupTag : ICleanupComponentData 
    {
        public bool InstantiateAgainWhenAddingStacks;
    }
}