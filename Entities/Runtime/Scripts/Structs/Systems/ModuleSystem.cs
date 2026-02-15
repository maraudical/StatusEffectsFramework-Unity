#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public partial class ModuleSystem : SystemBase
    {
        /*public EntityQuery m_ReferencesQuery;
        public EntityQuery m_RequestQuery;

        protected override void OnCreate()
        {
            EntityManager.CreateEntity(typeof(StatusReferencesSetupRequest));

            m_ReferencesQuery = SystemAPI.QueryBuilder().WithAll<StatusReferences>().Build();
            m_RequestQuery = SystemAPI.QueryBuilder().WithAll<StatusReferencesSetupRequest>().Build();

            RequireForUpdate(m_RequestQuery);
        }*/

        protected override void OnUpdate()
        {
        }
    }
}
#endif