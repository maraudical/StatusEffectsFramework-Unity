using StatusEffectFramework.Templates;
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
using static StatusEffectFramework.Editor.ModulePopup;
using static StatusEffectFramework.Editor.StatusNamePopup;
using static StatusEffectFramework.Editor.DynamicEffectPopup;

namespace StatusEffectFramework.Editor
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
            ModuleButton.SetEnabled(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Any(type => type.IsSubclassOf(typeof(Module)) && !type.IsAbstract));
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
            
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Module)) && !type.IsAbstract);
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
            DynamicEffectButton.SetEnabled(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Any(type => type.IsSubclassOf(typeof(DynamicEffect)) && !type.IsAbstract));
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

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(DynamicEffect)) && !type.IsAbstract);
            foreach (var type in types)
            {
                var moduleButton = new Button(() => SelectionType(type)) { text = type.Name };
                root.Add(moduleButton);
            }

            return root;
        }
    }

    [CustomEditor(typeof(StatusEffectSettings))]
    [CanEditMultipleObjects]
    internal class StatusEffectSettingsEditor : UnityEditor.Editor
    {
        public VisualTreeAsset StatusEffectSettingsVisualTree;
        public VisualTreeAsset StatusEffectSettingsDataVisualTree;
        public VisualTreeAsset StatusEffectSettingsNameVisualTree;
        public VisualTreeAsset StatusEffectSettingsScriptTypeVisualTree;

        public override VisualElement CreateInspectorGUI()
        {
            var groupsProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.Groups));
            var defaultStatusDataPathProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.DefaultStatusDataPath));
            var defaultStatusNamesPathProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.DefaultStatusNamesPath));
            var defaultComparableNamesPathProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.DefaultComparableNamesPath));
            var defaultModulesPathProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.DefaultModulesPath));
            var defaultDynamicEffectsPathProperty = serializedObject.FindProperty(nameof(StatusEffectSettings.DefaultDynamicEffectsPath));

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

            #region List View Methods
            VisualElement MakeListItem(VisualTreeAsset visualTreeAsset)
            {
                var root = new VisualElement();
                visualTreeAsset.CloneTree(root);
                
                var foldout = root.Q<Foldout>();
                if (foldout != null)
                {
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

                    var foldoutRow = root.Q("foldout-row");

                    foldout.RegisterCallbackOnce<GeometryChangedEvent>((_) => { foldout.hierarchy.Insert(0, foldoutRow); foldoutRow.Insert(0, foldoutToggle); });
                }

                var info = root.Q("info");

                var nameContainer = root.Q("name-container");

                var name = root.Q<Label>("name");

                var renameIcon = root.Q("rename-icon");
                renameIcon.style.backgroundImage = EditorGUIUtility.IconContent("editicon.sml").image as Texture2D;

                var renameContainer = root.Q("rename-container");

                var renameField = root.Q<TextField>("rename-field");
                var renameText = renameField.Q<TextElement>();
                renameText.style.marginLeft = 0;
                renameText.style.paddingTop = 0;
                renameText.style.paddingRight = 0;

                var buttonContainer = root.Q("button-container");

                var scriptButton = root.Q<Button>("script-button");
                if (scriptButton != null)
                    scriptButton.iconImage = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

                var pingButton = root.Q<Button>("ping-button");
                pingButton.iconImage = EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D;

                info.RegisterCallback<PointerEnterEvent>((_) => { renameIcon.style.display = DisplayStyle.Flex; buttonContainer.style.display = DisplayStyle.Flex; });
                info.RegisterCallback<PointerLeaveEvent>((_) => { renameIcon.style.display = DisplayStyle.None; buttonContainer.style.display = DisplayStyle.None; });

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

            void BindListItem<T>(VisualElement element, int index, T item) where T : UnityEngine.Object
            {
                if (item == null)
                {
                    element.style.display = DisplayStyle.None;
                    return;
                }
                element.Q<Button>("ping-button").clicked += () => EditorGUIUtility.PingObject(item);
                var title = element.Q<Label>("name");
                title.text = item.name;
                var renameField = element.Q<TextField>("rename-field");
                renameField.RegisterCallback<BlurEvent>((_) =>
                {
                    var value = renameField.value;
                    renameField.value = title.text;
                    if (string.IsNullOrEmpty(value) || value == title.text)
                        return;

                    if (string.IsNullOrEmpty(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(item), value)))
                        return;

                    title.experimental.animation.Start(Color.red, Color.clear, 300, (element, color) =>
                    {
                        element.style.backgroundColor = color;
                    }).Ease(Easing.OutCubic);
                });
            }

            void InitializeListSubscriptions<T>(List<T> list, ListView listView, Tab tab, ToolbarSearchField searchBar) where T : UnityEngine.Object
            {
                SearchContext context = null;
                
                listView.onRemove += OnRemoveItem;
                tabView.activeTabChanged += TabCheck;
                searchBar.RegisterValueChangedCallback(SearchRefresh);
                tab.RegisterCallback<DetachFromPanelEvent>(_ =>
                {
                    context?.Dispose();
                    SearchMonitor.contentRefreshed -= OnContentRefreshed;
                });
                TabCheck(null, tabView.activeTab);

                void OnContentRefreshed(IEnumerable<string> updated, IEnumerable<string> removed, IEnumerable<string> moved)
                {
                    bool needsRefresh = false;
                    if (removed.Any())
                    {
                        needsRefresh = true;
                        list.RemoveAll(item => { return item == null || removed.Contains(AssetDatabase.GetAssetPath(item)); });
                    }
                    if (updated.Any())
                    {
                        foreach (var path in updated)
                        {
                            var item = AssetDatabase.LoadAssetAtPath<T>(path);
                            if (item != null && !list.Contains(item))
                            {
                                needsRefresh = true;
                                list.Add(item);
                            } 
                        }
                    }
                    if (needsRefresh)
                        listView.Rebuild();
                }

                void OnRemoveItem(BaseListView listView)
                {
                    List<string> paths = new();
                    foreach (var index in listView.selectedIndices)
                        paths.Add(AssetDatabase.GetAssetPath(list[index]));

                    if (paths.Count <= 0)
                    {
                        NothingSelectedDialogue();
                        return;
                    }

                    if (!DeleteAssetsDialogue(paths))
                        return;
                    
                    listView.viewController.RemoveItems(listView.selectedIndices.ToList());
                    listView.ClearSelection();
                    
                    AssetDatabase.DeleteAssets(paths.ToArray(), paths);
                }

                void TabCheck(Tab previous, Tab current)
                {
                    if (previous == tab)
                    {
                        context?.Dispose();
                        SearchMonitor.contentRefreshed -= OnContentRefreshed;
                    }
                    if (current == tab)
                    {
                        list.Clear();
                        listView.Rebuild();
                        context = SearchService.CreateContext($"t={typeof(T).Name} {searchBar.value}");
                        SearchService.Request(context, SearchResults, default, SearchFlags.None);
                        SearchMonitor.contentRefreshed += OnContentRefreshed;
                    }
                }

                void SearchRefresh(ChangeEvent<string> _)
                {
                    ItemsChanged();
                }

                void ItemsChanged()
                {
                    context?.Dispose();
                    list.Clear();
                    listView.Rebuild();
                    context = SearchService.CreateContext($"t={typeof(T).Name} {searchBar.value}");
                    SearchService.Request(context, SearchResults, default, SearchFlags.None);
                }

                void SearchResults(SearchContext context, IEnumerable<SearchItem> items)
                {
                    foreach (var item in items)
                    {
                        var itemObject = item.ToObject<T>();
                        if (itemObject == null)
                            continue;
                        list.Add(itemObject);
                    }
                    listView.Rebuild();
                }

                bool NothingSelectedDialogue() => EditorUtility.DisplayDialog("Delete selected asset?", "Nothing is selected.\n", "Ok");
                bool DeleteAssetsDialogue(IEnumerable<string> paths) => EditorUtility.DisplayDialog($"Delete selected asset{(paths.Count() > 1 ? "s" : "")}?", $"{string.Join("\n", paths)}\n\nYou cannot undo the delete assets action.\n", "Delete", "Cancel");

            }
            #endregion

            #region Datas Section
            var dataSearchBar = datasTab.Q<ToolbarSearchField>();
            var datas = new List<StatusEffectData>();
            var datasListView = datasTab.Q<ListView>();
            datasListView.itemsSource = datas;
            datasListView.makeItem = () => MakeListItem(StatusEffectSettingsDataVisualTree);
            datasListView.bindItem = (element, index) =>
            {
                var data = datas[index];
                if (data == null)
                    return;
                var foldout = element.Q<Foldout>("foldout");
                var icon = element.Q<Image>("icon");
                SetIcon(data.Icon);
                element.Q<Label>("subtext").text = data.Id.ToString();
                BindListItem(element, index, data);
                foldout.Q<Toggle>().RegisterValueChangedCallback((changeEvent) =>
                {
                    if (changeEvent.newValue)
                    {
                        var editor = CreateEditor(data).CreateInspectorGUI();
                        editor.Q<PropertyField>("icon").RegisterValueChangeCallback(IconChanged);
                        foldout.Add(editor);

                        void IconChanged(SerializedPropertyChangeEvent evt) => SetIcon(evt.changedProperty.objectReferenceValue as Sprite);
                    }
                    else
                        foldout.Clear();
                });

                void SetIcon(Sprite sprite)
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
            };
            datasListView.onAdd += (listView) =>
            {
                listView.ClearSelection();
                Directory.CreateDirectory(Path.Combine(Application.dataPath, defaultStatusDataPathProperty.stringValue));
                var path = EditorUtility.SaveFilePanelInProject("Creating new StatusEffectData", "", "asset", "Enter the name of the new StatusEffectData", Path.Combine("Assets", defaultStatusDataPathProperty.stringValue));
                if (string.IsNullOrEmpty(path))
                    return;
                var data = CreateInstance<StatusEffectData>();
                AssetDatabase.CreateAsset(data, path);
                var itemsSourceCount = listView.itemsSource.Count;
                listView.itemsSource.Add(data);
                listView.ScrollToItem(itemsSourceCount);
                EditorGUIUtility.PingObject(data);
            };
            InitializeListSubscriptions(datas, datasListView, datasTab, dataSearchBar);

            var dataScroll = datasListView.Q<ScrollView>();
            dataScroll.style.borderTopRightRadius = 0;
            dataScroll.style.borderTopLeftRadius = 0;
            #endregion

            #region Names Section
            var nameSearchBar = namesTab.Q<ToolbarSearchField>();

            var names = new List<StatusName>();
            var namesListView = namesTab.Q<ListView>();
            VisualElement namesAddButton = null;
            namesListView.RegisterCallbackOnce<GeometryChangedEvent>((_) => { namesAddButton = namesListView.Q(BaseListView.footerAddButtonName); });
            namesListView.itemsSource = names;
            namesListView.makeItem = () => MakeListItem(StatusEffectSettingsNameVisualTree);
            namesListView.bindItem = (element, index) =>
            {
                var name = names[index];
                if (name == null)
                    return;
                element.Q<Image>("icon").image = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(name));
                element.Q<Label>("subtext").text = name.Id.ToString();
                BindListItem(element, index, name);
            };
            namesListView.onAdd += (listView) =>
            {
                listView.ClearSelection();
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
                    var itemsSourceCount = listView.itemsSource.Count;
                    listView.itemsSource.Add(name);
                    listView.ScrollToItem(itemsSourceCount);
                    EditorGUIUtility.PingObject(name);
                }
            };
            InitializeListSubscriptions(names, namesListView, namesTab, nameSearchBar);

            var nameScroll = namesListView.Q<ScrollView>();
            nameScroll.style.borderTopRightRadius = 0;
            nameScroll.style.borderTopLeftRadius = 0;
            #endregion

            #region Comparables Section
            var comparableSearchBar = comparablesTab.Q<ToolbarSearchField>();

            var comparables = new List<ComparableName>();
            var comparablesListView = comparablesTab.Q<ListView>();
            comparablesListView.itemsSource = comparables;
            comparablesListView.makeItem = () => MakeListItem(StatusEffectSettingsNameVisualTree);
            comparablesListView.bindItem = (element, index) =>
            {
                var comparable = comparables[index];
                if (comparable == null)
                    return;
                element.Q<Image>("icon").image = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(comparable));
                element.Q<Label>("subtext").text = comparable.Id.ToString();
                BindListItem(element, index, comparable);
            };
            comparablesListView.onAdd += (listView) =>
            {
                listView.ClearSelection();
                Directory.CreateDirectory(Path.Combine(Application.dataPath, defaultComparableNamesPathProperty.stringValue));
                var path = EditorUtility.SaveFilePanelInProject("Creating new ComparableName", "", "asset", "Enter the name of the new ComparableName", Path.Combine("Assets", defaultComparableNamesPathProperty.stringValue));
                if (string.IsNullOrEmpty(path))
                    return;

                ComparableName comparable = CreateInstance<ComparableName>();
                AssetDatabase.CreateAsset(comparable, path);
                var itemsSourceCount = listView.itemsSource.Count;
                listView.itemsSource.Add(comparable);
                listView.ScrollToItem(itemsSourceCount);
                EditorGUIUtility.PingObject(comparable);
            };
            InitializeListSubscriptions(comparables, comparablesListView, comparablesTab, comparableSearchBar);

            var comparableScroll = comparablesListView.Q<ScrollView>();
            comparableScroll.style.borderTopRightRadius = 0;
            comparableScroll.style.borderTopLeftRadius = 0;
            #endregion

            #region Modules Section
            var moduleSearchBar = modulesTab.Q<ToolbarSearchField>();

            var modules = new List<Module>();
            var moduleListView = modulesTab.Q<ListView>();
            VisualElement modulesAddButton = null;
            moduleListView.RegisterCallbackOnce<GeometryChangedEvent>((_) => { modulesAddButton = moduleListView.Q(BaseListView.footerAddButtonName); });
            moduleListView.itemsSource = modules;
            moduleListView.makeItem = () => MakeListItem(StatusEffectSettingsScriptTypeVisualTree);
            moduleListView.bindItem = (element, index) =>
            {
                var module = modules[index];
                if (module == null)
                    return;
                var foldout = element.Q<Foldout>("foldout");
                var foldoutToggle = foldout.Q<Toggle>();
                var modulePropertyCheck = CreateEditor(module).serializedObject.GetIterator();
                modulePropertyCheck.NextVisible(true);
                foldoutToggle.style.display = modulePropertyCheck.NextVisible(true) ? DisplayStyle.Flex : DisplayStyle.None;
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
                        foldout.Clear();
                });
                element.Q<Image>("icon").image = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(module));
                var script = MonoScript.FromScriptableObject(module);
                element.Q<Label>("subtext").text = script.GetClass().ToString();
                element.Q<Button>("script-button").clicked += () => EditorGUIUtility.PingObject(script);
                BindListItem(element, index, module);
            };
            moduleListView.onAdd += (listView) =>
            {
                listView.ClearSelection();
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
                            var itemsSourceCount = listView.itemsSource.Count;
                            listView.itemsSource.Add(module);
                            listView.ScrollToItem(itemsSourceCount);
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
            };
            InitializeListSubscriptions(modules, moduleListView, modulesTab, moduleSearchBar);

            var moduleScroll = moduleListView.Q<ScrollView>();
            moduleScroll.style.borderTopRightRadius = 0;
            moduleScroll.style.borderTopLeftRadius = 0;
            #endregion

            #region Dynamic Effects Section
            var dynamicEffectSearchBar = dynamicEffectsTab.Q<ToolbarSearchField>();

            var dynamicEffects = new List<DynamicEffect>();
            var dynamicEffectListView = dynamicEffectsTab.Q<ListView>();
            VisualElement dynamicEffectsAddButton = null;
            dynamicEffectListView.RegisterCallbackOnce<GeometryChangedEvent>((_) => { dynamicEffectsAddButton = dynamicEffectListView.Q(BaseListView.footerAddButtonName); });
            dynamicEffectListView.itemsSource = dynamicEffects;
            dynamicEffectListView.makeItem = () => MakeListItem(StatusEffectSettingsScriptTypeVisualTree);
            dynamicEffectListView.bindItem = (element, index) =>
            {
                var dynamicEffect = dynamicEffects[index];
                if (dynamicEffect == null)
                    return;
                var foldout = element.Q<Foldout>("foldout");
                var foldoutToggle = foldout.Q<Toggle>();
                var dynamicEffectPropertyCheck = CreateEditor(dynamicEffect).serializedObject.GetIterator();
                dynamicEffectPropertyCheck.NextVisible(true);
                foldoutToggle.style.display = dynamicEffectPropertyCheck.NextVisible(true) ? DisplayStyle.Flex : DisplayStyle.None;
                foldoutToggle.RegisterValueChangedCallback((changeEvent) =>
                {
                    if (changeEvent.newValue)
                    {
                        var root = new VisualElement();
                        var instance = CreateEditor(dynamicEffect).serializedObject;

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
                        foldout.Clear();
                });
                element.Q<Image>("icon").image = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(dynamicEffect));
                var script = MonoScript.FromScriptableObject(dynamicEffect);
                element.Q<Label>("subtext").text = script.GetClass().ToString();
                element.Q<Button>("script-button").clicked += () => EditorGUIUtility.PingObject(script);
                BindListItem(element, index, dynamicEffect);
            };
            dynamicEffectListView.onAdd += (listView) =>
            {
                listView.ClearSelection();
                var popup = new DynamicEffectPopup();
                popup.SelectionType += OnSelectionType;
                UnityEditor.PopupWindow.Show(dynamicEffectsAddButton.worldBound, popup);

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
                            Directory.CreateDirectory(Path.Combine(Application.dataPath, defaultDynamicEffectsPathProperty.stringValue));
                            var path = EditorUtility.SaveFilePanelInProject("Creating new Dynamic Effect", "", "asset", "Enter the name of the new Dynamic Effect", Path.Combine("Assets", defaultDynamicEffectsPathProperty.stringValue));
                            if (string.IsNullOrEmpty(path))
                                return;
                            var dynamicEffect = CreateInstance(type);
                            AssetDatabase.CreateAsset(dynamicEffect, path);
                            var itemsSourceCount = listView.itemsSource.Count;
                            listView.itemsSource.Add(dynamicEffect);
                            listView.ScrollToItem(itemsSourceCount);
                            EditorGUIUtility.PingObject(dynamicEffect);
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
            InitializeListSubscriptions(dynamicEffects, dynamicEffectListView, dynamicEffectsTab, dynamicEffectSearchBar);

            var dynamicEffectScroll = dynamicEffectListView.Q<ScrollView>();
            dynamicEffectScroll.style.borderTopRightRadius = 0;
            dynamicEffectScroll.style.borderTopLeftRadius = 0;
#endregion

            var pathsBox = new VisualElement();
            pathsBox.style.borderTopRightRadius = 0;
            pathsBox.style.borderTopLeftRadius = 0;
            pathsBox.style.paddingTop = 2;
            pathsBox.style.paddingBottom = 2;
            pathsBox.AddToClassList(StatusEffectsStyleSheet.BoxGroupClassName);
            pathsTab.Add(pathsBox);

            var defaultStatusDataPathGroup = new VisualElement();
            defaultStatusDataPathGroup.style.flexDirection = FlexDirection.Row;
            pathsBox.Add(defaultStatusDataPathGroup);
            var defaultStatusDataPath = new PropertyField();
            defaultStatusDataPath.style.flexGrow = 1;
            defaultStatusDataPath.BindProperty(defaultStatusDataPathProperty);
            defaultStatusDataPathGroup.Add(defaultStatusDataPath);
            var defaultStatusDataPathButton = MakePathButton(defaultStatusDataPath);
            defaultStatusDataPathGroup.Add(defaultStatusDataPathButton);

            var defaultStatusNamesPathGroup = new VisualElement();
            defaultStatusNamesPathGroup.style.flexDirection = FlexDirection.Row;
            pathsBox.Add(defaultStatusNamesPathGroup);
            var defaultStatusNamesPath = new PropertyField();
            defaultStatusNamesPath.style.flexGrow = 1;
            defaultStatusNamesPath.BindProperty(defaultStatusNamesPathProperty);
            defaultStatusNamesPathGroup.Add(defaultStatusNamesPath);
            var defaultStatusNamesPathButton = MakePathButton(defaultStatusNamesPath);
            defaultStatusNamesPathGroup.Add(defaultStatusNamesPathButton);

            var defaultComparableNamesPathGroup = new VisualElement();
            defaultComparableNamesPathGroup.style.flexDirection = FlexDirection.Row;
            pathsBox.Add(defaultComparableNamesPathGroup);
            var defaultComparableNamesPath = new PropertyField();
            defaultComparableNamesPath.style.flexGrow = 1;
            defaultComparableNamesPath.BindProperty(defaultComparableNamesPathProperty);
            defaultComparableNamesPathGroup.Add(defaultComparableNamesPath);
            var defaultComparableNamesPathButton = MakePathButton(defaultComparableNamesPath);
            defaultComparableNamesPathGroup.Add(defaultComparableNamesPathButton);

            var defaultModulesPathGroup = new VisualElement();
            defaultModulesPathGroup.style.flexDirection = FlexDirection.Row;
            pathsBox.Add(defaultModulesPathGroup);
            var defaultModulesPath = new PropertyField();
            defaultModulesPath.style.flexGrow = 1;
            defaultModulesPath.BindProperty(defaultModulesPathProperty);
            defaultModulesPathGroup.Add(defaultModulesPath);
            var defaultModulesPathButton = MakePathButton(defaultModulesPath);
            defaultModulesPathGroup.Add(defaultModulesPathButton);

            var defaultDynamicEffectsPathGroup = new VisualElement();
            defaultDynamicEffectsPathGroup.style.flexDirection = FlexDirection.Row;
            pathsBox.Add(defaultDynamicEffectsPathGroup);
            var defaultDynamicEffectsPath = new PropertyField();
            defaultDynamicEffectsPath.style.flexGrow = 1;
            defaultDynamicEffectsPath.BindProperty(defaultDynamicEffectsPathProperty);
            defaultDynamicEffectsPathGroup.Add(defaultDynamicEffectsPath);
            var defaultDynamicEffectsPathButton = MakePathButton(defaultDynamicEffectsPath);
            defaultDynamicEffectsPathGroup.Add(defaultDynamicEffectsPathButton);

            Button MakePathButton(PropertyField propertyField)
            {
                var button = new Button(EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D, () =>
                {
                    var path = EditorUtility.OpenFolderPanel("Select Path", "Assets", string.Empty);
                    if (string.IsNullOrWhiteSpace(path))
                        return;
                    path = Path.GetRelativePath(Application.dataPath, path);
                    if (path.StartsWith('.'))
                    {
                        EditorUtility.DisplayDialog("Invalid path", "Please select a folder within the Assets directory.\n", "Ok");
                        return;
                    }
                    propertyField.Q<TextField>().value = path;
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
