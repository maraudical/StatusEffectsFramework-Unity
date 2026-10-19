#if UNITY_2023_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace StatusEffectsFramework.Editor
{
    /// <summary>
    /// The content shown when a row of an <see cref="AssetListSection{T}"/> is expanded.
    /// </summary>
    internal sealed class ListItemDetails
    {
        public readonly VisualElement Content;
        /// <summary>
        /// Called after <see cref="Content"/> was removed, to release what was created for it.
        /// </summary>
        public readonly Action Dispose;

        public ListItemDetails(VisualElement content, Action dispose = null)
        {
            Content = content;
            Dispose = dispose;
        }
    }

    /// <summary>
    /// Shows the properties of an asset the way an inspector does, without the script property.
    /// </summary>
    internal static class InspectorFields
    {
        public static bool Any(UnityEngine.Object target)
        {
            using var serializedObject = new SerializedObject(target);
            var iterator = serializedObject.GetIterator();
            // Skip the script property
            iterator.NextVisible(true);
            return iterator.NextVisible(true);
        }

        public static ListItemDetails Create(UnityEngine.Object target)
        {
            var serializedObject = new SerializedObject(target);
            var root = new VisualElement();

            var iterator = serializedObject.GetIterator();
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

            return new ListItemDetails(root, serializedObject.Dispose);
        }
    }

    /// <summary>
    /// A tab of the settings that lists every asset of type <typeparamref name="T"/> in the project, with search,
    /// rename, delete and an optional expandable details panel for each row.
    /// </summary>
    internal sealed class AssetListSection<T> where T : UnityEngine.Object
    {
        /// <summary>
        /// Shows the icon of a row. By default the icon of the asset is used.
        /// </summary>
        public Action<Image, T> UpdateIcon = (icon, item) => icon.image = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(item));
        /// <summary>
        /// The text under the name of a row. By default the key of the registrant.
        /// </summary>
        public Func<T, string> GetSubtext = item => item is Registrant registrant ? registrant.Key : string.Empty;
        /// <summary>
        /// Whether a row can be expanded. Only used by rows that have a foldout.
        /// </summary>
        public Func<T, bool> HasDetails;
        /// <summary>
        /// Creates the content shown when a row is expanded. It receives the row so it can update it.
        /// </summary>
        public Func<T, VisualElement, ListItemDetails> CreateDetails;
        /// <summary>
        /// Called when the add button of the list is pressed.
        /// </summary>
        public Action OnAdd;

        /// <summary>
        /// Where the add button of the list is, to place popups next to it.
        /// </summary>
        public Rect AddButtonBounds => (m_ListView.Q(BaseListView.footerAddButtonName) ?? m_ListView).worldBound;

        private const long k_SearchDelayMilliseconds = 150;

        private readonly TabView m_TabView;
        private readonly Tab m_Tab;
        private readonly VisualTreeAsset m_RowTemplate;
        private readonly ToolbarSearchField m_SearchBar;
        private readonly ListView m_ListView;
        private readonly List<T> m_Items = new();

        private SearchContext m_Context;
        private IVisualElementScheduledItem m_PendingSearch;
        private bool m_Active;

        /// <summary>
        /// State of a row. Rows are reused for different items, so the handlers set up when a row is made read the
        /// current item from here.
        /// </summary>
        private sealed class RowState
        {
            public T Item;
            public T RenameTarget;
            public Toggle FoldoutToggle;
            public ListItemDetails Details;
        }

        public AssetListSection(TabView tabView, Tab tab, VisualTreeAsset rowTemplate)
        {
            m_TabView = tabView;
            m_Tab = tab;
            m_RowTemplate = rowTemplate;
            m_SearchBar = tab.Q<ToolbarSearchField>();
            m_ListView = tab.Q<ListView>();

            m_ListView.itemsSource = m_Items;
            m_ListView.makeItem = MakeItem;
            m_ListView.bindItem = (element, index) => BindItem(element, m_Items[index]);
            m_ListView.unbindItem = (element, _) => UnbindItem(element);
            m_ListView.onAdd += _ =>
            {
                m_ListView.ClearSelection();
                OnAdd?.Invoke();
            };
            m_ListView.onRemove += RemoveSelectedItems;

            var scroll = m_ListView.Q<ScrollView>();
            scroll.style.borderTopRightRadius = 0;
            scroll.style.borderTopLeftRadius = 0;

            m_SearchBar.RegisterValueChangedCallback(_ => ScheduleSearch());

            tab.RegisterCallback<AttachToPanelEvent>(_ => Subscribe());
            tab.RegisterCallback<DetachFromPanelEvent>(_ => Unsubscribe());
            if (tab.panel != null)
                Subscribe();
        }

        /// <summary>
        /// Adds a newly created asset to the list and selects it.
        /// </summary>
        public void AddCreatedAsset(T item)
        {
            if (!m_Items.Contains(item))
                m_Items.Add(item);

            m_ListView.Rebuild();

            int index = m_Items.IndexOf(item);
            m_ListView.ScrollToItem(index);
            m_ListView.SetSelection(index);
            EditorGUIUtility.PingObject(item);
        }

        #region Rows
        private VisualElement MakeItem()
        {
            var state = new RowState();
            var root = new VisualElement { userData = state };
            m_RowTemplate.CloneTree(root);

            var foldout = root.Q<Foldout>("foldout");
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
                state.FoldoutToggle = foldoutToggle;
                foldoutToggle.style.marginBottom = 0;
                foldoutToggle.style.marginRight = 0;
                foldoutToggle.style.marginTop = 0;
                var checkmark = foldout.Q("unity-checkmark");
                checkmark.style.marginLeft = 5;

                var foldoutRow = root.Q("foldout-row");

                foldout.RegisterCallbackOnce<GeometryChangedEvent>((_) => { foldout.hierarchy.Insert(0, foldoutRow); foldoutRow.Insert(0, foldoutToggle); });

                foldoutToggle.RegisterValueChangedCallback(changeEvent =>
                {
                    CloseDetails(foldout, state);
                    if (changeEvent.newValue)
                        OpenDetails(foldout, root, state);
                });
            }

            var info = root.Q("info");
            var nameContainer = root.Q("name-container");
            var title = root.Q<Label>("name");

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
            {
                scriptButton.iconImage = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
                scriptButton.clicked += () =>
                {
                    if (state.Item is ScriptableObject item)
                        EditorGUIUtility.PingObject(MonoScript.FromScriptableObject(item));
                };
            }

            var pingButton = root.Q<Button>("ping-button");
            pingButton.iconImage = EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D;
            pingButton.clicked += () => EditorGUIUtility.PingObject(state.Item);

            info.RegisterCallback<PointerEnterEvent>((_) => { renameIcon.style.display = DisplayStyle.Flex; buttonContainer.style.display = DisplayStyle.Flex; });
            info.RegisterCallback<PointerLeaveEvent>((_) => { renameIcon.style.display = DisplayStyle.None; buttonContainer.style.display = DisplayStyle.None; });

            title.RegisterCallback<PointerUpEvent>((_) =>
            {
                state.RenameTarget = state.Item;
                renameContainer.style.display = DisplayStyle.Flex;
                nameContainer.style.display = DisplayStyle.None;
                renameField.value = title.text;
                renameField.Focus();
            });
            renameField.RegisterCallback<BlurEvent>((_) =>
            {
                renameContainer.style.display = DisplayStyle.None;
                nameContainer.style.display = DisplayStyle.Flex;

                var item = state.RenameTarget;
                state.RenameTarget = null;
                var value = renameField.value;
                renameField.value = title.text;
                if (item == null || string.IsNullOrEmpty(value) || value == title.text)
                    return;

                if (string.IsNullOrEmpty(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(item), value)))
                {
                    title.text = item.name;
                    return;
                }

                title.experimental.animation.Start(Color.red, Color.clear, 300, (element, color) =>
                {
                    element.style.backgroundColor = color;
                }).Ease(Easing.OutCubic);
            });

            return root;
        }

        private void BindItem(VisualElement element, T item)
        {
            var state = (RowState)element.userData;
            state.Item = item;
            element.style.visibility = item == null ? Visibility.Hidden : Visibility.Visible;
            if (item == null)
                return;

            element.Q<Label>("name").text = item.name;
            element.Q<Label>("subtext").text = GetSubtext(item);
            UpdateIcon(element.Q<Image>("icon"), item);

            if (state.FoldoutToggle != null && HasDetails != null)
                state.FoldoutToggle.style.display = HasDetails(item) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UnbindItem(VisualElement element)
        {
            var state = (RowState)element.userData;
            state.Item = null;
            // Clearing the rename target stops a rename that is still open from being applied to the next item.
            state.RenameTarget = null;

            var renameField = element.Q<TextField>("rename-field");
            renameField.Blur();
            element.Q("rename-container").style.display = DisplayStyle.None;
            element.Q("name-container").style.display = DisplayStyle.Flex;

            var foldout = element.Q<Foldout>("foldout");
            if (foldout != null)
            {
                foldout.SetValueWithoutNotify(false);
                CloseDetails(foldout, state);
            }
        }

        private void OpenDetails(Foldout foldout, VisualElement row, RowState state)
        {
            if (state.Item == null || CreateDetails == null)
                return;

            state.Details = CreateDetails(state.Item, row);
            foldout.Add(state.Details.Content);
        }

        private static void CloseDetails(Foldout foldout, RowState state)
        {
            foldout.Clear();
            state.Details?.Dispose?.Invoke();
            state.Details = null;
        }
        #endregion

        #region Search
        // Search only runs while the tab is shown and attached to a window.
        private void Subscribe()
        {
            m_TabView.activeTabChanged += OnActiveTabChanged;
            if (m_TabView.activeTab == m_Tab)
                Activate();
        }

        private void Unsubscribe()
        {
            m_TabView.activeTabChanged -= OnActiveTabChanged;
            Deactivate();
        }

        private void OnActiveTabChanged(Tab previous, Tab current)
        {
            if (previous == m_Tab)
                Deactivate();
            if (current == m_Tab)
                Activate();
        }

        private void Activate()
        {
            if (m_Active)
                return;

            m_Active = true;
            SearchMonitor.contentRefreshed += OnContentRefreshed;
            StartSearch();
        }

        private void Deactivate()
        {
            if (!m_Active)
                return;

            m_Active = false;
            SearchMonitor.contentRefreshed -= OnContentRefreshed;
            m_PendingSearch?.Pause();
            DisposeContext();
        }

        // Waits for the typing to stop, so not every key press throws away a running search.
        private void ScheduleSearch()
        {
            m_PendingSearch?.Pause();
            if (!m_Active)
                return;

            m_PendingSearch = m_SearchBar.schedule.Execute(StartSearch).StartingIn(k_SearchDelayMilliseconds);
        }

        private void StartSearch()
        {
            DisposeContext();
            m_Items.Clear();
            m_ListView.Rebuild();

            m_Context = SearchService.CreateContext($"t={typeof(T).Name} {m_SearchBar.value}");
            SearchService.Request(m_Context, OnSearchResults, default, SearchFlags.None);
        }

        private void DisposeContext()
        {
            m_Context?.Dispose();
            m_Context = null;
        }

        private void OnSearchResults(SearchContext context, IEnumerable<SearchItem> items)
        {
            // Results of a search that was replaced in the meantime must not be added.
            if (context != m_Context)
                return;

            foreach (var item in items)
            {
                var itemObject = item.ToObject<T>();
                if (itemObject != null && !m_Items.Contains(itemObject))
                    m_Items.Add(itemObject);
            }

            m_ListView.Rebuild();
        }

        private void OnContentRefreshed(IEnumerable<string> updated, IEnumerable<string> removed, IEnumerable<string> moved)
        {
            bool needsRefresh = false;

            var removedPaths = removed.ToHashSet();
            if (removedPaths.Count > 0)
                needsRefresh = m_Items.RemoveAll(item => item == null || removedPaths.Contains(AssetDatabase.GetAssetPath(item))) > 0;

            foreach (var path in updated)
            {
                // Loading an asset is expensive, so only the ones of this type are loaded.
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == null || !typeof(T).IsAssignableFrom(type))
                    continue;

                var item = AssetDatabase.LoadAssetAtPath<T>(path);
                if (item != null && !m_Items.Contains(item))
                {
                    needsRefresh = true;
                    m_Items.Add(item);
                }
            }

            if (needsRefresh)
                m_ListView.Rebuild();
        }
        #endregion

        private void RemoveSelectedItems(BaseListView listView)
        {
            var indices = listView.selectedIndices.ToList();
            var pathsByIndex = indices.ToDictionary(index => index, index => AssetDatabase.GetAssetPath(m_Items[index]));
            var paths = pathsByIndex.Values.Where(path => !string.IsNullOrEmpty(path)).ToList();

            if (paths.Count <= 0)
            {
                EditorUtility.DisplayDialog("Delete selected asset?", "Nothing is selected.\n", "Ok");
                return;
            }

            if (!EditorUtility.DisplayDialog($"Delete selected asset{(paths.Count > 1 ? "s" : "")}?", $"{string.Join("\n", paths)}\n\nYou cannot undo the delete assets action.\n", "Delete", "Cancel"))
                return;

            var failedPaths = new List<string>();
            AssetDatabase.DeleteAssets(paths.ToArray(), failedPaths);

            // Only remove the rows of the assets that were actually deleted.
            listView.viewController.RemoveItems(indices.Where(index => !failedPaths.Contains(pathsByIndex[index])).ToList());
            listView.ClearSelection();

            if (failedPaths.Count > 0)
                EditorUtility.DisplayDialog("Some assets could not be deleted", string.Join("\n", failedPaths) + "\n", "Ok");
        }
    }
}
#endif
