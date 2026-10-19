using StatusEffectsFramework.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffectsFramework.Samples
{
    // This would be more optimized and scalable from a SystemBase.
    // For simplicity everything is done in this MonoBehaviour.
    public class StatusEffectUIManagerEntities : MonoBehaviour
    {
        public bool onlyOne = true;
        [SerializeField] protected Transform m_EffectParent;
        [SerializeField] protected GameObject m_EffectPrefab;
        [SerializeField] protected Dropdown m_EffectDropdown;
        [SerializeField] protected Button m_EffectAddButton;
        [SerializeField] protected Button m_EffectRemoveButton;
        [Space]
        [SerializeField] protected List<StatusEffectData> m_StatusEffectDatas;

        protected StatusEffectData m_StatusEffectData;

        protected Dictionary<ushort, StatusEffectUI> m_StatusEffectUIs;
        protected Dictionary<ushort, int> m_CurrentStackCounts;
        protected HashSet<ushort> m_CombinedStatusEffects;

        protected virtual void Awake()
        {
            m_StatusEffectUIs = new();
            m_CurrentStackCounts = new();

            m_EffectDropdown.AddOptions(m_StatusEffectDatas.Select(data => new Dropdown.OptionData(data.name)).ToList());

            m_StatusEffectData = m_StatusEffectDatas.First();
        }

        protected virtual void OnEnable()
        {
            m_EffectDropdown.onValueChanged.AddListener(DropdownValueChanged);
            m_EffectAddButton.onClick.AddListener(AddButtonClicked);
            m_EffectRemoveButton.onClick.AddListener(RemoveButtonClicked);
        }

        protected virtual void OnDisable()
        {
            m_EffectDropdown.onValueChanged.RemoveListener(DropdownValueChanged);
            m_EffectAddButton.onClick.RemoveListener(AddButtonClicked);
            m_EffectRemoveButton.onClick.RemoveListener(RemoveButtonClicked);
        }

        /// <summary>
        /// The world the UI displays. It is looked up every time instead of being stored, along with
        /// the EntityManager and queries, since they become invalid when the world is disposed, such
        /// as on a domain reload or when exiting play mode.
        /// </summary>
        protected virtual World GetWorld() => World.DefaultGameObjectInjectionWorld;

        protected virtual void Update()
        {
            var world = GetWorld();

            if (world == null || !world.IsCreated)
                return;

            var manager = world.EntityManager;

            if (!TryGetRegistry(manager, out var registry))
                return;

            using var playerQuery = manager.CreateEntityQuery(typeof(ExamplePlayerComponent), typeof(StatusEffects));
            using var array = playerQuery.ToEntityArray(Allocator.Temp);

            if (array.Length <= 0)
                return;

            var entity = array[0];

            m_CurrentStackCounts.Clear();

            var buffer = manager.GetBuffer<StatusEffects>(entity);

            foreach (var statusEffect in buffer)
                if (m_CurrentStackCounts.TryGetValue(statusEffect.Id, out int value))
                    m_CurrentStackCounts[statusEffect.Id] = value + statusEffect.Stacks;
                else
                    m_CurrentStackCounts.Add(statusEffect.Id, statusEffect.Stacks);

            m_CombinedStatusEffects = m_CurrentStackCounts.Keys.Concat(m_StatusEffectUIs.Keys).ToHashSet();
            
            foreach (var id in m_CombinedStatusEffects)
            {
                ref var data = ref registry.GetStatusEffectDataOrNullRef(id, out bool exists);

                if (!exists || !data.Icon.IsValid())
                    continue;

                bool currentExists = m_CurrentStackCounts.TryGetValue(id, out int currentStacks);
                bool statusEffectUIExists = m_StatusEffectUIs.TryGetValue(id, out var statusEffectUI);
                // Check if it got added.
                if (currentExists && !statusEffectUIExists)
                {
                    GameObject effectUIObject = Instantiate(m_EffectPrefab, m_EffectParent);
                    StatusEffectUI effectUI = effectUIObject.GetComponent<StatusEffectUI>();
                    // There is an initialize method to setup the icon and stack count.
                    effectUI.Initialize(data.Icon, currentStacks);
                    m_StatusEffectUIs.Add(id, effectUI);
                }
                // Check if it got removed.
                else if (!currentExists && statusEffectUIExists)
                {
                    m_StatusEffectUIs.Remove(id);
                    Destroy(statusEffectUI.gameObject);
                }
                // Check if stack updated.
                else if (currentStacks != statusEffectUI.Stacks)
                    statusEffectUI.UpdateStack(currentStacks);
            }
        }

        protected virtual void DropdownValueChanged(int value)
        {
            m_StatusEffectData = m_StatusEffectDatas.ElementAtOrDefault(value);
        }

        protected virtual void AddButtonClicked()
        {
            AddRequest(id => StatusEffectRequests.Add(id));
        }

        protected virtual void RemoveButtonClicked()
        {
            AddRequest(id => StatusEffectRequests.RemoveWithId(id, 1));
        }

        protected virtual void AddRequest(Func<ushort, StatusEffectRequests> createRequest)
        {
            var world = GetWorld();

            if (world != null && world.IsCreated)
                AddRequest(world.EntityManager, createRequest);
        }

        protected void AddRequest(EntityManager manager, Func<ushort, StatusEffectRequests> createRequest)
        {
            // Ids are resolved per world since each world has its own registry.
            if (!TryGetRegistry(manager, out var registry)
                || !registry.TryGetId(m_StatusEffectData.GetKeyHash(), out var id))
                return;

            using var playerQuery = manager.CreateEntityQuery(typeof(ExamplePlayerComponent), typeof(StatusEffectRequests));
            using var array = playerQuery.ToEntityArray(Allocator.Temp);

            foreach (var entity in array)
            {
                var buffer = manager.GetBuffer<StatusEffectRequests>(entity);
                buffer.Add(createRequest(id));
                if (onlyOne)
                    return;
            }
        }

        protected static bool TryGetRegistry(EntityManager manager, out UnmanagedStatusRegistry registry)
        {
            using var registryQuery = manager.CreateEntityQuery(typeof(UnmanagedStatusRegistry));
            return registryQuery.TryGetSingleton(out registry);
        }
    }
}
