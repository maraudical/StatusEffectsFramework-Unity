using StatusEffects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StatusEffects.UI
{
    public class StatusEffectUIManager : MonoBehaviour
    {
        [SerializeField] private ExampleEntity _exampleEntity;
        [SerializeField] private Transform _effectParent;
        [SerializeField] private GameObject _effectPrefab;

        private Dictionary<StatusEffectData, StatusEffectUI> _statusEffectUIs;

        private void Awake()
        {
            _statusEffectUIs = new();
        }

        private void OnEnable()
        {
            _exampleEntity.onStatusEffect += OnStatusEffect;
        }

        private void OnDisable()
        {
            _exampleEntity.onStatusEffect -= OnStatusEffect;
        }

        private void OnStatusEffect(StatusEffect statusEffect, bool added, int stacks)
        {
            if (!statusEffect.data.icon)
                return;

            if (_statusEffectUIs.TryGetValue(statusEffect.data, out StatusEffectUI ui))
            {
                ui.UpdateStack(added ? stacks : -stacks);

                if (ui.stack <= 0)
                    RemoveUI(ui);

                return;
            }

            if (added)
                AddUI(stacks);

            void AddUI(int stacks)
            {
                GameObject effectUIObject = Instantiate(_effectPrefab, _effectParent);
                StatusEffectUI effectUI = effectUIObject.GetComponent<StatusEffectUI>();
                effectUI.Initialize(statusEffect.data.icon, stacks);
                _statusEffectUIs.Add(statusEffect.data, effectUI);
            }

            void RemoveUI(StatusEffectUI ui)
            {
                _statusEffectUIs.Remove(statusEffect.data);
                Destroy(ui.gameObject);
            }
        }
    }

}