using StatusEffects.Modules;
using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace StatusEffects.Editor
{
    public class SettingsEventPostProcessor : AssetPostprocessor
    {
        public static event Action OnDatasImported;
        public static event Action OnNamesImported;
        public static event Action OnComparablesImported;
        public static event Action OnModulesImported;

        public static event Action OnAssetRemoved;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            bool datasImported = false;
            bool namesImported = false;
            bool comparablesImported = false;
            bool modulesImported = false;

            foreach (var assetPath in importedAssets)
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                if (asset == null)
                    return;

                switch (asset)
                {
                    case StatusEffectData:
                        datasImported = true;
                        break;
                    case StatusName:
                        namesImported = true;
                        break;
                    case ComparableName:
                        comparablesImported = true;
                        break;
                    case Module:
                        modulesImported = true;
                        break;
                }
            }

            if (deletedAssets != null && deletedAssets.Length > 0) OnAssetRemoved?.Invoke();

            if (datasImported) OnDatasImported?.Invoke();
            if (namesImported) OnNamesImported?.Invoke();
            if (comparablesImported) OnComparablesImported?.Invoke();
            if (modulesImported) OnModulesImported?.Invoke();
        }
    }
}
