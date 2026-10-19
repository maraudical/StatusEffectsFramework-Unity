using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffectsFramework.Samples
{
    // This would be more optimized and scalable from a SystemBase.
    // For simplicity everything is done in this MonoBehaviour.
    public class NetworkStatusEffectUIManagerEntities : StatusEffectUIManagerEntities
    {
        public bool kill = false;

        // Only the instance hosting the server displays and changes status effects.
        protected override World GetWorld() => ClientServerBootstrap.ServerWorld;

        protected override void Update()
        {
            if (kill)
                Kill();

            base.Update();
        }

        protected override void AddButtonClicked()
        {
            if (!ClientServerBootstrap.HasServerWorld)
                return;

            base.AddButtonClicked();
        }

        protected override void RemoveButtonClicked()
        {
            if (!ClientServerBootstrap.HasServerWorld)
                return;

            base.RemoveButtonClicked();
        }

        public void Kill()
        {
            kill = false;

            var world = GetWorld();

            if (world == null || !world.IsCreated)
                return;

            var manager = world.EntityManager;
            using var playerQuery = manager.CreateEntityQuery(typeof(ExamplePlayerComponent));
            manager.DestroyEntity(playerQuery);
        }
    }
}
