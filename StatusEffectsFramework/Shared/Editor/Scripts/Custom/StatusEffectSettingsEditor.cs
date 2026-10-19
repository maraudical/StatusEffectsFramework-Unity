using StatusEffectsFramework.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static StatusEffectsFramework.Editor.ModulePopup;
using static StatusEffectsFramework.Editor.StatusNamePopup;
using static StatusEffectsFramework.Editor.DynamicEffectPopup;

namespace StatusEffectsFramework.Editor
{
    public class StatusNamePopup : PopupWindowContent
    {
        public event Action<StatusNameSelection> SelectionType;

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();

            var floatButton = new Button(() => SelectionType(StatusNameSelection.Float));
            floatButton.text = "Float";
            root.Add(floatButton);

            var intButton = new Button(() => SelectionType(StatusNameSelection.Int));
            intButton.text = "Int";
            root.Add(intButton);

            var boolButton = new Button(() => SelectionType(StatusNameSelection.Bool));
            boolButton.text = "Bool";
            root.Add(boolButton);

            return root;
        }

        public enum StatusNameSelection
        {
            Float,
            Int,
            Bool,
        }
    }

    public class ModulePopup : PopupWindowContent
    {
        public event Action<ModuleSelection> SelectionType;
        public Button ModuleButton;

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();

            ModuleButton = new Button(() => SelectionType(ModuleSelection.Module)) { text = "Module" };
            ModuleButton.SetEnabled(TypeCache.GetTypesDerivedFrom<Module>()
                .Any(type => !type.IsAbstract && !type.ContainsGenericParameters));
            root.Add(ModuleButton);

            var moduleScriptButton = new Button(() => SelectionType(ModuleSelection.ModuleScript)) { text = "Module Script" };
            root.Add(moduleScriptButton);

            var moduleInstanceScript = new Button(() => SelectionType(ModuleSelection.ModuleInstanceScript)) { text = "Module Instance Script" };
            root.Add(moduleInstanceScript);

            return root;
        }

        public enum ModuleSelection
        {
            Module,
            ModuleScript,
            ModuleInstanceScript
        }
    }

    public class ModuleTypePopup : PopupWindowContent
    {
        public event Action<Type> SelectionType;

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();
            
            var types = TypeCache.GetTypesDerivedFrom<Module>()
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters);
            foreach (var type in types)
            {
                var moduleButton = new Button(() => SelectionType(type)) { text = type.Name };
                root.Add(moduleButton);
            }

            return root;
        }
    }

    public class DynamicEffectPopup : PopupWindowContent
    {
        public event Action<DynamicEffectSelection> SelectionType;
        public Button DynamicEffectButton;

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();

            DynamicEffectButton = new Button(() => SelectionType(DynamicEffectSelection.DynamicEffect)) { text = "Dynamic Effect" };
            DynamicEffectButton.SetEnabled(TypeCache.GetTypesDerivedFrom<DynamicEffect>()
                .Any(type => !type.IsAbstract && !type.ContainsGenericParameters));
            root.Add(DynamicEffectButton);

            var dynamicEffectFloatScriptButton = new Button(() => SelectionType(DynamicEffectSelection.DynamicEffectFloatScript)) { text = "Dynamic Effect Float Script" };
            root.Add(dynamicEffectFloatScriptButton);

            var dynamicEffectIntScriptButton = new Button(() => SelectionType(DynamicEffectSelection.DynamicEffectIntScript)) { text = "Dynamic Effect Int Script" };
            root.Add(dynamicEffectIntScriptButton);

            var dynamicEffectBoolScriptButton = new Button(() => SelectionType(DynamicEffectSelection.DynamicEffectBoolScript)) { text = "Dynamic Effect Bool Script" };
            root.Add(dynamicEffectBoolScriptButton);

            return root;
        }

        public enum DynamicEffectSelection
        {
            DynamicEffect,
            DynamicEffectFloatScript,
            DynamicEffectIntScript,
            DynamicEffectBoolScript
        }
    }

    public class DynamicEffectTypePopup : PopupWindowContent
    {
        public event Action<Type> SelectionType;

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();

            var types = TypeCache.GetTypesDerivedFrom<DynamicEffect>()
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters);
            foreach (var type in types)
            {
                var moduleButton = new Button(() => SelectionType(type)) { text = type.Name };
                root.Add(moduleButton);
            }

            return root;
        }
    }

    [CustomEditor(typeof(StatusSettings))]
    [CanEditMultipleObjects]
    internal class StatusEffectSettingsEditor : UnityEditor.Editor
    {
        public VisualTreeAsset StatusEffectSettingsVisualTree;
        public VisualTreeAsset StatusEffectSettingsDataVisualTree;
        public VisualTreeAsset StatusEffectSettingsNameVisualTree;
        public VisualTreeAsset StatusEffectSettingsScriptTypeVisualTree;

        public override VisualElement CreateInspectorGUI()
        {
            var groupsProperty = serializedObject.FindProperty(nameof(StatusSettings.Groups));
            var defaultKeyNamespaceProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultKeyNamespace));
            var defaultStatusDataPathProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultStatusDataPath));
            var defaultStatusNamesPathProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultStatusNamesPath));
            var defaultComparableNamesPathProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultComparableNamesPath));
            var defaultModulesPathProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultModulesPath));
            var defaultDynamicEffectsPathProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultDynamicEffectsPath));

            var root = new VisualElement();
            root.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
