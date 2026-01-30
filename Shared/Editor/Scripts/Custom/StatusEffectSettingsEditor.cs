using StatusEffects.Modules;
using StatusEffects.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using static StatusEffects.Editor.ModulePopup;
using static StatusEffects.Editor.StatusNamePopup;

namespace StatusEffects.Editor
{
    public class StatusEffectSettingsWindow : EditorWindow
    {
        [MenuItem("Tools/Status Effect Framework/Settings")]
        public static void OpenStatusEffectSettingsWindow()
        {
            EditorWindow window = GetWindow<StatusEffectSettingsWindow>();
            window.titleContent = new GUIContent("Status Effect Settings");
        }

        public void CreateGUI()
        {
            rootVisualElement.Add(UnityEditor.Editor.CreateEditor(StatusEffectSettings.GetOrCreateSettings()).CreateInspectorGUI());
            rootVisualElement.style.paddingLeft = 10;
            rootVisualElement.style.paddingRight = 8;
            rootVisualElement.style.paddingBottom = 8;
        }
    }

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

            ModuleButton = new Button(() => SelectionType(ModuleSelection.Module));
            ModuleButton.text = "Module";
            root.Add(ModuleButton);

            var moduleScriptButton = new Button(() => SelectionType(ModuleSelection.ModuleScript));
            moduleScriptButton.text = "Module Script";
            root.Add(moduleScriptButton);

            var moduleInstanceScript = new Button(() => SelectionType(ModuleSelection.ModuleInstanceScript));
            moduleInstanceScript.text = "Module Instance Script";
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
            
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Module)));
            foreach (var type in types)
            {
                var moduleButton = new Button(() => SelectionType(type));
                moduleButton.text = type.Name;
                root.Add(moduleButton);
            }

            return root;
        }

        public enum ModuleSelection
        {
            Module,
            ModuleScript,
            ModuleInstanceScript
        }
    }

    [CustomEditor(typeof(StatusEffectSettings))]
    [CanEditMultipleObjects]
    internal class StatusEffectSettingsEditor : UnityEditor.Editor
    {
        public const string TabViewDataKey = "status-effect-settings-tabs";
        public static readonly string GroupsTabDataKey = TabViewDataKey + "-groups";
        public static readonly string DatasTabDataKey = TabViewDataKey + "-datas";
        public static readonly string NamesTabDataKey = TabViewDataKey + "-names";
        public static readonly string ComparablesTabDataKey = TabViewDataKey + "-comparables";
        public static readonly string ModulesTabDataKey = TabViewDataKey + "-modules";
        public static readonly string PathsTabDataKey = TabViewDataKey + "-paths";

        private static bool NothingSelectedDialogue() => EditorUtility.DisplayDialog("Delete selected asset?", "Nothing is selected.\n", "Ok");
        private static bool DeleteAssetsDialogue(IEnumerable<string> paths) => EditorUtility.DisplayDialog($"Delete selected asset{(paths.Count() > 1 ? "s" : "")}?", $"{string.Join("\n", paths)}\n\nYou cannot undo the delete assets action.\n", "Delete", "Cancel");

        public override VisualElement CreateInspectorGUI()
        {
            var groupsProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.Groups));
            var defaultStatusDataPathProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.DefaultStatusDataPath));
            var defaultStatusNamesPathProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.DefaultStatusNamesPath));
            var defaultComparableNamesPathProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.DefaultComparableNamesPath));
            var defaultModulesPathProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.DefaultModulesPath));

            var root = new VisualElement();
            root.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
