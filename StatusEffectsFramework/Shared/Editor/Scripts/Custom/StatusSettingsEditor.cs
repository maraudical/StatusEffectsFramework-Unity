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
    [CustomEditor(typeof(StatusSettings))]
    internal class StatusSettingsEditor : UnityEditor.Editor
    {
        public VisualTreeAsset StatusSettingsVisualTree;
        public VisualTreeAsset StatusSettingsDataVisualTree;
        public VisualTreeAsset StatusSettingsScriptTypeVisualTree;

        public override VisualElement CreateInspectorGUI()
        {
            var groupsProperty = serializedObject.FindProperty(nameof(StatusSettings.Groups));
            var defaultKeyNamespaceProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultKeyNamespace));
            var defaultStatusDataPathProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultStatusDataPath));
            var defaultStatusNamesPathProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultStatusNamesPath));
            var defaultComparableNamesPathProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultComparableNamesPath));
            var defaultModulesPathProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultModulesPath));
            var defaultDynamicEffectsPathProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultDynamicEffectsPath));
            var defaultStatusEventsPathProperty = serializedObject.FindProperty(nameof(StatusSettings.DefaultStatusEventsPath));

            var root = new VisualElement();
            root.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);

            StatusSettingsVisualTree.CloneTree(root);
            root.Q(className: TabView.headerContainerClassName).style.flexGrow = 1;

            var tabView = root.Q<TabView>();

            var groupTab = SetUpTab("groups-tab", EditorGUIUtility.IconContent("VerticalLayoutGroup Icon").image as Texture2D);
            var datasTab = SetUpTab("datas-tab", AssetPreview.GetMiniTypeThumbnail(typeof(StatusEffectData)));
            var namesTab = SetUpTab("names-tab", AssetPreview.GetMiniTypeThumbnail(typeof(StatusName)));
            var comparablesTab = SetUpTab("comparables-tab", AssetPreview.GetMiniTypeThumbnail(typeof(ComparableName)));
            var modulesTab = SetUpTab("modules-tab", AssetPreview.GetMiniTypeThumbnail(typeof(Module)));
            var dynamicEffectsTab = SetUpTab("dynamic-effects-tab", AssetPreview.GetMiniTypeThumbnail(typeof(DynamicEffect)));
            var eventsTab = SetUpTab("events-tab", AssetPreview.GetMiniTypeThumbnail(typeof(StatusEvent)));
            var pathsTab = SetUpTab("paths-tab", EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D);

            Tab SetUpTab(string tabName, Texture2D icon)
            {
                var tab = root.Q<Tab>(tabName);
                tab.iconImage = icon;
                var header = tab.tabHeader;
                header.style.flexGrow = 1;
                header.style.justifyContent = Justify.Center;
                return tab;
            }

            #region Sections
            var datasSection = new AssetListSection<StatusEffectData>(tabView, datasTab, StatusSettingsDataVisualTree)
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

            var namesSection = new AssetListSection<StatusName>(tabView, namesTab, StatusSettingsDataVisualTree);
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

            var comparablesSection = new AssetListSection<ComparableName>(tabView, comparablesTab, StatusSettingsDataVisualTree);
            comparablesSection.OnAdd = () =>
            {
                var path = SaveAssetPanel("Creating new ComparableName", "Enter the name of the new ComparableName", defaultComparableNamesPathProperty.stringValue, "Comparable Names path");
                if (string.IsNullOrEmpty(path))
                    return;

                var comparable = CreateInstance<ComparableName>();
                AssetDatabase.CreateAsset(comparable, path);
                comparablesSection.AddCreatedAsset(comparable);
            };

            var modulesSection = new AssetListSection<Module>(tabView, modulesTab, StatusSettingsScriptTypeVisualTree)
            {
                GetSubtext = module => module.GetType().ToString(),
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
                        var typePopup = new TypePopup<Module>();
                        typePopup.SelectionType += OnModuleType;
                        UnityEditor.PopupWindow.Show(modulesSection.AddButtonBounds, typePopup);

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
                        var script = StatusEffectScriptTemplates.CreateScriptAsset(content, path);
                        EditorGUIUtility.PingObject(script);
                    }
                }
            };

            var dynamicEffectsSection = new AssetListSection<DynamicEffect>(tabView, dynamicEffectsTab, StatusSettingsScriptTypeVisualTree)
            {
                GetSubtext = dynamicEffect => dynamicEffect.GetType().ToString(),
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
                        var typePopup = new TypePopup<DynamicEffect>();
                        typePopup.SelectionType += OnDynamicEffectType;
                        UnityEditor.PopupWindow.Show(dynamicEffectsSection.AddButtonBounds, typePopup);

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
                        var script = StatusEffectScriptTemplates.CreateScriptAsset(content, path);
                        EditorGUIUtility.PingObject(script);
                    }
                }
            };

            var statusEventsSection = new AssetListSection<StatusEvent>(tabView, eventsTab, StatusSettingsDataVisualTree);
            statusEventsSection.OnAdd = () =>
            {
                var path = SaveAssetPanel("Creating new StatusEvent", "Enter the name of the new StatusEvent", defaultStatusEventsPathProperty.stringValue, "Status Events path");
                if (string.IsNullOrEmpty(path))
                    return;

                var statusEvent = CreateInstance<StatusEvent>();
                AssetDatabase.CreateAsset(statusEvent, path);
                statusEventsSection.AddCreatedAsset(statusEvent);
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

            AddPathField(defaultStatusDataPathProperty);
            AddPathField(defaultStatusNamesPathProperty);
            AddPathField(defaultComparableNamesPathProperty);
            AddPathField(defaultModulesPathProperty);
            AddPathField(defaultDynamicEffectsPathProperty);
            AddPathField(defaultStatusEventsPathProperty);

            void AddPathField(SerializedProperty property)
            {
                var group = new VisualElement();
                group.style.flexDirection = FlexDirection.Row;
                pathsBox.Add(group);
                var field = new PropertyField();
                field.style.flexGrow = 1;
                field.BindProperty(property);
                group.Add(field);
                group.Add(MakePathButton(property));
            }

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
                propertyField.BindProperty(groupsProperty.GetArrayElementAtIndex(index));
            };
            groupList.BindProperty(groupsProperty);
            groupTab.Add(groupList);
            var groupScroll = groupList.Q<ScrollView>();
            groupScroll.style.borderTopLeftRadius = 0;
            groupScroll.style.borderTopRightRadius = 0;

            return root;
        }

        private static void SetDataIcon(Image icon, StatusEffectData data, Sprite sprite)
        {
            if (sprite)
            {
                icon.sprite = sprite;
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
        public static SettingsProvider CreateStatusSettingsProvider()
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
                    var tabView = settings.Q<TabView>();
                    tabView.style.overflow = Overflow.Hidden;
                    tabView.style.borderTopLeftRadius = 6;
                    tabView.style.borderTopRightRadius = 6;
                    root.Add(settings);
                },

                deactivateHandler = () =>
                {
                    if (settingsEditor != null)
                        DestroyImmediate(settingsEditor);
                    settingsEditor = null;
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Status", "Effect", "Group", "Stack", "Framework", "Event", "Module", "Dynamic", "Comparable", "Path", "Key" })
            };

            return provider;
        }
    }
}
