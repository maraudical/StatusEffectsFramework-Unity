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

            var stableTypeHash = TypeManager.GetTypeInfo<ExamplePlayerComponent>().StableTypeHash;

            baker.AppendToBuffer(entity, new StatusFloats(stableTypeHash, StatusMaxHealth));
            baker.AppendToBuffer(entity, new StatusFloats(stableTypeHash, StatusSpeed));
            baker.AppendToBuffer(entity, new StatusInts(stableTypeHash, StatusCoinMultiplier));
            baker.AppendToBuffer(entity, new StatusBools(stableTypeHash, StatusStunned));
        }

    }
}