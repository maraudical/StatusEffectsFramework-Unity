#if ENTITIES
using StatusEffects.Example.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Hash128 = Unity.Entities.Hash128;
using System.Collections;

#if NETCODE_ENTITIES
using Unity.NetCode;
#endif

namespace StatusEffects.Entities.Example.UI
{
    // This would be more optimized and scalable from a SystemBase.
    // For simplicity everything is done in this MonoBehaviour.
    public class StatusEffectUIManagerEntities : MonoBehaviour
    {
        [SerializeField] private Transform m_EffectParent;
        [SerializeField] private GameObject m_EffectPrefab;
        [SerializeField] private Dropdown m_EffectDropdown;
        [SerializeField] private Button m_EffectAddButton;
        [SerializeField] private Button m_EffectRemoveButton;
        [Space]
        [SerializeField] private List<global::StatusEffects.StatusEffectData> m_StatusEffectDatas;

        private global::StatusEffects.StatusEffectData m_StatusEffectData;

        private Dictionary<Hash128, StatusEffectUI> m_StatusEffectUIs;
        private Dictionary<Hash128, int> m_CurrentStackCounts;
        HashSet<Hash128> m_CombinedStatusEffects;

        private EntityManager m_Manager;
        private EntityQuery m_PlayerQuery;
        private EntityQuery m_StatusEffectsQuery;
        private EntityQuery m_StatusReferencesQuery;
        private DynamicBuffer<StatusEffects> m_StatusEffects;
        private StatusReferences m_StatusReferences;
        private StatusEffectData m_StatusEffectDataReference;
#if NETCODE_ENTITIES

        private bool m_Initialized;
#endif

        private void Awake()
        {
            m_StatusEffectUIs = new();
            m_CurrentStackCounts = new();

            m_EffectDropdown.AddOptions(m_StatusEffectDatas.Select(data => new Dropdown.OptionData(data.name)).ToList());

            m_StatusEffectData = m_StatusEffectDatas.First();
        }

        private void OnEnable()
        {
            m_EffectDropdown.onValueChanged.AddListener(DropdownValueChanged);
            m_EffectAddButton.onClick.AddListener(AddButtonClicked);
            m_EffectRemoveButton.onClick.AddListener(RemoveButtonClicked);
        }

        private void OnDisable()
        {
            m_EffectDropdown.onValueChanged.RemoveListener(DropdownValueChanged);
            m_EffectAddButton.onClick.RemoveListener(AddButtonClicked);
            m_EffectRemoveButton.onClick.RemoveListener(RemoveButtonClicked);
        }

        private
#if NETCODE_ENTITIES
            IEnumerator
#else
            void 
#endif
            Start()
        {
#if NETCODE_ENTITIES
            yield return new WaitUntil(() => ClientServerBootstrap.HasClientWorlds || ClientServerBootstrap.HasServerWorld);

            if (ClientServerBootstrap.HasServerWorld)
                m_Manager = ClientServerBootstrap.ServerWorld.EntityManager;
            else
                m_Manager = ClientServerBootstrap.ClientWorld.EntityManager;

            m_Initialized = true;
#else
            m_Manager = World.DefaultGameObjectInjectionWorld.EntityManager;
#endif
            m_PlayerQuery = m_Manager.CreateEntityQuery(typeof(ExamplePlayer));
            m_StatusEffectsQuery = m_Manager.CreateEntityQuery(typeof(StatusEffects));
            m_StatusReferencesQuery = m_Manager.CreateEntityQuery(typeof(StatusReferences));
        }

        private void Update()
        {
#if NETCODE_ENTITIES
            if (!m_Initialized)
                return;

#endif
            if (!m_StatusEffectsQuery.TryGetSingletonBuffer(out m_StatusEffects, true))
                return;

            if (!m_StatusReferencesQuery.TryGetSingleton(out m_StatusReferences))
                return;
            
            m_CurrentStackCounts.Clear();
            
            foreach (var statusEffect in m_StatusEffects)
                if (m_CurrentStackCounts.TryGetValue(statusEffect.Id, out int value))
                    m_CurrentStackCounts[statusEffect.Id] = value + statusEffect.Stacks;
                else
                    m_CurrentStackCounts.Add(statusEffect.Id, statusEffect.Stacks);

            m_CombinedStatusEffects = m_CurrentStackCounts.Keys.Concat(m_StatusEffectUIs.Keys).ToHashSet();
            
            foreach (var id in m_CombinedStatusEffects)
            {
                if (!m_StatusReferences.TryGetReference(id, out var reference))
                    continue;

                m_StatusEffectDataReference = reference.Value;

                if (!m_StatusEffectDataReference.Icon.IsValid())
                    return;

                bool currentExists = m_CurrentStackCounts.TryGetValue(id, out int currentStacks);
                bool statusEffectUIExists = m_StatusEffectUIs.TryGetValue(id, out var statusEffectUI);
                // Check if it got added.
                if (currentExists && !statusEffectUIExists)
                    AddUI(currentStacks);
                // Check if it got removed.
                else if (!currentExists && statusEffectUIExists)
                    RemoveUI(statusEffectUI);
                // Check if stack updated.
                else if (currentStacks != statusEffectUI.Stacks)
                    statusEffectUI.UpdateStack(currentStacks);

                void AddUI(int stacks)
                {
                    GameObject effectUIObject = Instantiate(m_EffectPrefab, m_EffectParent);
                    StatusEffectUI effectUI = effectUIObject.GetComponent<StatusEffectUI>();
                    // There is an initialize method to setup the icon and stack count.
                    effectUI.Initialize(m_StatusEffectDataReference.Icon, stacks);
                    m_StatusEffectUIs.Add(id, effectUI);
                }

                void RemoveUI(StatusEffectUI ui)
                {
                    m_StatusEffectUIs.Remove(id);
                    Destroy(ui.gameObject);
                }
            }
        }

        private void DropdownValueChanged(int value)
        {
            m_StatusEffectData = m_StatusEffectDatas.ElementAtOrDefault(value);
        }

        private void AddButtonClicked()
        {
#if NETCODE_ENTITIES
            if (!ClientServerBootstrap.HasServerWorld)
                return;
#endif
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var entity = m_PlayerQuery.GetSingletonEntity();
            commandBuffer.AppendToBuffer(entity, new StatusEffectRequests(m_StatusEffectData.Id));
            commandBuffer.Playback(m_Manager);
            commandBuffer.Dispose();
        }

        private void RemoveButtonClicked()
        {
#if NETCODE_ENTITIES
            if (!ClientServerBootstrap.HasServerWorld)
                return;
#endif
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var entity = m_PlayerQuery.GetSingletonEntity();
            commandBuffer.AppendToBuffer(entity, new StatusEffectRequests(StatusEffectRemovalType.Data, id: m_StatusEffectData.Id, stacks: 1));
            commandBuffer.Playback(m_Manager);
            commandBuffer.Dispose();
        }
    }
}
#endif