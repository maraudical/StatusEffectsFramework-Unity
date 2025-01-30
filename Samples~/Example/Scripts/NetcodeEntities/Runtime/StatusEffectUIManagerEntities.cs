#if ENTITIES && ADDRESSABLES
using StatusEffects.Example.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.NetCode.Entities.Example.UI
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

        private Dictionary<FixedString64Bytes, StatusEffectUI> m_StatusEffectUIs;
        private Dictionary<FixedString64Bytes, int> m_CurrentStackCounts;
        HashSet<FixedString64Bytes> m_CombinedStatusEffects;

        private EntityManager m_Manager;
        private EntityQuery m_PlayerQuery;
        private EntityQuery m_StatusEffectsQuery;
        private EntityQuery m_StatusReferencesQuery;
        private DynamicBuffer<StatusEffects> m_StatusEffects;
        private StatusReferences m_StatusReferences;
        private BlobAssetReference<StatusEffectData> m_StatusEffectDataReference;

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

        private void Start()
        {
            m_Manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            m_PlayerQuery = m_Manager.CreateEntityQuery(typeof(ExamplePlayer));
            m_StatusEffectsQuery = m_Manager.CreateEntityQuery(typeof(StatusEffects));
            m_StatusReferencesQuery = m_Manager.CreateEntityQuery(typeof(StatusReferences));
        }

        private void Update()
        {
            if (!m_StatusEffectsQuery.TryGetSingletonBuffer(out m_StatusEffects, true))
                return;

            m_StatusReferences = m_StatusReferencesQuery.GetSingleton<StatusReferences>();
            
            m_CurrentStackCounts.Clear();
            
            foreach (var statusEffect in m_StatusEffects)
                if (m_CurrentStackCounts.TryGetValue(statusEffect.Data.Value.Id, out int value))
                    m_CurrentStackCounts[statusEffect.Data.Value.Id] = value + statusEffect.Stacks;
                else
                    m_CurrentStackCounts.Add(statusEffect.Data.Value.Id, statusEffect.Stacks);

            m_CombinedStatusEffects = m_CurrentStackCounts.Keys.Concat(m_StatusEffectUIs.Keys).ToHashSet();
            
            foreach (var id in m_CombinedStatusEffects)
            {
                if (!m_StatusReferences.TryGetReference(id, out m_StatusEffectDataReference))
                    continue;
                
                if (!m_StatusEffectDataReference.Value.Icon.IsValid())
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
                    effectUI.Initialize(m_StatusEffectDataReference.Value.Icon, stacks);
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
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var entity = m_PlayerQuery.GetSingletonEntity();
            commandBuffer.AppendToBuffer(entity, new StatusEffectRequests(m_StatusEffectData.Id));
            commandBuffer.Playback(m_Manager);
            commandBuffer.Dispose();
        }

        private void RemoveButtonClicked()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var entity = m_PlayerQuery.GetSingletonEntity();
            commandBuffer.AppendToBuffer(entity, new StatusEffectRequests(StatusEffectRemovalType.Data, id: m_StatusEffectData.Id, stacks: 1));
            commandBuffer.Playback(m_Manager);
            commandBuffer.Dispose();
        }
    }
}
#endif