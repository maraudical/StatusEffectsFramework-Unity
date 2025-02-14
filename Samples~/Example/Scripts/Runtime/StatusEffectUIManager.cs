using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.Example.UI
{
    public class StatusEffectUIManager : MonoBehaviour
    {
        [SerializeField] private StatusManager m_StatusManager;
        [SerializeField] private Transform m_EffectParent;
        [SerializeField] private GameObject m_EffectPrefab;
        [SerializeField] private Dropdown m_EffectDropdown;
        [SerializeField] private Button m_EffectAddButton;
        [SerializeField] private Button m_EffectRemoveButton;
        [Space]
        [SerializeField] private List<StatusEffectData> m_StatusEffectDatas;

        private Dictionary<StatusEffectData, StatusEffectUI> m_StatusEffectUIs;

        private IStatusManager m_ShownStatusManager;
        private IExamplePlayer m_ExamplePlayer;

        private void Awake()
        {
            m_ShownStatusManager = m_StatusManager.GetComponents<IStatusManager>().First((m) => m is Component component && component.hideFlags == HideFlags.None);
            m_ExamplePlayer = m_StatusManager.GetComponent<IExamplePlayer>();

            m_StatusEffectUIs = new();

            m_EffectDropdown.AddOptions(m_StatusEffectDatas.Select(data => new Dropdown.OptionData(data.name)).ToList());

            m_ExamplePlayer.StatusEffectData = m_StatusEffectDatas.First();
        }

        private void OnEnable()
        {
            m_ShownStatusManager.OnStatusEffect += OnStatusEffect;

            m_EffectDropdown.onValueChanged.AddListener(DropdownValueChanged);
            m_EffectAddButton.onClick.AddListener(AddButtonClicked);
            m_EffectRemoveButton.onClick.AddListener(RemoveButtonClicked);
        }

        private void OnDisable()
        {
            m_ShownStatusManager.OnStatusEffect -= OnStatusEffect;

            m_EffectDropdown.onValueChanged.RemoveListener(DropdownValueChanged);
            m_EffectAddButton.onClick.RemoveListener(AddButtonClicked);
            m_EffectRemoveButton.onClick.RemoveListener(RemoveButtonClicked);
        }

        private void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks)
        {
            // If there is no icon for the effect we ignore it.
            if (!statusEffect.Data.Icon)
                return;
            // We check the current list of UI elements to see if it exists
            // already, with the objective to update the stack count.
            if (m_StatusEffectUIs.TryGetValue(statusEffect.Data, out StatusEffectUI statusEffectUI))
            {
                // Add or remove the given stack count.
                statusEffectUI.UpdateStack(currentStacks);
                // If the stack count after removing is below or equal to 0 then remove it.
                if (statusEffectUI.Stacks <= 0)
                    RemoveUI(statusEffectUI);

                return;
            }
            // Otherwise we will instantiate a new UI prefab to the scene.
            if (action is StatusEffectAction.AddedStatusEffect or StatusEffectAction.AddedStacks)
                AddUI(currentStacks);

            void AddUI(int stacks)
            {
                GameObject effectUIObject = Instantiate(m_EffectPrefab, m_EffectParent);
                StatusEffectUI effectUI = effectUIObject.GetComponent<StatusEffectUI>();
                // There is an initialize method to setup the icon and stack count.
                effectUI.Initialize(statusEffect.Data.Icon, stacks);
                m_StatusEffectUIs.Add(statusEffect.Data, effectUI);
            }

            void RemoveUI(StatusEffectUI ui)
            {
                m_StatusEffectUIs.Remove(statusEffect.Data);
                Destroy(ui.gameObject);
            }
        }

        private void DropdownValueChanged(int value)
        {
            m_ExamplePlayer.StatusEffectData = m_StatusEffectDatas.ElementAtOrDefault(value);
        }

        private void AddButtonClicked()
        {
            m_ShownStatusManager.AddStatusEffect(m_ExamplePlayer.StatusEffectData);
        }

        private void RemoveButtonClicked()
        {
            m_ShownStatusManager.RemoveStatusEffect(m_ExamplePlayer.StatusEffectData, 1);
        }
    }

}