#if UNITY_2023_1_OR_NEWER

            StatusEffectSettingsVisualTree.CloneTree(root);
            root.Q(className: TabView.headerContainerClassName).style.flexGrow = 1;

            var tabView = root.Q<TabView>();

            var groupTab = root.Q<Tab>("groups-tab");
            groupTab.iconImage = EditorGUIUtility.IconContent("VerticalLayoutGroup Icon").image as Texture2D;
            var groupHeader = groupTab.tabHeader;
            groupHeader.style.flexGrow = 1;
            groupHeader.style.justifyContent = Justify.Center;

            var datasTab = root.Q<Tab>("datas-tab");
            datasTab.iconImage = AssetPreview.GetMiniTypeThumbnail(typeof(StatusEffectData));
            var dataHeader = datasTab.tabHeader;
            dataHeader.style.flexGrow = 1;
            dataHeader.style.justifyContent = Justify.Center;

            var namesTab = root.Q<Tab>("names-tab");
            namesTab.iconImage = AssetPreview.GetMiniTypeThumbnail(typeof(StatusName));
            var namesHeader = namesTab.tabHeader;
            namesHeader.style.flexGrow = 1;
            namesHeader.style.justifyContent = Justify.Center;

            var comparablesTab = root.Q<Tab>("comparables-tab");
            comparablesTab.iconImage = AssetPreview.GetMiniTypeThumbnail(typeof(ComparableName));
            var comparablesHeader = comparablesTab.tabHeader;
            comparablesHeader.style.flexGrow = 1;
            comparablesHeader.style.justifyContent = Justify.Center;

            var modulesTab = root.Q<Tab>("modules-tab");
            modulesTab.iconImage = AssetPreview.GetMiniTypeThumbnail(typeof(Module));
            var modulesHeader = modulesTab.tabHeader;
            modulesHeader.style.flexGrow = 1;
            modulesHeader.style.justifyContent = Justify.Center;

            var dynamicEffectsTab = root.Q<Tab>("dynamic-effects-tab");
            dynamicEffectsTab.iconImage = AssetPreview.GetMiniTypeThumbnail(typeof(DynamicEffect));
            var dynamicEffectsHeader = dynamicEffectsTab.tabHeader;
            dynamicEffectsHeader.style.flexGrow = 1;
            dynamicEffectsHeader.style.justifyContent = Justify.Center;

            var pathsTab = root.Q<Tab>("paths-tab");
            pathsTab.iconImage = EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D;
            var pathsHeader = pathsTab.tabHeader;
            pathsHeader.style.flexGrow = 1;
            pathsHeader.style.justifyContent = Justify.Center;

            #region Sections
            var datasSection = new AssetListSection<StatusEffectData>(tabView, datasTab, StatusEffectSettingsDataVisualTree)
            {
                UpdateIcon = (icon, data) => SetDataIcon(icon, data, data.Icon),
                CreateDetails = CreateDataDetails,
            };
            datasSection.OnAdd = () =>
            {
                var path = SaveAssetPanel("Creating new StatusEffectData", "Enter the name of the new StatusEffectData", defaultStatusDataPathProperty.stringValue, "Status Effect Data path");
                if (string.IsNullOrEmpty(path))
                    return;
                var data = CreateInstance<StatusEffectData>();
                AssetDatabase.CreateAsset(data, path);
                datasSection.AddCreatedAsset(data);
            };

            var namesSection = new AssetListSection<StatusName>(tabView, namesTab, StatusEffectSettingsNameVisualTree);
            namesSection.OnAdd = () =>
            {
                var popup = new StatusNamePopup();
                popup.SelectionType += OnSelectionType;
                UnityEditor.PopupWindow.Show(namesSection.AddButtonBounds, popup);

                void OnSelectionType(StatusNameSelection selection)
                {
                    var path = SaveAssetPanel("Creating new StatusName", "Enter the name of the new StatusName", defaultStatusNamesPathProperty.stringValue, "Status Names path");
                    if (string.IsNullOrEmpty(path))
                        return;

                    StatusName statusName = selection switch
                    {
                        StatusNameSelection.Int => CreateInstance<StatusNameInt>(),
                        StatusNameSelection.Bool => CreateInstance<StatusNameBool>(),
                        _ => CreateInstance<StatusNameFloat>()
                    };
                    AssetDatabase.CreateAsset(statusName, path);
                    namesSection.AddCreatedAsset(statusName);
                }
            };

            var comparablesSection = new AssetListSection<ComparableName>(tabView, comparablesTab, StatusEffectSettingsNameVisualTree);
            comparablesSection.OnAdd = () =>
            {
                var path = SaveAssetPanel("Creating new ComparableName", "Enter the name of the new ComparableName", defaultComparableNamesPathProperty.stringValue, "Comparable Names path");
                if (string.IsNullOrEmpty(path))
                    return;

                var comparable = CreateInstance<ComparableName>();
                AssetDatabase.CreateAsset(comparable, path);
                comparablesSection.AddCreatedAsset(comparable);
            };

            var modulesSection = new AssetListSection<Module>(tabView, modulesTab, StatusEffectSettingsScriptTypeVisualTree)
            {
                GetSubtext = module => module.GetType().ToString(),
                HasDetails = InspectorFields.Any,
                CreateDetails = (module, _) => InspectorFields.Create(module),
            };
            modulesSection.OnAdd = () =>
            {
                var popup = new ModulePopup();
                popup.SelectionType += OnSelectionType;
                UnityEditor.PopupWindow.Show(modulesSection.AddButtonBounds, popup);

                void OnSelectionType(ModuleSelection selection)
                {
                    if (selection is ModuleSelection.Module)
                    {
                        popup.editorWindow.Close();
                        var typePopup = new ModuleTypePopup();
                        typePopup.SelectionType += OnModuleType;
                        UnityEditor.PopupWindow.Show(popup.ModuleButton.worldBound, typePopup);

                        void OnModuleType(Type type)
                        {
                            var path = SaveAssetPanel("Creating new Module", "Enter the name of the new Module", defaultModulesPathProperty.stringValue, "Modules path");
                            if (string.IsNullOrEmpty(path))
                                return;
                            var module = CreateInstance(type);
                            AssetDatabase.CreateAsset(module, path);
                            modulesSection.AddCreatedAsset(module as Module);
                        }
                    }
                    else
                    {
                        var typeName = selection is ModuleSelection.ModuleInstanceScript ? nameof(ModuleInstance) : nameof(Module);
                        var path = EditorUtility.SaveFilePanelInProject($"Creating new {typeName}", "", "cs", $"Enter the name of the new {typeName}");
                        if (string.IsNullOrEmpty(path))
                            return;
                        string directoryPath = Path.GetDirectoryName(path);
                        string enteredName = Path.GetFileNameWithoutExtension(path);
                        string cleanedEnteredNamed = enteredName.Replace(" ", "");
                        var content = selection switch
                        {
                            ModuleSelection.ModuleInstanceScript => StatusEffectScriptTemplates.ModuleInstanceScriptContent,
                            _ =>
#if ENTITIES
                                StatusEffectScriptTemplates.EntityModuleScriptContent
#else
                                StatusEffectScriptTemplates.ModuleScriptContent
#endif

                        };
                        var script = StatusEffectScriptTemplates.CreateScriptAssetFromContent(content, Path.Combine(directoryPath, cleanedEnteredNamed + ".cs"), enteredName, cleanedEnteredNamed);
                        EditorGUIUtility.PingObject(script);
                    }
                }
            };

            var dynamicEffectsSection = new AssetListSection<DynamicEffect>(tabView, dynamicEffectsTab, StatusEffectSettingsScriptTypeVisualTree)
            {
                GetSubtext = dynamicEffect => dynamicEffect.GetType().ToString(),
                HasDetails = InspectorFields.Any,
                CreateDetails = (dynamicEffect, _) => InspectorFields.Create(dynamicEffect),
            };
            dynamicEffectsSection.OnAdd = () =>
            {
                var popup = new DynamicEffectPopup();
                popup.SelectionType += OnSelectionType;
                UnityEditor.PopupWindow.Show(dynamicEffectsSection.AddButtonBounds, popup);

                void OnSelectionType(DynamicEffectSelection selection)
                {
                    if (selection is DynamicEffectSelection.DynamicEffect)
                    {
                        popup.editorWindow.Close();
                        var typePopup = new DynamicEffectTypePopup();
                        typePopup.SelectionType += OnDynamicEffectType;
                        UnityEditor.PopupWindow.Show(popup.DynamicEffectButton.worldBound, typePopup);

                        void OnDynamicEffectType(Type type)
                        {
                            var path = SaveAssetPanel("Creating new Dynamic Effect", "Enter the name of the new Dynamic Effect", defaultDynamicEffectsPathProperty.stringValue, "Dynamic Effects path");
                            if (string.IsNullOrEmpty(path))
                                return;
                            var dynamicEffect = CreateInstance(type);
                            AssetDatabase.CreateAsset(dynamicEffect, path);
                            dynamicEffectsSection.AddCreatedAsset(dynamicEffect as DynamicEffect);
                        }
                    }
                    else
                    {
                        var typeName = selection switch
                        {
                            DynamicEffectSelection.DynamicEffectIntScript => nameof(DynamicEffectInt),
                            DynamicEffectSelection.DynamicEffectBoolScript => nameof(DynamicEffectBool),
                            _ => nameof(DynamicEffectFloat)
                        };
                        var path = EditorUtility.SaveFilePanelInProject($"Creating new {typeName}", "", "cs", $"Enter the name of the new {typeName}");
                        if (string.IsNullOrEmpty(path))
                            return;
                        string directoryPath = Path.GetDirectoryName(path);
                        string enteredName = Path.GetFileNameWithoutExtension(path);
                        string cleanedEnteredNamed = enteredName.Replace(" ", "");
                        var content = selection switch
                        {
                            DynamicEffectSelection.DynamicEffectIntScript =>
#if ENTITIES
                            StatusEffectScriptTemplates.EntityDynamicEffectIntScriptContent,
#else
                            StatusEffectScriptTemplates.DynamicEffectIntScriptContent,
#endif
                            DynamicEffectSelection.DynamicEffectBoolScript =>
#if ENTITIES
                            StatusEffectScriptTemplates.EntityDynamicEffectBoolScriptContent,
#else
                            StatusEffectScriptTemplates.DynamicEffectBoolScriptContent,
#endif
                            _ =>
#if ENTITIES
                            StatusEffectScriptTemplates.EntityDynamicEffectFloatScriptContent
#else
                            StatusEffectScriptTemplates.DynamicEffectFloatScriptContent
#endif
                        };
                        var script = StatusEffectScriptTemplates.CreateScriptAssetFromContent(content, Path.Combine(directoryPath, cleanedEnteredNamed + ".cs"), enteredName, cleanedEnteredNamed);
                        EditorGUIUtility.PingObject(script);
                    }
                }
            };
            #endregion

            var pathsBox = new VisualElement();
            pathsBox.style.borderTopRightRadius = 0;
            pathsBox.style.borderTopLeftRadius = 0;
            pathsBox.style.paddingTop = 2;
            pathsBox.style.paddingBottom = 2;
            pathsBox.AddToClassList(StatusEffectsStyleSheet.BoxGroupClassName);
            pathsTab.Add(pathsBox);

            var defaultKeyNamespace = new PropertyField();
            defaultKeyNamespace.BindProperty(defaultKeyNamespaceProperty);
            pathsBox.Add(defaultKeyNamespace);

            var defaultStatusDataPathGroup = new VisualElement();
            defaultStatusDataPathGroup.style.flexDirection = FlexDirection.Row;
            pathsBox.Add(defaultStatusDataPathGroup);
            var defaultStatusDataPath = new PropertyField();
            defaultStatusDataPath.style.flexGrow = 1;
            defaultStatusDataPath.BindProperty(defaultStatusDataPathProperty);
            defaultStatusDataPathGroup.Add(defaultStatusDataPath);
            var defaultStatusDataPathButton = MakePathButton(defaultStatusDataPathProperty);
            defaultStatusDataPathGroup.Add(defaultStatusDataPathButton);

            var defaultStatusNamesPathGroup = new VisualElement();
            defaultStatusNamesPathGroup.style.flexDirection = FlexDirection.Row;
            pathsBox.Add(defaultStatusNamesPathGroup);
            var defaultStatusNamesPath = new PropertyField();
            defaultStatusNamesPath.style.flexGrow = 1;
            defaultStatusNamesPath.BindProperty(defaultStatusNamesPathProperty);
            defaultStatusNamesPathGroup.Add(defaultStatusNamesPath);
            var defaultStatusNamesPathButton = MakePathButton(defaultStatusNamesPathProperty);
            defaultStatusNamesPathGroup.Add(defaultStatusNamesPathButton);

            var defaultComparableNamesPathGroup = new VisualElement();
            defaultComparableNamesPathGroup.style.flexDirection = FlexDirection.Row;
            pathsBox.Add(defaultComparableNamesPathGroup);
            var defaultComparableNamesPath = new PropertyField();
            defaultComparableNamesPath.style.flexGrow = 1;
            defaultComparableNamesPath.BindProperty(defaultComparableNamesPathProperty);
            defaultComparableNamesPathGroup.Add(defaultComparableNamesPath);
            var defaultComparableNamesPathButton = MakePathButton(defaultComparableNamesPathProperty);
            defaultComparableNamesPathGroup.Add(defaultComparableNamesPathButton);

            var defaultModulesPathGroup = new VisualElement();
            defaultModulesPathGroup.style.flexDirection = FlexDirection.Row;
            pathsBox.Add(defaultModulesPathGroup);
            var defaultModulesPath = new PropertyField();
            defaultModulesPath.style.flexGrow = 1;
            defaultModulesPath.BindProperty(defaultModulesPathProperty);
            defaultModulesPathGroup.Add(defaultModulesPath);
            var defaultModulesPathButton = MakePathButton(defaultModulesPathProperty);
            defaultModulesPathGroup.Add(defaultModulesPathButton);

            var defaultDynamicEffectsPathGroup = new VisualElement();
            defaultDynamicEffectsPathGroup.style.flexDirection = FlexDirection.Row;
            pathsBox.Add(defaultDynamicEffectsPathGroup);
            var defaultDynamicEffectsPath = new PropertyField();
            defaultDynamicEffectsPath.style.flexGrow = 1;
            defaultDynamicEffectsPath.BindProperty(defaultDynamicEffectsPathProperty);
            defaultDynamicEffectsPathGroup.Add(defaultDynamicEffectsPath);
            var defaultDynamicEffectsPathButton = MakePathButton(defaultDynamicEffectsPathProperty);
            defaultDynamicEffectsPathGroup.Add(defaultDynamicEffectsPathButton);

            Button MakePathButton(SerializedProperty property)
            {
                var button = new Button(EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D, () =>
                {
                    var selected = EditorUtility.OpenFolderPanel("Select Path", "Assets", string.Empty);
                    if (string.IsNullOrWhiteSpace(selected))
                        return;
                    if (!TryGetPathRelativeToAssets(selected, out var relativePath))
                    {
                        EditorUtility.DisplayDialog("Invalid path", "Please select a folder within the Assets directory.\n", "Ok");
                        return;
                    }
                    property.stringValue = relativePath;
                    property.serializedObject.ApplyModifiedProperties();
                });
                button.style.width = 19;
                button.style.marginLeft = -2;
                button.style.marginRight = 3;
                button.style.marginBottom = 1;
                button.style.marginTop = 1;
                button.style.paddingBottom = 0;
                button.style.paddingLeft = 0;
                button.style.paddingRight = 2;
                button.style.paddingTop = 0;
                var image = button.Q<Image>();
                image.style.position = Position.Absolute;
                image.style.width = Length.Percent(100);
                image.style.height = Length.Percent(100);
                return button;
            }
