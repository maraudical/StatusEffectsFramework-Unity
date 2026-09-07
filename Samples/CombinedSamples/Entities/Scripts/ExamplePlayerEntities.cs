using StatusEffectsFramework.Entities;
using StatusEffectsFramework.Entities.Samples;
using Unity.Entities;

namespace StatusEffectsFramework.Samples
{
    // Just adding on the IEntityStatus implemenation to the existing base class.
    public partial class ExamplePlayer : IEntityStatus
    {
        public void OnBake(Entity entity, StatusManagerBaker baker)
        {
            baker.DependsOn(this);

            var typeIndex = TypeManager.GetTypeIndex<ExamplePlayerComponent>();

            baker.AppendToBuffer(entity, new StatusFloats(typeIndex, StatusMaxHealth));
            baker.AppendToBuffer(entity, new StatusFloats(typeIndex, StatusSpeed));
            baker.AppendToBuffer(entity, new StatusInts(typeIndex, StatusCoinMultiplier));
            baker.AppendToBuffer(entity, new StatusBools(typeIndex, StatusStunned));
        }

    }
}