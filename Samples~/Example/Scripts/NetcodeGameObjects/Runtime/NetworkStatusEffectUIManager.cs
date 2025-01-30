#if NETCODE_GAMEOBJECTS && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
#if UNITASK
using Cysharp.Threading.Tasks;
#else
using StatusEffects.Extensions;
#endif
using StatusEffects.Example.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;

namespace StatusEffects.NetCode.GameObjects.Example.UI
{
    public class NetworkStatusEffectUIManager : MonoBehaviour
    {
        [SerializeField] private NetworkStatusManager m_StatusManager;
        [SerializeField] private NetworkExamplePlayer m_ExamplePlayer;
        [SerializeField] private Transform m_EffectParent;
        [SerializeField] private GameObject m_EffectPrefab;
        [SerializeField] private Dropdown m_EffectDropdown;
        [SerializeField] private Button m_EffectAddButton;
        [SerializeField] private Button m_EffectRemoveButton;
        [Space]
        [SerializeField] private List<AssetReferenceT<StatusEffectData>> m_StatusEffectDatas;

        private Dictionary<StatusEffectData, StatusEffectUI> m_StatusEffectUIs;

        private void Awake()
        {
#if UNITASK
            AwakeAsync(destroyCancellationToken).Forget();
#else
            _ = AwakeAsync(destroyCancellationToken);
#endif
        }

        private async
#if UNITASK
            UniTaskVoid
#else
            Awaitable
#endif
            AwakeAsync(CancellationToken token)
        {
            m_StatusEffectUIs = new();

            List<AsyncOperationHandle<StatusEffectData>> handles = new List<AsyncOperationHandle<StatusEffectData>>();

            foreach (var data in m_StatusEffectDatas)
                handles.Add(data.LoadAssetAsync());

#if UNITASK
            await UniTask.WhenAll(handles.Select(h => h.ToUniTask(cancellationToken: token)));
#else
            await AwaitableExtensions.WaitUntil(() => handles.All(h => h.IsDone), token);
#endif

            m_EffectDropdown.AddOptions(handles.Select(h => new Dropdown.OptionData(h.Result.name)).ToList());

            foreach (var handle in handles)
                handle.Release();

            m_ExamplePlayer.StatusEffectData = m_StatusEffectDatas.First();
        }

        private void OnEnable()
        {
            m_StatusManager.OnStatusEffect += OnStatusEffect;

            m_EffectDropdown.onValueChanged.AddListener(DropdownValueChanged);
            m_EffectAddButton.onClick.AddListener(AddButtonClicked);
            m_EffectRemoveButton.onClick.AddListener(RemoveButtonClicked);
        }

        private void OnDisable()
        {
            m_StatusManager.OnStatusEffect -= OnStatusEffect;

            m_EffectDropdown.onValueChanged.RemoveListener(DropdownValueChanged);
            m_EffectAddButton.onClick.RemoveListener(AddButtonClicked);
            m_EffectRemoveButton.onClick.RemoveListener(RemoveButtonClicked);
        }

        private void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int stacks)
        {
            // If there is no icon for the effect we ignore it.
            if (!statusEffect.Data.Icon)
                return;
            // We check the current list of UI elements to see if it exists
            // already, with the objective to update the stack count.
            if (m_StatusEffectUIs.TryGetValue(statusEffect.Data, out StatusEffectUI ui))
            {
                // Add or remove the given stack count.
                ui.UpdateStack(action is StatusEffectAction.AddedStatusEffect or StatusEffectAction.AddedStacks ? stacks : -stacks);
                // If the stack count after removing is below or equal to 0 then remove it.
                if (ui.Stack <= 0)
                    RemoveUI(ui);

                return;
            }
            // Otherwise we will instantiate a new UI prefab to the scene.
            if (action is StatusEffectAction.AddedStatusEffect or StatusEffectAction.AddedStacks)
                AddUI(stacks);

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
            _ = m_StatusManager.AddStatusEffect(m_ExamplePlayer.StatusEffectData);
        }

        private void RemoveButtonClicked()
        {
            _ = m_StatusManager.RemoveStatusEffect(m_ExamplePlayer.StatusEffectData, 1);
        }
    }
}
#endif