#endif

            var groupList = new ListView();
            groupList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            groupList.showBorder = true;
            groupList.selectionType = SelectionType.None;
            groupList.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            groupList.showAddRemoveFooter = false;
            groupList.allowAdd = false;
            groupList.allowRemove = false;
            groupList.showBoundCollectionSize = false;
            groupList.headerTitle = groupsProperty.displayName;
            groupList.viewDataKey = groupsProperty.displayName;
            groupList.makeItem = () => 
            {
                return new PropertyField();
            };
            groupList.bindItem = (existingElement, index) =>
            {
                var propertyField = existingElement as PropertyField;
                propertyField.label = $"Group {index}";
                propertyField.BindProperty(groupsProperty.FindPropertyRelative($"Array.data[{index}]"));
            };
            groupList.BindProperty(groupsProperty);
#if UNITY_2023_1_OR_NEWER
            groupTab.Add(groupList);
#else
            root.Add(groupList);
#endif
            var groupScroll = groupList.Q<ScrollView>();
            groupScroll.style.borderTopLeftRadius = 0;
            groupScroll.style.borderTopRightRadius = 0;

            return root;
        }

        private static void SetDataIcon(Image icon, StatusEffectData data, Sprite sprite)
        {
            if (sprite)
            {
                icon.image = sprite.texture;
                icon.style.opacity = 1f;
            }
            else
            {
                icon.image = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(data));
                icon.style.opacity = 0.2f;
            }
        }

        // The inspector of a status effect data, which keeps the icon and key of its row up to date while it is edited.
        private static ListItemDetails CreateDataDetails(StatusEffectData data, VisualElement row)
        {
            var editor = CreateEditor(data);
            var content = editor.CreateInspectorGUI();
            content.Q<PropertyField>("icon").RegisterValueChangeCallback(evt => SetDataIcon(row.Q<Image>("icon"), data, evt.changedProperty.objectReferenceValue as Sprite));
            content.TrackSerializedObjectValue(editor.serializedObject, _ => row.Q<Label>("subtext").text = data.Key);
            return new ListItemDetails(content, () => DestroyImmediate(editor));
        }

        /// <summary>
        /// Opens a save dialog in the folder from a path setting. The folder is created for the dialog and removed
        /// again if the asset isn't saved in it. Returns null if the dialog was cancelled or the setting is invalid.
        /// </summary>
        private static string SaveAssetPanel(string title, string message, string setting, string settingName)
        {
            if (!TryGetAssetFolder(setting, out var folder))
            {
                EditorUtility.DisplayDialog("Invalid path", $"The {settingName} must be a folder inside the Assets folder, for example ScriptableObjects/Modules.\n", "Ok");
                return null;
            }

            var createdFolder = StatusSettings.EnsureFolder(folder);
            var path = EditorUtility.SaveFilePanelInProject(title, "", "asset", message, folder);

            // Don't leave folders behind that nothing was saved in.
            if (createdFolder != null && (string.IsNullOrEmpty(path) || !path.StartsWith(createdFolder + "/", StringComparison.OrdinalIgnoreCase)))
                AssetDatabase.DeleteAsset(createdFolder);

            return string.IsNullOrEmpty(path) ? null : path;
        }

        /// <summary>
        /// Turns a path setting, which is relative to Assets, into a folder path starting with Assets.
        /// Fails for paths that leave the Assets folder or contain characters a folder name can't have.
        /// </summary>
        private static bool TryGetAssetFolder(string setting, out string folder)
        {
            folder = "Assets";
            if (string.IsNullOrWhiteSpace(setting))
                return true;

            var invalidChars = Path.GetInvalidFileNameChars();
            var segments = new List<string> { "Assets" };
            foreach (var segment in setting.Trim().Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (segment == ".")
                    continue;
                if (segment == ".." || segment.IndexOfAny(invalidChars) >= 0)
                    return false;
                segments.Add(segment);
            }

            folder = string.Join("/", segments);
            return true;
        }

        /// <summary>
        /// Turns a folder on disk into a path relative to the Assets folder, using forward slashes.
        /// Fails if the folder isn't inside Assets. The Assets folder itself gives an empty path.
        /// </summary>
        private static bool TryGetPathRelativeToAssets(string absolutePath, out string relativePath)
        {
            relativePath = null;
            var assets = Application.dataPath.Replace('\\', '/').TrimEnd('/');
            var path = absolutePath.Replace('\\', '/').TrimEnd('/');

            if (path.Equals(assets, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = string.Empty;
                return true;
            }

            if (!path.StartsWith(assets + "/", StringComparison.OrdinalIgnoreCase))
                return false;

            relativePath = path.Substring(assets.Length + 1);
            return true;
        }

        [SettingsProvider]
        public static SettingsProvider CreateStatusEffectSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            UnityEditor.Editor settingsEditor = null;
            var provider = new SettingsProvider("Project/StatusEffectsFramework", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "Status Effects Framework",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = (searchContext, root) =>
                {
                    root.style.paddingLeft = 10;
                    root.style.paddingRight = 8;
                    root.style.paddingBottom = 8;
                    var header = new Label("Status Effects Framework");
                    header.style.marginTop = 2;
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.style.fontSize = 19;
                    root.Add(header);
                    var space = new VisualElement();
                    space.style.minHeight = 8;
                    space.style.flexGrow = 0;
                    root.Add(space);
                    settingsEditor = CreateEditor(StatusSettings.Instance);
                    var settings = settingsEditor.CreateInspectorGUI();
#if UNITY_2023_1_OR_NEWER
                    var tabView = settings.Q<TabView>();
                    tabView.style.overflow = Overflow.Hidden;
                    tabView.style.borderTopLeftRadius = 6;
                    tabView.style.borderTopRightRadius = 6;
#endif
                    root.Add(settings);
                },

                deactivateHandler = () =>
                {
                    if (settingsEditor != null)
                        DestroyImmediate(settingsEditor);
                    settingsEditor = null;
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Status", "Effect", "Group", "Stack", "Framework" })
            };

            return provider;
        }
    }
}
