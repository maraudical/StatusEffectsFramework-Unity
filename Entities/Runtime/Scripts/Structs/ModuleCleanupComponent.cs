#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public struct ModuleCleanupComponent : ICleanupComponentData 
    {
        /// <summary>
        /// Note that <see cref="Target"/> can be <see cref="Entity.Null"/> since it may 
        /// be being cleanup up due to the <see cref="Target"/> being destroyed first.
        /// </summary>
        public Entity Target;
    }
}
#endif