#if UNITY_2023_1_OR_NEWER

            var tabView = new TabView();
            tabView.viewDataKey = TabViewDataKey;
            tabView.Q(className: TabView.headerContainerClassName).style.flexGrow = 1;
            root.Add(tabView);

            var groupTab = new Tab("Groups");
            groupTab.viewDataKey = GroupsTabDataKey;
            var groupHeader = groupTab.tabHeader;
            groupHeader.style.flexGrow = 1;
            groupHeader.style.justifyContent = Justify.Center;
            tabView.Add(groupTab);

            var datasTab = new Tab("Datas");
            datasTab.viewDataKey = DatasTabDataKey;
            var dataHeader = datasTab.tabHeader;
            dataHeader.style.flexGrow = 1;
            dataHeader.style.justifyContent = Justify.Center;
            tabView.Add(datasTab);

            var namesTab = new Tab("Names");
            namesTab.viewDataKey = NamesTabDataKey;
            var namesHeader = namesTab.tabHeader;
            namesHeader.style.flexGrow = 1;
            namesHeader.style.justifyContent = Justify.Center;
            tabView.Add(namesTab);

            var comparablesTab = new Tab("Comparables");
            comparablesTab.viewDataKey = ComparablesTabDataKey;
            var comparablesHeader = comparablesTab.tabHeader;
            comparablesHeader.style.flexGrow = 1;
            comparablesHeader.style.justifyContent = Justify.Center;
            tabView.Add(comparablesTab);

            var modulesTab = new Tab("Modules");
            modulesTab.viewDataKey = ModulesTabDataKey;
            var modulesHeader = modulesTab.tabHeader;
            modulesHeader.style.flexGrow = 1;
            modulesHeader.style.justifyContent = Justify.Center;
            tabView.Add(modulesTab);

            var pathsTab = new Tab("Paths");
            pathsTab.viewDataKey = PathsTabDataKey;
            var pathsHeader = pathsTab.tabHeader;
            pathsHeader.style.flexGrow = 1;
            pathsHeader.style.justifyContent = Justify.Center;
            tabView.Add(pathsTab);

            var dataSearchBox = new VisualElement();
            dataSearchBox.style.borderTopRightRadius = 0;
            dataSearchBox.style.borderTopLeftRadius = 0;
            dataSearchBox.style.borderBottomRightRadius = 0;
            dataSearchBox.style.borderBottomLeftRadius = 0;
            dataSearchBox.style.paddingBottom = 1;
            dataSearchBox.style.marginBottom = -2;
            dataSearchBox.style.flexGrow = 1;
            dataSearchBox.style.flexShrink = 0;
            dataSearchBox.AddToClassList(StatusEffectsStyleSheet.BoxGroupClassName);
            datasTab.Add(dataSearchBox);

            var dataSearchBar = new ToolbarSearchField();
            dataSearchBar.style.marginLeft = 2;
            dataSearchBar.style.flexGrow = 1;
            dataSearchBar.style.width = Length.Auto();
            dataSearchBox.Add(dataSearchBar);

            var datas = new List<StatusEffectData>();
            var dataContainer = new ListView(datas, 40, MakeDatas, BindDatas);
            dataContainer.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            dataContainer.selectionType = SelectionType.Multiple;
            dataContainer.reorderable = false;
            dataContainer.showAddRemoveFooter = true;
            dataContainer.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            dataContainer.showBorder = true;
            dataContainer.onAdd += OnAddDatas;
            dataContainer.onRemove += OnRemoveDatas;
            datasTab.Add(dataContainer);

            var dataScroll = dataContainer.Q<ScrollView>();
            dataScroll.style.borderTopRightRadius = 0;
            dataScroll.style.borderTopLeftRadius = 0;

            SearchContext dataContext = null;
            dataSearchBar.RegisterValueChangedCallback(DatasSearch);
            datasTab.RegisterCallback<DetachFromPanelEvent>(_ => 
            {
                dataContext?.Dispose();
                SettingsEventPostProcessor.OnDatasImported -= DatasChanged;
                SettingsEventPostProcessor.OnAssetRemoved -= DatasChanged;
            });
            tabView.activeTabChanged += DatasTabCheck;

            #region Data Methods
            VisualElement MakeDatas()
            {
                var foldout = new Foldout();
                foldout.delegatesFocus = true;
                foldout.name = "foldout";
                foldout.style.flexGrow = 1;
                foldout.value = false;
                foldout.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
                var foldoutContainer = foldout.contentContainer;
                foldoutContainer.style.marginLeft = 0;
                foldoutContainer.style.paddingLeft = 10;
                foldoutContainer.style.paddingRight = 10;
                foldoutContainer.style.paddingTop = 10;
                foldoutContainer.style.paddingBottom = 10;
                foldoutContainer.AddToClassList(StatusEffectsStyleSheet.BackgroundColorClassName);
                var foldoutToggle = foldout.Q<Toggle>();
                foldoutToggle.style.marginBottom = 0;
                foldoutToggle.style.marginRight = 0;
                foldoutToggle.style.marginTop = 0;
                var checkmark = foldout.Q("unity-checkmark");
                checkmark.style.marginLeft = 5;

                var rowContainer = new VisualElement();
                rowContainer.style.flexDirection = FlexDirection.Row;
                foldout.Add(rowContainer);
                
                var info = new VisualElement();
                info.style.flexGrow = 1;
                info.style.flexShrink = 1;
                info.style.flexDirection = FlexDirection.Row;
                info.style.justifyContent = Justify.SpaceBetween;
                rowContainer.Add(info);

                var icon = new Image();
                icon.name = "icon";
                icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                icon.style.width = 40;
                icon.style.height = 40;
                icon.style.paddingBottom = 2; 
                icon.style.paddingLeft = 2; 
                icon.style.paddingRight = 2; 
                icon.style.paddingTop = 2;
                info.Add(icon);

                var labelContainer = new VisualElement();
                labelContainer.style.flexDirection = FlexDirection.Column;
                labelContainer.style.flexGrow = 1;
                info.Add(labelContainer);

                var nameContainer = new VisualElement();
                nameContainer.style.flexDirection = FlexDirection.Row;
                nameContainer.style.justifyContent = Justify.FlexStart;
                nameContainer.style.flexGrow = 1;
                labelContainer.Add(nameContainer);

                var name = new Label();
                name.name = "name";
                name.selection.isSelectable = true;
                name.style.marginLeft = 0;
                name.style.paddingTop = 0;
                name.style.paddingRight= 0;
                name.style.unityTextAlign = TextAnchor.LowerLeft;
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameContainer.Add(name);

                var renameIcon = new VisualElement();
                renameIcon.style.backgroundImage = new Background { texture = EditorGUIUtility.IconContent("editicon.sml").image as Texture2D };
                renameIcon.style.width = 15;
                renameIcon.style.height = 15;
                renameIcon.style.alignSelf = Align.FlexEnd;
                renameIcon.style.display = DisplayStyle.None;
                nameContainer.Add(renameIcon);
                
                var renameContainer = new VisualElement();
                renameContainer.style.display = DisplayStyle.None;
                renameContainer.style.flexGrow = 1;
                renameContainer.style.justifyContent = Justify.FlexEnd;
                labelContainer.Add(renameContainer);

                var renameField = new TextField();
                renameField.name = "rename-field";
                renameField.style.flexDirection = FlexDirection.Row;
                renameField.style.alignSelf = Align.FlexStart;
                renameField.style.minWidth = 15;
                renameField.style.marginLeft = 0;
                renameField.style.marginRight = 0;
                renameField.style.marginTop = 0;
                renameField.style.marginBottom = 0;
                renameField.selectAllOnFocus = true;
                var renameText = renameField.Q<TextElement>();
                renameText.style.marginLeft = 0;
                renameText.style.paddingTop = 0;
                renameText.style.paddingRight = 0;
                renameContainer.Add(renameField);

                var id = new Label();
                id.name = "id";
                id.style.unityTextAlign = TextAnchor.UpperLeft;
                id.style.fontSize = 10;
                id.style.opacity = 0.5f;
                id.style.marginBottom = 4;
                id.style.marginLeft = 0;
                id.style.paddingTop = 0;
                id.style.paddingRight = 0;
                id.AddToClassList(StatusEffectsStyleSheet.HeaderTextColorClassName);
                labelContainer.Add(id);

                var pingButton = new Button();
                pingButton.name = "ping-button";
                pingButton.iconImage = new Background { texture = EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D };
                pingButton.style.height = 30;
                pingButton.style.width = 30;
                pingButton.style.marginRight = 5;
                pingButton.style.alignSelf = Align.Center;
                pingButton.style.display = DisplayStyle.None;
                info.Add(pingButton);

                foldout.RegisterCallbackOnce<GeometryChangedEvent>((_) => { foldout.hierarchy.Insert(0, rowContainer); rowContainer.Insert(0, foldoutToggle); });

                info.RegisterCallback<PointerEnterEvent>((_) => { renameIcon.style.display = DisplayStyle.Flex; pingButton.style.display = DisplayStyle.Flex; });
                info.RegisterCallback<PointerLeaveEvent>((_) => { renameIcon.style.display = DisplayStyle.None; pingButton.style.display = DisplayStyle.None; });

                name.RegisterCallback<PointerUpEvent>((_) =>
                {
                    renameContainer.style.display = DisplayStyle.Flex;
                    nameContainer.style.display = DisplayStyle.None;
                    renameField.value = name.text;
                    renameField.Focus();
                });
                renameField.RegisterCallback<BlurEvent>((_) => { renameContainer.style.display = DisplayStyle.None; nameContainer.style.display = DisplayStyle.Flex; });

                return foldout;
            }

            void BindDatas(VisualElement element, int index)
            {
                var data = datas[index];
                if (data == null)
                    return;

                var foldout = element.Q<Foldout>("foldout");
                foldout.Q<Toggle>().RegisterValueChangedCallback((changeEvent) => 
                {
                    if (changeEvent.newValue)
                    {
                        var editor = CreateEditor(data).CreateInspectorGUI();
                        foldout.Add(editor);
                    }
                    else
                    {
                        foldout.Clear();
                    }
                });
                var icon = element.Q<Image>("icon");
                if (data.Icon)
                    icon.image = data.Icon.texture;
                else
                {
                    icon.image = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(data));
                    icon.style.opacity = 0.2f;
                }
                element.Q<Label>("id").text = data.Id.ToString();
                element.Q<Button>("ping-button").clicked += () => EditorGUIUtility.PingObject(data);
                var dataTitle = element.Q<Label>("name");
                dataTitle.text = data.name;
                var renameField = element.Q<TextField>("rename-field");
                renameField.RegisterCallback<BlurEvent>((_) =>
                {
                    var value = renameField.value;
                    renameField.value = dataTitle.text;
                    if (string.IsNullOrEmpty(value) || value == dataTitle.text)
                        return;

                    if (string.IsNullOrEmpty(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(data), value)))
                        return;

                    dataTitle.experimental.animation.Start(Color.red, Color.clear, 300, (element, value) =>
                    {
                        element.style.backgroundColor = value;
                    }).Ease(Easing.OutCubic);
                });
            }

            void OnAddDatas(BaseListView view)
            {
                view.ClearSelection();
                Directory.CreateDirectory(Path.Combine(Application.dataPath, defaultStatusDataPathProperty.stringValue));
                var path = EditorUtility.SaveFilePanelInProject("Creating new StatusEffectData", "", "asset", "Enter the name of the new StatusEffectData", Path.Combine("Assets", defaultStatusDataPathProperty.stringValue));
                if (string.IsNullOrEmpty(path))
                    return;
                var data = CreateInstance<StatusEffectData>();
                AssetDatabase.CreateAsset(data, path);
                var itemsSourceCount = view.itemsSource.Count;
                view.itemsSource.Add(data);
                view.ScrollToItem(itemsSourceCount);
                EditorGUIUtility.PingObject(data);
            }

            void OnRemoveDatas(BaseListView view)
            {
                List<string> paths = new();
                foreach (var index in view.selectedIndices)
                    paths.Add(AssetDatabase.GetAssetPath(datas[index]));

                if (paths.Count <= 0)
                {
                    NothingSelectedDialogue();
                    return;
                }

                if (!DeleteAssetsDialogue(paths))
                    return;

                view.ClearSelection();
                AssetDatabase.DeleteAssets(paths.ToArray(), paths);
            }

            void DatasTabCheck(Tab previous, Tab current)
            {
                if (previous == datasTab)
                {
                    dataContext?.Dispose();
                    SettingsEventPostProcessor.OnDatasImported -= DatasChanged;
                    SettingsEventPostProcessor.OnAssetRemoved -= DatasChanged;
                }
                if (current == datasTab)
                {
                    datas.Clear();
                    dataContainer.Rebuild();
                    dataContext = SearchService.CreateContext($"t={nameof(StatusEffectData)} {dataSearchBar.value}");
                    SearchService.Request(dataContext, DatasResults, default, SearchFlags.None);
                    SettingsEventPostProcessor.OnDatasImported += DatasChanged;
                    SettingsEventPostProcessor.OnAssetRemoved += DatasChanged;
                }
            }

            void DatasSearch(ChangeEvent<string> _)
            {
                DatasChanged();
            }

            void DatasChanged()
            {
                dataContext?.Dispose();
                datas.Clear();
                dataContainer.Rebuild();
                dataContext = SearchService.CreateContext($"t={nameof(StatusEffectData)} {dataSearchBar.value}");
                SearchService.Request(dataContext, DatasResults, default, SearchFlags.None);
            }

            void DatasResults(SearchContext context, IEnumerable<SearchItem> items)
            {
                foreach (var item in items)
                {
                    var data = item.ToObject<StatusEffectData>();
                    if (data == null)
                        continue;
                    datas.Add(data);
                }
                dataContainer.Rebuild();
            }
            #endregion

            var nameSearchBox = new VisualElement();
            nameSearchBox.style.borderTopRightRadius = 0;
            nameSearchBox.style.borderTopLeftRadius = 0;
            nameSearchBox.style.borderBottomRightRadius = 0;
            nameSearchBox.style.borderBottomLeftRadius = 0;
            nameSearchBox.style.paddingBottom = 1;
            nameSearchBox.style.marginBottom = -2;
            nameSearchBox.style.flexGrow = 1;
            nameSearchBox.style.flexShrink = 0;
            nameSearchBox.AddToClassList(StatusEffectsStyleSheet.BoxGroupClassName);
            namesTab.Add(nameSearchBox);

            var nameSearchBar = new ToolbarSearchField();
            nameSearchBar.style.marginLeft = 2;
            nameSearchBar.style.flexGrow = 1;
            nameSearchBar.style.width = Length.Auto();
            nameSearchBox.Add(nameSearchBar);

            var names = new List<StatusName>();
            var nameContainer = new ListView(names, 40, MakeNames, BindNames);
            VisualElement namesAddButton = null;
            nameContainer.RegisterCallbackOnce<GeometryChangedEvent>((_) => { namesAddButton = nameContainer.Q(BaseListView.footerAddButtonName); });
            nameContainer.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            nameContainer.selectionType = SelectionType.Multiple;
            nameContainer.reorderable = false;
            nameContainer.showAddRemoveFooter = true;
            nameContainer.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            nameContainer.showBorder = true;
            nameContainer.onAdd += OnAddNames;
            nameContainer.onRemove += OnRemoveNames;
            namesTab.Add(nameContainer);

            var nameScroll = nameContainer.Q<ScrollView>();
            nameScroll.style.borderTopRightRadius = 0;
            nameScroll.style.borderTopLeftRadius = 0;

            SearchContext nameContext = null;
            nameSearchBar.RegisterValueChangedCallback(NamesSearch);
            namesTab.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                nameContext?.Dispose();
                SettingsEventPostProcessor.OnNamesImported -= NamesChanged;
                SettingsEventPostProcessor.OnAssetRemoved -= NamesChanged;
            });
            tabView.activeTabChanged += NamesTabCheck;

            #region Name Methods
            VisualElement MakeNames()
            {
                var root = new VisualElement();
                root.style.flexGrow = 1;
                root.style.flexShrink = 1;
                root.style.flexDirection = FlexDirection.Row;
                root.style.justifyContent = Justify.SpaceBetween;
                
                var icon = new Image();
                icon.name = "icon";
                icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                icon.style.width = 40;
                icon.style.height = 40;
                icon.style.paddingBottom = 2;
                icon.style.paddingLeft = 2;
                icon.style.paddingRight = 2;
                icon.style.paddingTop = 2;
                root.Add(icon);

                var labelContainer = new VisualElement();
                labelContainer.style.flexDirection = FlexDirection.Column;
                labelContainer.style.flexGrow = 1;
                root.Add(labelContainer);

                var nameContainer = new VisualElement();
                nameContainer.style.flexDirection = FlexDirection.Row;
                nameContainer.style.justifyContent = Justify.FlexStart;
                nameContainer.style.flexGrow = 1;
                labelContainer.Add(nameContainer);

                var name = new Label();
                name.name = "name";
                name.selection.isSelectable = true;
                name.style.marginLeft = 0;
                name.style.paddingTop = 0;
                name.style.paddingRight = 0;
                name.style.unityTextAlign = TextAnchor.LowerLeft;
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameContainer.Add(name);

                var renameIcon = new VisualElement();
                renameIcon.style.backgroundImage = new Background { texture = EditorGUIUtility.IconContent("editicon.sml").image as Texture2D };
                renameIcon.style.width = 15;
                renameIcon.style.height = 15;
                renameIcon.style.alignSelf = Align.FlexEnd;
                renameIcon.style.display = DisplayStyle.None;
                nameContainer.Add(renameIcon);

                var renameContainer = new VisualElement();
                renameContainer.style.display = DisplayStyle.None;
                renameContainer.style.flexGrow = 1;
                renameContainer.style.justifyContent = Justify.FlexEnd;
                labelContainer.Add(renameContainer);

                var renameField = new TextField();
                renameField.name = "rename-field";
                renameField.style.flexDirection = FlexDirection.Row;
                renameField.style.alignSelf = Align.FlexStart;
                renameField.style.minWidth = 15;
                renameField.style.marginLeft = 0;
                renameField.style.marginRight = 0;
                renameField.style.marginTop = 0;
                renameField.style.marginBottom = 0;
                renameField.selectAllOnFocus = true;
                var renameText = renameField.Q<TextElement>();
                renameText.style.marginLeft = 0;
                renameText.style.paddingTop = 0;
                renameText.style.paddingRight = 0;
                renameContainer.Add(renameField);

                var id = new Label();
                id.name = "id";
                id.style.unityTextAlign = TextAnchor.UpperLeft;
                id.style.fontSize = 10;
                id.style.opacity = 0.5f;
                id.style.marginBottom = 4;
                id.style.marginLeft = 0;
                id.style.paddingTop = 0;
                id.style.paddingRight = 0;
                id.AddToClassList(StatusEffectsStyleSheet.HeaderTextColorClassName);
                labelContainer.Add(id);

                var pingButton = new Button();
                pingButton.name = "ping-button";
                pingButton.iconImage = new Background { texture = EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D };
                pingButton.style.height = 30;
                pingButton.style.width = 30;
                pingButton.style.marginRight = 5;
                pingButton.style.alignSelf = Align.Center;
                pingButton.style.display = DisplayStyle.None;
                root.Add(pingButton);

                root.RegisterCallback<PointerEnterEvent>((_) => { renameIcon.style.display = DisplayStyle.Flex; pingButton.style.display = DisplayStyle.Flex; });
                root.RegisterCallback<PointerLeaveEvent>((_) => { renameIcon.style.display = DisplayStyle.None; pingButton.style.display = DisplayStyle.None; });

                name.RegisterCallback<PointerUpEvent>((_) =>
                {
                    renameContainer.style.display = DisplayStyle.Flex;
                    nameContainer.style.display = DisplayStyle.None;
                    renameField.value = name.text;
                    renameField.Focus();
                });
                renameField.RegisterCallback<BlurEvent>((_) => { renameContainer.style.display = DisplayStyle.None; nameContainer.style.display = DisplayStyle.Flex; });

                return root;
            }

            void BindNames(VisualElement element, int index)
            {
                var name = names[index];
                if (name == null)
                    return;
                
                var icon = element.Q<Image>("icon");
                icon.image = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(name));
                element.Q<Label>("id").text = name.Id.ToString();
                element.Q<Button>("ping-button").clicked += () => EditorGUIUtility.PingObject(name);
                var nameTitle = element.Q<Label>("name");
                nameTitle.text = name.name;
                var renameField = element.Q<TextField>("rename-field");
                renameField.RegisterCallback<BlurEvent>((_) =>
                {
                    var value = renameField.value;
                    renameField.value = nameTitle.text;
                    if (string.IsNullOrEmpty(value) || value == nameTitle.text)
                        return;

                    if (string.IsNullOrEmpty(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(name), value)))
                        return;

                    nameTitle.experimental.animation.Start(Color.red, Color.clear, 300, (element, value) =>
                    {
                        element.style.backgroundColor = value;
                    }).Ease(Easing.OutCubic);
                });
            }

            void OnAddNames(BaseListView view)
            {
                view.ClearSelection();
                var popup = new StatusNamePopup();
                popup.SelectionType += OnSelectionType;
                UnityEditor.PopupWindow.Show(namesAddButton.worldBound, popup);

                void OnSelectionType(StatusNameSelection selection)
                {
                    Directory.CreateDirectory(Path.Combine(Application.dataPath, defaultStatusNamesPathProperty.stringValue));
                    var path = EditorUtility.SaveFilePanelInProject("Creating new StatusName", "", "asset", "Enter the name of the new StatusName", Path.Combine("Assets", defaultStatusNamesPathProperty.stringValue));
                    if (string.IsNullOrEmpty(path))
                        return;
                    
                    StatusName name = selection switch
                    {
                        StatusNameSelection.Int => CreateInstance<StatusNameInt>(),
                        StatusNameSelection.Bool => CreateInstance<StatusNameBool>(),
                        _ => CreateInstance<StatusNameFloat>()
                    };
                    AssetDatabase.CreateAsset(name, path);
                    var itemsSourceCount = view.itemsSource.Count;
                    view.itemsSource.Add(name);
                    view.ScrollToItem(itemsSourceCount);
                    EditorGUIUtility.PingObject(name);
                }
            }

            void OnRemoveNames(BaseListView view)
            {
                List<string> paths = new();
                foreach (var index in view.selectedIndices)
                    paths.Add(AssetDatabase.GetAssetPath(names[index]));

                if (paths.Count <= 0)
                {
                    NothingSelectedDialogue();
                    return;
                }

                if (!DeleteAssetsDialogue(paths))
                    return;

                view.ClearSelection();
                AssetDatabase.DeleteAssets(paths.ToArray(), paths);
            }

            void NamesTabCheck(Tab previous, Tab current)
            {
                if (previous == namesTab)
                {
                    nameContext?.Dispose();
                    SettingsEventPostProcessor.OnNamesImported -= NamesChanged;
                    SettingsEventPostProcessor.OnAssetRemoved -= NamesChanged;
                }
                if (current == namesTab)
                {
                    names.Clear();
                    nameContainer.Rebuild();
                    nameContext = SearchService.CreateContext($"t={nameof(StatusName)} {nameSearchBar.value}");
                    SearchService.Request(nameContext, NamesResults, default, SearchFlags.None);
                    SettingsEventPostProcessor.OnNamesImported += NamesChanged;
                    SettingsEventPostProcessor.OnAssetRemoved += NamesChanged;
                }
            }

            void NamesSearch(ChangeEvent<string> _)
            {
                NamesChanged();
            }

            void NamesChanged()
            {
                nameContext?.Dispose();
                names.Clear();
                nameContainer.Rebuild();
                nameContext = SearchService.CreateContext($"t={nameof(StatusName)} {nameSearchBar.value}");
                SearchService.Request(nameContext, NamesResults, default, SearchFlags.None);
            }

            void NamesResults(SearchContext context, IEnumerable<SearchItem> items)
            {
                foreach (var item in items)
                {
                    var name = item.ToObject<StatusName>();
                    if (name == null)
                        continue;
                    names.Add(name);
                }
                nameContainer.Rebuild();
            }
            #endregion

            var comparableSearchBox = new VisualElement();
            comparableSearchBox.style.borderTopRightRadius = 0;
            comparableSearchBox.style.borderTopLeftRadius = 0;
            comparableSearchBox.style.borderBottomRightRadius = 0;
            comparableSearchBox.style.borderBottomLeftRadius = 0;
            comparableSearchBox.style.paddingBottom = 1;
            comparableSearchBox.style.marginBottom = -2;
            comparableSearchBox.style.flexGrow = 1;
            comparableSearchBox.style.flexShrink = 0;
            comparableSearchBox.AddToClassList(StatusEffectsStyleSheet.BoxGroupClassName);
            comparablesTab.Add(comparableSearchBox);

            var comparableSearchBar = new ToolbarSearchField();
            comparableSearchBar.style.marginLeft = 2;
            comparableSearchBar.style.flexGrow = 1;
            comparableSearchBar.style.width = Length.Auto();
            comparableSearchBox.Add(comparableSearchBar);

            var comparables = new List<ComparableName>();
            var comparableContainer = new ListView(comparables, 40, MakeComparables, BindComparables);
            comparableContainer.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            comparableContainer.selectionType = SelectionType.Multiple;
            comparableContainer.reorderable = false;
            comparableContainer.showAddRemoveFooter = true;
            comparableContainer.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            comparableContainer.showBorder = true;
            comparableContainer.onAdd += OnAddComparables;
            comparableContainer.onRemove += OnRemoveComparables;
            comparablesTab.Add(comparableContainer);

            var comparableScroll = comparableContainer.Q<ScrollView>();
            comparableScroll.style.borderTopRightRadius = 0;
            comparableScroll.style.borderTopLeftRadius = 0;

            SearchContext comparableContext = null;
            comparableSearchBar.RegisterValueChangedCallback(ComparablesSearch);
            comparablesTab.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                comparableContext?.Dispose();
                SettingsEventPostProcessor.OnComparablesImported -= ComparablesChanged;
                SettingsEventPostProcessor.OnAssetRemoved -= ComparablesChanged;
            });
            tabView.activeTabChanged += ComparablesTabCheck;

            #region Comparable Methods
            VisualElement MakeComparables()
            {
                var root = new VisualElement();
                root.style.flexGrow = 1;
                root.style.flexShrink = 1;
                root.style.flexDirection = FlexDirection.Row;
                root.style.justifyContent = Justify.SpaceBetween;

                var icon = new Image();
                icon.name = "icon";
                icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                icon.style.width = 40;
                icon.style.height = 40;
                icon.style.paddingBottom = 2;
                icon.style.paddingLeft = 2;
                icon.style.paddingRight = 2;
                icon.style.paddingTop = 2;
                root.Add(icon);

                var labelContainer = new VisualElement();
                labelContainer.style.flexDirection = FlexDirection.Column;
                labelContainer.style.flexGrow = 1;
                root.Add(labelContainer);

                var nameContainer = new VisualElement();
                nameContainer.style.flexDirection = FlexDirection.Row;
                nameContainer.style.justifyContent = Justify.FlexStart;
                nameContainer.style.flexGrow = 1;
                labelContainer.Add(nameContainer);

                var name = new Label();
                name.name = "name";
                name.selection.isSelectable = true;
                name.style.marginLeft = 0;
                name.style.paddingTop = 0;
                name.style.paddingRight = 0;
                name.style.unityTextAlign = TextAnchor.LowerLeft;
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameContainer.Add(name);

                var renameIcon = new VisualElement();
                renameIcon.style.backgroundImage = new Background { texture = EditorGUIUtility.IconContent("editicon.sml").image as Texture2D };
                renameIcon.style.width = 15;
                renameIcon.style.height = 15;
                renameIcon.style.alignSelf = Align.FlexEnd;
                renameIcon.style.display = DisplayStyle.None;
                nameContainer.Add(renameIcon);

                var renameContainer = new VisualElement();
                renameContainer.style.display = DisplayStyle.None;
                renameContainer.style.flexGrow = 1;
                renameContainer.style.justifyContent = Justify.FlexEnd;
                labelContainer.Add(renameContainer);

                var renameField = new TextField();
                renameField.name = "rename-field";
                renameField.style.flexDirection = FlexDirection.Row;
                renameField.style.alignSelf = Align.FlexStart;
                renameField.style.minWidth = 15;
                renameField.style.marginLeft = 0;
                renameField.style.marginRight = 0;
                renameField.style.marginTop = 0;
                renameField.style.marginBottom = 0;
                renameField.selectAllOnFocus = true;
                var renameText = renameField.Q<TextElement>();
                renameText.style.marginLeft = 0;
                renameText.style.paddingTop = 0;
                renameText.style.paddingRight = 0;
                renameContainer.Add(renameField);

                var id = new Label();
                id.name = "id";
                id.style.unityTextAlign = TextAnchor.UpperLeft;
                id.style.fontSize = 10;
                id.style.opacity = 0.5f;
                id.style.marginBottom = 4;
                id.style.marginLeft = 0;
                id.style.paddingTop = 0;
                id.style.paddingRight = 0;
                id.AddToClassList(StatusEffectsStyleSheet.HeaderTextColorClassName);
                labelContainer.Add(id);

                var pingButton = new Button();
                pingButton.name = "ping-button";
                pingButton.iconImage = new Background { texture = EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D };
                pingButton.style.height = 30;
                pingButton.style.width = 30;
                pingButton.style.marginRight = 5;
                pingButton.style.alignSelf = Align.Center;
                pingButton.style.display = DisplayStyle.None;
                root.Add(pingButton);

                root.RegisterCallback<PointerEnterEvent>((_) => { renameIcon.style.display = DisplayStyle.Flex; pingButton.style.display = DisplayStyle.Flex; });
                root.RegisterCallback<PointerLeaveEvent>((_) => { renameIcon.style.display = DisplayStyle.None; pingButton.style.display = DisplayStyle.None; });

                name.RegisterCallback<PointerUpEvent>((_) =>
                {
                    renameContainer.style.display = DisplayStyle.Flex;
                    nameContainer.style.display = DisplayStyle.None;
                    renameField.value = name.text;
                    renameField.Focus();
                });
                renameField.RegisterCallback<BlurEvent>((_) => { renameContainer.style.display = DisplayStyle.None; nameContainer.style.display = DisplayStyle.Flex; });

                return root;
            }

            void BindComparables(VisualElement element, int index)
            {
                var comparable = comparables[index];
                if (comparable == null)
                    return;

                var icon = element.Q<Image>("icon");
                icon.image = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(comparable));
                element.Q<Label>("id").text = comparable.Id.ToString();
                element.Q<Button>("ping-button").clicked += () => EditorGUIUtility.PingObject(comparable);
                var comparableTitle = element.Q<Label>("name");
                comparableTitle.text = comparable.name;
                var renameField = element.Q<TextField>("rename-field");
                renameField.RegisterCallback<BlurEvent>((_) =>
                {
                    var value = renameField.value;
                    renameField.value = comparableTitle.text;
                    if (string.IsNullOrEmpty(value) || value == comparableTitle.text)
                        return;

                    if (string.IsNullOrEmpty(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(comparable), value)))
                        return;

                    comparableTitle.experimental.animation.Start(Color.red, Color.clear, 300, (element, value) =>
                    {
                        element.style.backgroundColor = value;
                    }).Ease(Easing.OutCubic);
                });
            }

            void OnAddComparables(BaseListView view)
            {
                view.ClearSelection();
                Directory.CreateDirectory(Path.Combine(Application.dataPath, defaultComparableNamesPathProperty.stringValue));
                var path = EditorUtility.SaveFilePanelInProject("Creating new ComparableName", "", "asset", "Enter the name of the new ComparableName", Path.Combine("Assets", defaultComparableNamesPathProperty.stringValue));
                if (string.IsNullOrEmpty(path))
                    return;

                ComparableName comparable = CreateInstance<ComparableName>();
                AssetDatabase.CreateAsset(comparable, path);
                var itemsSourceCount = view.itemsSource.Count;
                view.itemsSource.Add(comparable);
                view.ScrollToItem(itemsSourceCount);
                EditorGUIUtility.PingObject(comparable);
            }

            void OnRemoveComparables(BaseListView view)
            {
                List<string> paths = new();
                foreach (var index in view.selectedIndices)
                    paths.Add(AssetDatabase.GetAssetPath(comparables[index]));

                if (paths.Count <= 0)
                {
                    NothingSelectedDialogue();
                    return;
                }

                if (!DeleteAssetsDialogue(paths))
                    return;

                view.ClearSelection();
                AssetDatabase.DeleteAssets(paths.ToArray(), paths);
            }

            void ComparablesTabCheck(Tab previous, Tab current)
            {
                if (previous == comparablesTab)
                {
                    comparableContext?.Dispose();
                    SettingsEventPostProcessor.OnComparablesImported -= ComparablesChanged;
                    SettingsEventPostProcessor.OnAssetRemoved -= ComparablesChanged;
                }
                if (current == comparablesTab)
                {
                    comparables.Clear();
                    comparableContainer.Rebuild();
                    comparableContext = SearchService.CreateContext($"t={nameof(ComparableName)} {comparableSearchBar.value}");
                    SearchService.Request(comparableContext, ComparablesResults, default, SearchFlags.None);
                    SettingsEventPostProcessor.OnComparablesImported += ComparablesChanged;
                    SettingsEventPostProcessor.OnAssetRemoved += ComparablesChanged;
                }
            }

            void ComparablesSearch(ChangeEvent<string> _)
            {
                ComparablesChanged();
            }

            void ComparablesChanged()
            {
                comparableContext?.Dispose();
                comparables.Clear();
                comparableContainer.Rebuild();
                comparableContext = SearchService.CreateContext($"t={nameof(ComparableName)} {comparableSearchBar.value}");
                SearchService.Request(comparableContext, ComparablesResults, default, SearchFlags.None);
            }

            void ComparablesResults(SearchContext context, IEnumerable<SearchItem> items)
            {
                foreach (var item in items)
                {
                    var comparable = item.ToObject<ComparableName>();
                    if (comparable == null)
                        continue;
                    comparables.Add(comparable);
                }
                comparableContainer.Rebuild();
            }
            #endregion

            var moduleSearchBox = new VisualElement();
            moduleSearchBox.style.borderTopRightRadius = 0;
            moduleSearchBox.style.borderTopLeftRadius = 0;
            moduleSearchBox.style.borderBottomRightRadius = 0;
            moduleSearchBox.style.borderBottomLeftRadius = 0;
            moduleSearchBox.style.paddingBottom = 1;
            moduleSearchBox.style.marginBottom = -2;
            moduleSearchBox.style.flexGrow = 1;
            moduleSearchBox.style.flexShrink = 0;
            moduleSearchBox.AddToClassList(StatusEffectsStyleSheet.BoxGroupClassName);
            modulesTab.Add(moduleSearchBox);

            var moduleSearchBar = new ToolbarSearchField();
            moduleSearchBar.style.marginLeft = 2;
            moduleSearchBar.style.flexGrow = 1;
            moduleSearchBar.style.width = Length.Auto();
            moduleSearchBox.Add(moduleSearchBar);

            var modules = new List<Module>();
            var moduleContainer = new ListView(modules, 40, MakeModules, BindModules);
            VisualElement modulesAddButton = null;
            moduleContainer.RegisterCallbackOnce<GeometryChangedEvent>((_) => { modulesAddButton = moduleContainer.Q(BaseListView.footerAddButtonName); });
            moduleContainer.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            moduleContainer.selectionType = SelectionType.Multiple;
            moduleContainer.reorderable = false;
            moduleContainer.showAddRemoveFooter = true;
            moduleContainer.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            moduleContainer.showBorder = true;
            moduleContainer.onAdd += OnAddModules;
            moduleContainer.onRemove += OnRemoveModules;
            modulesTab.Add(moduleContainer);

            var moduleScroll = moduleContainer.Q<ScrollView>();
            moduleScroll.style.borderTopRightRadius = 0;
            moduleScroll.style.borderTopLeftRadius = 0;

            SearchContext moduleContext = null;
            moduleSearchBar.RegisterValueChangedCallback(ModulesSearch);
            modulesTab.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                moduleContext?.Dispose();
                SettingsEventPostProcessor.OnModulesImported -= ModulesChanged;
                SettingsEventPostProcessor.OnAssetRemoved -= ModulesChanged;
            });
            tabView.activeTabChanged += ModulesTabCheck;

            #region Module Methods
            VisualElement MakeModules()
            {
                var foldout = new Foldout();
                foldout.delegatesFocus = true;
                foldout.name = "foldout";
                foldout.style.flexGrow = 1;
                foldout.value = false;
                foldout.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
                var foldoutContainer = foldout.contentContainer;
                foldoutContainer.style.marginLeft = 0;
                foldoutContainer.style.paddingLeft = 10;
                foldoutContainer.style.paddingRight = 10;
                foldoutContainer.style.paddingTop = 10;
                foldoutContainer.style.paddingBottom = 10;
                foldoutContainer.AddToClassList(StatusEffectsStyleSheet.BackgroundColorClassName);
                var foldoutToggle = foldout.Q<Toggle>();
                foldoutToggle.style.marginBottom = 0;
                foldoutToggle.style.marginRight = 0;
                foldoutToggle.style.marginTop = 0;
                var checkmark = foldout.Q("unity-checkmark");
                checkmark.style.marginLeft = 5;

                var rowContainer = new VisualElement();
                rowContainer.style.flexDirection = FlexDirection.Row;
                foldout.Add(rowContainer);

                var info = new VisualElement();
                info.style.flexGrow = 1;
                info.style.flexShrink = 1;
                info.style.flexDirection = FlexDirection.Row;
                info.style.justifyContent = Justify.SpaceBetween;
                rowContainer.Add(info);

                var icon = new Image();
                icon.name = "icon";
                icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                icon.style.width = 40;
                icon.style.height = 40;
                icon.style.paddingBottom = 2;
                icon.style.paddingLeft = 2;
                icon.style.paddingRight = 2;
                icon.style.paddingTop = 2;
                info.Add(icon);

                var labelContainer = new VisualElement();
                labelContainer.style.flexDirection = FlexDirection.Column;
                labelContainer.style.flexGrow = 1;
                info.Add(labelContainer);

                var nameContainer = new VisualElement();
                nameContainer.style.flexDirection = FlexDirection.Row;
                nameContainer.style.justifyContent = Justify.FlexStart;
                nameContainer.style.flexGrow = 1;
                labelContainer.Add(nameContainer);

                var name = new Label();
                name.name = "name";
                name.selection.isSelectable = true;
                name.style.marginLeft = 0;
                name.style.paddingTop = 0;
                name.style.paddingRight = 0;
                name.style.unityTextAlign = TextAnchor.LowerLeft;
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameContainer.Add(name);

                var renameIcon = new VisualElement();
                renameIcon.style.backgroundImage = new Background { texture = EditorGUIUtility.IconContent("editicon.sml").image as Texture2D };
                renameIcon.style.width = 15;
                renameIcon.style.height = 15;
                renameIcon.style.alignSelf = Align.FlexEnd;
                renameIcon.style.display = DisplayStyle.None;
                nameContainer.Add(renameIcon);

                var renameContainer = new VisualElement();
                renameContainer.style.display = DisplayStyle.None;
                renameContainer.style.flexGrow = 1;
                renameContainer.style.justifyContent = Justify.FlexEnd;
                labelContainer.Add(renameContainer);

                var renameField = new TextField();
                renameField.name = "rename-field";
                renameField.style.flexDirection = FlexDirection.Row;
                renameField.style.alignSelf = Align.FlexStart;
                renameField.style.minWidth = 15;
                renameField.style.marginLeft = 0;
                renameField.style.marginRight = 0;
                renameField.style.marginTop = 0;
                renameField.style.marginBottom = 0;
                renameField.selectAllOnFocus = true;
                var renameText = renameField.Q<TextElement>();
                renameText.style.marginLeft = 0;
                renameText.style.paddingTop = 0;
                renameText.style.paddingRight = 0;
                renameContainer.Add(renameField);

                var type = new Label();
                type.name = "type";
                type.style.unityTextAlign = TextAnchor.UpperLeft;
                type.style.fontSize = 10;
                type.style.opacity = 0.5f;
                type.style.marginBottom = 4;
                type.style.marginLeft = 0;
                type.style.paddingTop = 0;
                type.style.paddingRight = 0;
                type.AddToClassList(StatusEffectsStyleSheet.HeaderTextColorClassName);
                labelContainer.Add(type);

                var scriptButton = new Button();
                scriptButton.name = "script-button";
                scriptButton.iconImage = new Background { texture = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D };
                scriptButton.style.height = 30;
                scriptButton.style.width = 30;
                scriptButton.style.marginRight = 5;
                scriptButton.style.paddingLeft = 2;
                scriptButton.style.paddingRight = 2;
                scriptButton.style.paddingTop = 3;
                scriptButton.style.paddingBottom = 3;
                scriptButton.style.alignSelf = Align.Center;
                scriptButton.style.display = DisplayStyle.None;
                info.Add(scriptButton);

                var pingButton = new Button();
                pingButton.name = "ping-button";
                pingButton.iconImage = new Background { texture = EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D };
                pingButton.style.height = 30;
                pingButton.style.width = 30;
                pingButton.style.marginRight = 5;
                pingButton.style.alignSelf = Align.Center;
                pingButton.style.display = DisplayStyle.None;
                info.Add(pingButton);

                foldout.RegisterCallbackOnce<GeometryChangedEvent>((_) => { foldout.hierarchy.Insert(0, rowContainer); rowContainer.Insert(0, foldoutToggle); });

                info.RegisterCallback<PointerEnterEvent>((_) => { renameIcon.style.display = DisplayStyle.Flex; pingButton.style.display = DisplayStyle.Flex; scriptButton.style.display = DisplayStyle.Flex; });
                info.RegisterCallback<PointerLeaveEvent>((_) => { renameIcon.style.display = DisplayStyle.None; pingButton.style.display = DisplayStyle.None; scriptButton.style.display = DisplayStyle.None; });

                name.RegisterCallback<PointerUpEvent>((_) =>
                {
                    renameContainer.style.display = DisplayStyle.Flex;
                    nameContainer.style.display = DisplayStyle.None;
                    renameField.value = name.text;
                    renameField.Focus();
                });
                renameField.RegisterCallback<BlurEvent>((_) => { renameContainer.style.display = DisplayStyle.None; nameContainer.style.display = DisplayStyle.Flex; });

                return foldout;
            }

            void BindModules(VisualElement element, int index)
            {
                var module = modules[index];
                if (module == null)
                    return;

                var foldout = element.Q<Foldout>("foldout");
                var foldoutToggle = foldout.Q<Toggle>();
                var modulePropertyCheck = CreateEditor(module).serializedObject.GetIterator();
                modulePropertyCheck.NextVisible(true);
                if (!modulePropertyCheck.NextVisible(true))
                {
                    // FIX FOLDOUT GETTING TOGGLED!! I DON"T WANT IT INTERACTAbLE HERE ANDI WANT THE LABEL AREA TO NOT TOGGLE IT.
                    // ALSO FIX THE BUTTTONS GETTING CUT OFF WHEN ARE TOO SMALl, MAKE NAME GET Cut FIRST.
                    //foldout.pickingMode = PickingMode;
                    foldout.Q("unity-checkmark").RemoveFromHierarchy();
                }
                foldoutToggle.RegisterValueChangedCallback((changeEvent) =>
                {
                    if (changeEvent.newValue)
                    {
                        var root = new VisualElement();
                        var instance = CreateEditor(module).serializedObject;

                        var iterator = instance.GetIterator();
                        // Skip the script property
                        iterator.NextVisible(true);

                        if (iterator.NextVisible(true))
                        {
                            do
                            {
                                var propertyField = new PropertyField() { name = "property-field: " + iterator.propertyPath };
                                propertyField.BindProperty(iterator.Copy());

                                root.Add(propertyField);
                            }
                            while (iterator.NextVisible(false));
                        }
                        foldout.Add(root);
                    }
                    else
                    {
                        foldout.Clear();
                    }
                });
                var icon = element.Q<Image>("icon");
                icon.image = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(module));
                var script = MonoScript.FromScriptableObject(module);
                element.Q<Label>("type").text = script.GetClass().ToString();
                element.Q<Button>("script-button").clicked += () => EditorGUIUtility.PingObject(script);
                element.Q<Button>("ping-button").clicked += () => EditorGUIUtility.PingObject(module);
                var moduleTitle = element.Q<Label>("name");
                moduleTitle.text = module.name;
                var renameField = element.Q<TextField>("rename-field");
                renameField.RegisterCallback<BlurEvent>((_) =>
                {
                    var value = renameField.value;
                    renameField.value = moduleTitle.text;
                    if (string.IsNullOrEmpty(value) || value == moduleTitle.text)
                        return;

                    if (string.IsNullOrEmpty(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(module), value)))
                        return;

                    moduleTitle.experimental.animation.Start(Color.red, Color.clear, 300, (element, value) =>
                    {
                        element.style.backgroundColor = value;
                    }).Ease(Easing.OutCubic);
                });
            }

            void OnAddModules(BaseListView view)
            {
                view.ClearSelection();
                var popup = new ModulePopup();
                popup.SelectionType += OnSelectionType;
                UnityEditor.PopupWindow.Show(modulesAddButton.worldBound, popup);

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
                            Directory.CreateDirectory(Path.Combine(Application.dataPath, defaultModulesPathProperty.stringValue));
                            var path = EditorUtility.SaveFilePanelInProject("Creating new Module", "", "asset", "Enter the name of the new Module", Path.Combine("Assets", defaultModulesPathProperty.stringValue));
                            if (string.IsNullOrEmpty(path))
                                return;
                            var module = CreateInstance(type);
                            AssetDatabase.CreateAsset(module, path);
                            var itemsSourceCount = view.itemsSource.Count;
                            view.itemsSource.Add(module);
                            view.ScrollToItem(itemsSourceCount);
                            EditorGUIUtility.PingObject(module);
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
#elif UNITASK
                                StatusEffectScriptTemplates.UniTaskModuleScriptContent
#else
                                StatusEffectScriptTemplates.ModuleScriptContent
#endif

                        };
                        var script = StatusEffectScriptTemplates.CreateScriptAssetFromContent(content, Path.Combine(directoryPath, cleanedEnteredNamed + ".cs"), enteredName, cleanedEnteredNamed);
                        EditorGUIUtility.PingObject(script);
                    }
                }
                
            }

            void OnRemoveModules(BaseListView view)
            {
                List<string> paths = new();
                foreach (var index in view.selectedIndices)
                    paths.Add(AssetDatabase.GetAssetPath(modules[index]));

                if (paths.Count <= 0)
                {
                    NothingSelectedDialogue();
                    return;
                }

                if (!DeleteAssetsDialogue(paths))
                    return;

                view.ClearSelection();
                AssetDatabase.DeleteAssets(paths.ToArray(), paths);
            }

            void ModulesTabCheck(Tab previous, Tab current)
            {
                if (previous == modulesTab)
                {
                    moduleContext?.Dispose();
                    SettingsEventPostProcessor.OnModulesImported -= ModulesChanged;
                    SettingsEventPostProcessor.OnAssetRemoved -= ModulesChanged;
                }
                if (current == modulesTab)
                {
                    modules.Clear();
                    moduleContainer.Rebuild();
                    moduleContext = SearchService.CreateContext($"t={nameof(Module)} {moduleSearchBar.value}");
                    SearchService.Request(moduleContext, ModulesResults, default, SearchFlags.None);
                    SettingsEventPostProcessor.OnModulesImported += ModulesChanged;
                    SettingsEventPostProcessor.OnAssetRemoved += ModulesChanged;
                }
            }

            void ModulesSearch(ChangeEvent<string> _)
            {
                ModulesChanged();
            }

            void ModulesChanged()
            {
                moduleContext?.Dispose();
                modules.Clear();
                moduleContainer.Rebuild();
                moduleContext = SearchService.CreateContext($"t={nameof(Module)} {moduleSearchBar.value}");
                SearchService.Request(moduleContext, ModulesResults, default, SearchFlags.None);
            }

            void ModulesResults(SearchContext context, IEnumerable<SearchItem> items)
            {
                foreach (var item in items)
                {
                    var module = item.ToObject<Module>();
                    if (module == null)
                        continue;
                    modules.Add(module);
                }
                moduleContainer.Rebuild();
            }
            #endregion

            var pathsBox = new VisualElement();
            pathsBox.style.borderTopRightRadius = 0;
            pathsBox.style.borderTopLeftRadius = 0;
            pathsBox.style.paddingTop = 2;
            pathsBox.style.paddingBottom = 2;
            pathsBox.AddToClassList(StatusEffectsStyleSheet.BoxGroupClassName);
            pathsTab.Add(pathsBox);

            var defaultStatusDataPath = new PropertyField();
            defaultStatusDataPath.BindProperty(defaultStatusDataPathProperty);
            pathsBox.Add(defaultStatusDataPath);
            var defaultStatusNamesPath = new PropertyField();
            defaultStatusNamesPath.BindProperty(defaultStatusNamesPathProperty);
            pathsBox.Add(defaultStatusNamesPath);
            var defaultComparableNamesPath = new PropertyField();
            defaultComparableNamesPath.BindProperty(defaultComparableNamesPathProperty);
            pathsBox.Add(defaultComparableNamesPath);
            var defaultModulesPath = new PropertyField();
            defaultModulesPath.BindProperty(defaultModulesPathProperty);
            pathsBox.Add(defaultModulesPath);
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

        [SettingsProvider]
        public static SettingsProvider CreateStatusEffectSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/StatusEffectFramework", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "Status Effect Framework",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = (searchContext, root) =>
                {
                    root.style.paddingLeft = 10;
                    root.style.paddingRight = 8;
                    root.style.paddingBottom = 8;
                    var header = new Label("Status Effect Framework");
                    header.style.marginTop = 2;
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.style.fontSize = 19;
                    root.Add(header);
                    var space = new VisualElement();
                    space.style.minHeight = 8;
                    space.style.flexGrow = 0;
                    root.Add(space);
                    var settings = CreateEditor(StatusEffectSettings.GetOrCreateSettings()).CreateInspectorGUI();
#if UNITY_2023_1_OR_NEWER
                    var tabView = settings.Q<TabView>();
                    tabView.style.overflow = Overflow.Hidden;
                    tabView.style.borderTopLeftRadius = 6;
                    tabView.style.borderTopRightRadius = 6;
#endif
                    root.Add(settings);
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Status", "Effect", "Group", "Stack", "Framework" })
            };

            return provider;
        }
    }
}
