#if NETCODE
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// The root entity of the ghost a status manager belongs to. Only added to status managers on ghost
    /// children, since <c>PredictedGhost</c> is only on the root.
    /// </summary>
    internal struct StatusEffectsGhostRoot : IComponentData
    {
        public Entity Root;
    }
}
#endif
