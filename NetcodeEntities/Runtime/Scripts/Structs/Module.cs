#if ENTITIES && ADDRESSABLES
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct Module : IComponentData
    {
        public Entity Parent;
        public float BaseValue;
        public int Stacks;
        public int PreviousStacks;
        public bool IsBeingUpdated;
        public bool IsBeingDestroyed;
    }
}
#endif