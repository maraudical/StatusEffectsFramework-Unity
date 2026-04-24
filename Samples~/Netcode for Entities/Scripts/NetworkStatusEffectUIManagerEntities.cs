using UnityEngine;
using System.Collections;
using Unity.Collections;
using Unity.NetCode;

namespace StatusEffectFramework.Entities.Samples
{
    // This would be more optimized and scalable from a SystemBase.
    // For simplicity everything is done in this MonoBehaviour.
    public class NetworkStatusEffectUIManagerEntities : StatusEffectUIManagerEntities
    {
        public bool kill = false;

        private bool m_Initialized;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        private new IEnumerator Start()
        {
            yield return new WaitUntil(() => ClientServerBootstrap.HasClientWorlds || ClientServerBootstrap.HasServerWorld);

            if (!ClientServerBootstrap.HasServerWorld)
            {
                enabled = false;
                yield break;
            } 

            m_Manager = ClientServerBootstrap.ServerWorld.EntityManager;
            m_Initialized = true;

            m_PlayerQuery = m_Manager.CreateEntityQuery(typeof(ExamplePlayerComponent));
            m_StatusEffectsQuery = m_Manager.CreateEntityQuery(typeof(StatusEffects));
            m_StatusReferencesQuery = m_Manager.CreateEntityQuery(typeof(StatusReferences));
        }

        protected override void Update()
        {
            if (!m_Initialized)
                return;

            if (kill)
                Kill();

            base.Update();
        }

        protected override void AddButtonClicked()
        {
            if (!m_Initialized)
                return;
            
            base.AddButtonClicked();
        }

        protected override void RemoveButtonClicked()
        {
            if (!m_Initialized)
                return;

            base.RemoveButtonClicked();
        }
        
        public void Kill()
        {
            kill = false;
            var manager = ClientServerBootstrap.ServerWorld.EntityManager;
            var playerQuery = manager.CreateEntityQuery(typeof(ExamplePlayerComponent));
            using var array = playerQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in array)
                manager.DestroyEntity(entity);
        }
    }
}