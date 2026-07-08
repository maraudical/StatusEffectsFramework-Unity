using StatusEffectsFramework.Samples;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Hash128 = Unity.Entities.Hash128;

namespace StatusEffectsFramework.Entities.Samples
{
    // This would be more optimized and scalable from a SystemBase.
    // For simplicity everything is done in this MonoBehaviour.
    public class StatusEffectUIManagerEntities : MonoBehaviour
    {
        [SerializeField] protected Transform m_EffectParent;
        [SerializeField] protected GameObject m_EffectPrefab;
        [SerializeField] protected Dropdown m_EffectDropdown;
        [SerializeField] protected Button m_EffectAddButton;
        [SerializeField] protected Button m_EffectRemoveButton;
        [Space]
        [SerializeField] protected List<StatusEffectData> m_StatusEffectDatas;

        protected StatusEffectData m_StatusEffectData;

        protected Dictionary<Hash128, StatusEffectUI> m_StatusEffectUIs;
        protected Dictionary<Hash128, int> m_CurrentStackCounts;
        protected HashSet<Hash128> m_CombinedStatusEffects;

        protected EntityManager m_Manager;
        protected EntityQuery m_PlayerQuery;
        protected EntityQuery m_StatusEffectsQuery;
        protected EntityQuery m_StatusReferencesQuery;

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

        protected virtual void Start()
        {
            m_Manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            m_PlayerQuery = m_Manager.CreateEntityQuery(typeof(ExamplePlayerComponent));
            m_StatusReferencesQuery = m_Manager.CreateEntityQuery(typeof(StatusReferences));
        }

        protected virtual void Update()
        {
            using var array = m_PlayerQuery.ToEntityArray(Allocator.Temp);

            if (array.Length <= 0)
                return;

            var entity = array[0];

            if (!m_StatusReferencesQuery.TryGetSingleton(out StatusReferences statusReferences))
                return;
            
            m_CurrentStackCounts.Clear();

            var buffer = m_Manager.GetBuffer<StatusEffects>(entity);

            foreach (var statusEffect in buffer)
                if (m_CurrentStackCounts.TryGetValue(statusEffect.StatusEffectDataId, out int value))
                    m_CurrentStackCounts[statusEffect.StatusEffectDataId] = value + statusEffect.Stacks;
                else
                    m_CurrentStackCounts.Add(statusEffect.StatusEffectDataId, statusEffect.Stacks);

            m_CombinedStatusEffects = m_CurrentStackCounts.Keys.Concat(m_StatusEffectUIs.Keys).ToHashSet();
            
            foreach (var id in m_CombinedStatusEffects)
            {
                if (!statusReferences.TryGetReference(id, out var reference))
                    continue;

                ref var statusEffectData = ref reference.Value;

                if (!statusEffectData.Icon.IsValid())
                    continue;

                bool currentExists = m_CurrentStackCounts.TryGetValue(id, out int currentStacks);
                bool statusEffectUIExists = m_StatusEffectUIs.TryGetValue(id, out var statusEffectUI);
                // Check if it got added.
                if (currentExists && !statusEffectUIExists)
                {
                    GameObject effectUIObject = Instantiate(m_EffectPrefab, m_EffectParent);
                    StatusEffectUI effectUI = effectUIObject.GetComponent<StatusEffectUI>();
                    // There is an initialize method to setup the icon and stack count.
                    effectUI.Initialize(statusEffectData.Icon, currentStacks);
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
            if (!m_StatusReferencesQuery.TryGetSingleton(out StatusReferences statusReferences))
                return;

            using var array = m_PlayerQuery.ToEntityArray(Allocator.Temp);

            foreach (var entity in array) 
            {
                var buffer = m_Manager.GetBuffer<StatusEffectRequests>(entity);
                buffer.Add(StatusEffectRequests.Add(m_StatusEffectData.Id));
                if (statusReferences.onlyOne)
                    return;
            }
        }

        protected virtual void RemoveButtonClicked()
        {
            if (!m_StatusReferencesQuery.TryGetSingleton(out StatusReferences statusReferences))
                return;

            using var array = m_PlayerQuery.ToEntityArray(Allocator.Temp);

            foreach (var entity in array)
            {
                var buffer = m_Manager.GetBuffer<StatusEffectRequests>(entity);
                buffer.Add(StatusEffectRequests.RemoveWithStatusEffectDataId(m_StatusEffectData.Id, 1));
                if (statusReferences.onlyOne)
                    return;
            }
        }
    }
}