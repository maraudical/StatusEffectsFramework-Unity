#if ENTITIES && ADDRESSABLES
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif

namespace StatusEffects.NetCode.Entities
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class StatusEffectAddressableLabels
    {
        public const string StatusEffectData = "Status Effect Data";
#if UNITY_EDITOR
        static StatusEffectAddressableLabels()
        {
            if (!AddressableAssetSettingsDefaultObject.SettingsExists)
                return;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.AddLabel(StatusEffectData);
        }
#endif
    }
}
#endif