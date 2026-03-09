using StatusEffectFramework.Entities;
using Unity.Entities;
using Hash128 = Unity.Entities.Hash128;

namespace StatusEffectFramework.Samples
{
    // Just adding on the IEntityStatus implemenation to the existing base class.
    public partial class ExamplePlayer : IEntityStatus
    {
        public Hash128 ComponentId => m_ComponentId;
        private Hash128 m_ComponentId = StatusEffectsECSUtility.GenerateBurstId();

        public void OnBake(Entity entity, StatusManagerBaker baker)
        {
            baker.DependsOn(this);
            baker.AppendToBuffer(entity, new StatusFloats(ComponentId, StatusMaxHealth));
            baker.AppendToBuffer(entity, new StatusFloats(ComponentId, StatusSpeed));
            baker.AppendToBuffer(entity, new StatusInts(ComponentId, StatusCoinMultiplier));
            baker.AppendToBuffer(entity, new StatusBools(ComponentId, StatusStunned));
        }

    }
}