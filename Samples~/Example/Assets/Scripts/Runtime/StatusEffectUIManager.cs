using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.Example.UI
{
    public class StatusEffectUIManager : MonoBehaviour
    {
        [SerializeField] private ExampleEntity _exampleEntity;
        [SerializeField] private Transform _effectParent;
        [SerializeField] private GameObject _effectPrefab;
        [SerializeField] private Dropdown _effectDropdown;
        [SerializeField] private Button _effectAddButton;
        [SerializeField] private Button _effectRemoveButton;
        [Space]
        [SerializeField] private List<StatusEffectData> _statusEffectDatas;

        private Dictionary<StatusEffectData, StatusEffectUI> _statusEffectUIs;

        private void Awake()
        {
            _statusEffectUIs = new();

            _effectDropdown.AddOptions(_statusEffectDatas.Select(data => new Dropdown.OptionData(data.name)).ToList());

            _exampleEntity.statusEffectData = _statusEffectDatas.First();
        }

        private void OnEnable()
        {
            _exampleEntity.onStatusEffect += OnStatusEffect;

            _effectDropdown.onValueChanged.AddListener(DropdownValueChanged);
            _effectAddButton.onClick.AddListener(AddButtonClicked);
            _effectRemoveButton.onClick.AddListener(RemoveButtonClicked);
        }

        private void OnDisable()
        {
            _exampleEntity.onStatusEffect -= OnStatusEffect;

            _effectDropdown.onValueChanged.RemoveListener(DropdownValueChanged);
            _effectAddButton.onClick.RemoveListener(AddButtonClicked);
            _effectRemoveButton.onClick.RemoveListener(RemoveButtonClicked);
        }

        private void OnStatusEffect(StatusEffect statusEffect, bool added, int stacks)
        {
            // If there is no icon for the effect we ignore it.
            if (!statusEffect.data.icon)
                return;
            // We check the current list of UI elements to see if it exists
            // already, with the objective to update the stack count.
            if (_statusEffectUIs.TryGetValue(statusEffect.data, out StatusEffectUI ui))
            {
                // Add or remove the given stack count.
                ui.UpdateStack(added ? stacks : -stacks);
                // If the stack count after removing is below or equal to 0 then remove it.
                if (ui.stack <= 0)
                    RemoveUI(ui);

                return;
            }
            // Otherwise we will instantiate a new UI prefab to the scene.
            if (added)
                AddUI(stacks);

            void AddUI(int stacks)
            {
                GameObject effectUIObject = Instantiate(_effectPrefab, _effectParent);
                StatusEffectUI effectUI = effectUIObject.GetComponent<StatusEffectUI>();
                // There is an initialize method to setup the icon and stack count.
                effectUI.Initialize(statusEffect.data.icon, stacks);
                _statusEffectUIs.Add(statusEffect.data, effectUI);
            }

            void RemoveUI(StatusEffectUI ui)
            {
                _statusEffectUIs.Remove(statusEffect.data);
                Destroy(ui.gameObject);
            }
        }

        private void DropdownValueChanged(int value)
        {
            _exampleEntity.statusEffectData = _statusEffectDatas.ElementAtOrDefault(value);
        }

        private void AddButtonClicked()
        {
            _exampleEntity.AddStatusEffect(_exampleEntity.statusEffectData);
        }

        private void RemoveButtonClicked()
        {
            _exampleEntity.RemoveStatusEffect(_exampleEntity.statusEffectData, 1);
        }
    }

}