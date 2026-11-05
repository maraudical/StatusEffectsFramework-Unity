using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
#if ADDRESSABLES
using UnityEditor.AddressableAssets;
#endif
using ContentSource = StatusEffectsFramework.StatusRegistryManifest.ContentSource;

namespace StatusEffectsFramework.Editor
{
    [CustomEditor(typeof(StatusRegistryManifest))]
    internal class StatusRegistryManifestEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var manifest = (StatusRegistryManifest)target;
            var sourceProperty = serializedObject.FindProperty("m_Source");
            var groupProperty = serializedObject.FindProperty("m_AddressableGroupGuid");
            var bundleProperty = serializedObject.FindProperty("m_AssetBundleName");
            var folderProperty = serializedObject.FindProperty("m_Folder");
            var registrantsProperty = serializedObject.FindProperty("m_Registrants");

            var root = new VisualElement();

            // Every element is made before any callback is registered, since the callbacks refresh all of them.
            #region Source
            // Addressable Group is only offered with Addressables, but a manifest that already uses it keeps showing it.
            var sources = new List<ContentSource>();
#if ADDRESSABLES
            sources.Add(ContentSource.AddressableGroup);
#endif
            sources.Add(ContentSource.AssetBundle);
            sources.Add(ContentSource.Folder);
            sources.Add(ContentSource.Explicit);
            var source = (ContentSource)sourceProperty.intValue;
            if (!sources.Contains(source))
                sources.Insert(0, source);

            var sourceField = new PopupField<ContentSource>("Source", sources, source, FormatSource, FormatSource)
            {
                tooltip = "Where this manifest finds the registrants it lists.",
            };
            sourceField.AddToClassList(BaseField<ContentSource>.alignedFieldUssClassName);
            root.Add(sourceField);
            #endregion

            #region Addressable Group
#if ADDRESSABLES
            // Groups are chosen by GUID, so renaming one keeps it selected. An empty GUID is no group.
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var groupGuids = new List<string> { string.Empty };
            if (settings)
                foreach (var group in settings.groups)
                    if (group && !group.ReadOnly)
                        groupGuids.Add(group.Guid);
            var groupGuid = groupProperty.stringValue ?? string.Empty;
            if (!groupGuids.Contains(groupGuid))
                groupGuids.Add(groupGuid);

            var groupField = new PopupField<string>("Addressable Group", groupGuids, groupGuid, FormatGroup, FormatGroup)
            {
                tooltip = "The group whose registrants are listed. The manifest is added to the group and labelled, so LoadManifestsAsync finds it.",
            };
            groupField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            root.Add(groupField);
#endif
            #endregion

            #region Asset Bundle
            var bundleNames = new List<string> { string.Empty };
            bundleNames.AddRange(AssetDatabase.GetAllAssetBundleNames());
            var bundleName = bundleProperty.stringValue ?? string.Empty;
            if (!bundleNames.Contains(bundleName))
                bundleNames.Add(bundleName);

            var bundleField = new PopupField<string>("Asset Bundle", bundleNames, bundleName, FormatBundle, FormatBundle)
            {
                tooltip = "The AssetBundle whose registrants are listed. The manifest is given the same AssetBundle name, so it is built into it.",
            };
            bundleField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            root.Add(bundleField);
            #endregion

            #region Folder
            //TEST DELETE THESE
            root.Add(new IntegerField("tenst"));
            root.Add(new PropertyField(folderProperty));


            var folderRow = new VisualElement();
            folderRow.style.flexDirection = FlexDirection.Row;
            root.Add(folderRow);

            // Shows which folder is used, disabled until another one is picked.
            var folderField = new TextField("Folder")
            {
                isDelayed = true,
                tooltip = "The folder whose registrants are listed, subfolders included. Until another folder is picked, it is the folder this manifest is in.",
            };
            folderField.style.flexGrow = 1;
            folderField.style.flexShrink = 1;
            folderField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            folderRow.Add(folderField);

            var resetFolderButton = new Button(EditorGUIUtility.IconContent("Refresh").image as Texture2D)
            {
                tooltip = "Use the folder this manifest is in.",
            };
            EditorPathUtility.StyleIconButton(resetFolderButton);
            folderRow.Add(resetFolderButton);
            #endregion

            var regenerateField = new PropertyField(serializedObject.FindProperty("m_RegenerateAutomatically"), "Regenerate Automatically")
            {
                tooltip = "Regenerates when its content changes and before player and Addressables builds.",
            };
            root.Add(regenerateField);

            var helpBox = new HelpBox();
            root.Add(helpBox);

            var registrantsList = new ListView
            {
                headerTitle = "Registrants",
                showFoldoutHeader = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                viewDataKey = "registry-manifest-registrants",
            };
            registrantsList.style.marginTop = 4;
            registrantsList.makeItem = () => new ObjectField { objectType = typeof(Registrant), allowSceneObjects = false };
            registrantsList.bindItem = (element, index) =>
            {
                // Only an explicit manifest's registrants are picked by hand.
                var field = (ObjectField)element;
                field.BindProperty(registrantsProperty.GetArrayElementAtIndex(index));
                field.SetEnabled((ContentSource)sourceProperty.intValue == ContentSource.Explicit);
            };
            registrantsList.BindProperty(registrantsProperty);
            root.Add(registrantsList);

            var generateButton = new Button
            {
                text = "Regenerate",
                tooltip = "Lists the registrants the source holds now.",
            };
            generateButton.style.marginTop = 4;
            root.Add(generateButton);

            #region Callbacks
            sourceField.RegisterValueChangedCallback(evt =>
            {
                sourceProperty.intValue = (int)evt.newValue;
                serializedObject.ApplyModifiedProperties();
                RefreshSource();
            });
#if ADDRESSABLES
            groupField.RegisterValueChangedCallback(evt =>
            {
                groupProperty.stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
                RefreshHelp();
            });
#endif
            bundleField.RegisterValueChangedCallback(evt =>
            {
                bundleProperty.stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
                RefreshHelp();
            });
            folderField.RegisterValueChangedCallback(evt => SetFolder(evt.newValue.Trim().Replace('\\', '/').TrimEnd('/')));
            folderRow.Insert(1, EditorPathUtility.CreateFolderButton(() => StatusRegistryManifestGenerator.GetFolder(manifest),
                relativePath => SetFolder(string.IsNullOrEmpty(relativePath) ? "Assets" : "Assets/" + relativePath)));
            resetFolderButton.clicked += () => SetFolder(string.Empty);
            generateButton.clicked += () =>
            {
                serializedObject.ApplyModifiedProperties();
                StatusRegistryManifestGenerator.Generate(manifest);
                serializedObject.Update();
                registrantsList.RefreshItems();
                RefreshHelp();
            };
            #endregion

            RefreshFolder();
            RefreshSource();
            return root;

            void SetFolder(string folder)
            {
                folderProperty.stringValue = folder;
                serializedObject.ApplyModifiedProperties();
                RefreshFolder();
            }

            void RefreshSource()
            {
                var current = (ContentSource)sourceProperty.intValue;
                bool isExplicit = current == ContentSource.Explicit;
#if ADDRESSABLES
                groupField.style.display = Display(current == ContentSource.AddressableGroup);
#endif
                bundleField.style.display = Display(current == ContentSource.AssetBundle);
                folderRow.style.display = Display(current == ContentSource.Folder);
                regenerateField.style.display = Display(!isExplicit);
                generateButton.style.display = Display(!isExplicit);
                registrantsList.showAddRemoveFooter = isExplicit;
                registrantsList.reorderable = isExplicit;
                registrantsList.RefreshItems();
                RefreshHelp();
            }

            void RefreshFolder()
            {
                bool custom = !string.IsNullOrEmpty(folderProperty.stringValue);
                folderField.SetEnabled(custom);
                folderField.SetValueWithoutNotify(custom ? folderProperty.stringValue : $"Manifest Folder ({StatusRegistryManifestGenerator.GetManifestFolder(manifest)})");
                resetFolderButton.style.display = Display(custom);
                RefreshHelp();
            }

            void RefreshHelp()
            {
                var (message, type) = GetHelp();
                helpBox.text = message;
                helpBox.messageType = type;
                helpBox.style.display = Display(message != null);
            }

            (string, HelpBoxMessageType) GetHelp()
            {
                switch ((ContentSource)sourceProperty.intValue)
                {
                    case ContentSource.AddressableGroup:
#if ADDRESSABLES
                        if (string.IsNullOrEmpty(groupProperty.stringValue))
                            return ("Select the group whose registrants this manifest lists.", HelpBoxMessageType.Info);
                        if (!StatusRegistryManifestGenerator.FindGroup(settings, groupProperty.stringValue))
                            return ("The selected group no longer exists, so the registrants stay as they are. Select another group.", HelpBoxMessageType.Warning);
                        break;
#else
                        return ("Addressables isn't installed, so this manifest can't be filled from a group. Pick another source.", HelpBoxMessageType.Warning);
#endif
                    case ContentSource.AssetBundle:
                        if (bundleNames.Count == 1)
                            return ("No AssetBundle names are set yet. Set one at the bottom of an asset's or folder's Inspector.", HelpBoxMessageType.Info);
                        if (string.IsNullOrEmpty(bundleProperty.stringValue))
                            return ("Select the AssetBundle whose registrants this manifest lists.", HelpBoxMessageType.Info);
                        break;
                    case ContentSource.Folder:
                        var folder = StatusRegistryManifestGenerator.GetFolder(manifest);
                        if (!AssetDatabase.IsValidFolder(folder))
                            return ($"The folder {folder} doesn't exist, so the registrants stay as they are.", HelpBoxMessageType.Warning);
                        break;
                }

                int shared = CountListedElsewhere(manifest);
                if (shared > 0)
                    return ($"{shared} of these registrants are also listed in another manifest. Only one of the two can be registered at a time, since the registry skips keys it already has.", HelpBoxMessageType.Warning);
                return (null, HelpBoxMessageType.None);
            }
#if ADDRESSABLES

            string FormatGroup(string guid)
            {
                if (string.IsNullOrEmpty(guid))
                    return "None";
                var group = StatusRegistryManifestGenerator.FindGroup(settings, guid);
                return group ? group.Name : "Missing Group";
            }
#endif
        }

        private static string FormatSource(ContentSource source) => source switch
        {
            ContentSource.AddressableGroup => "Addressable Group",
            ContentSource.AssetBundle => "Asset Bundle",
            _ => source.ToString(),
        };

        private static string FormatBundle(string bundleName) => string.IsNullOrEmpty(bundleName) ? "None" : bundleName;

        private static DisplayStyle Display(bool visible) => visible ? DisplayStyle.Flex : DisplayStyle.None;

        // How many of the manifest's registrants another manifest asset lists too.
        private static int CountListedElsewhere(StatusRegistryManifest manifest)
        {
            var others = new HashSet<Registrant>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(StatusRegistryManifest)}"))
            {
                var other = AssetDatabase.LoadAssetAtPath<StatusRegistryManifest>(AssetDatabase.GUIDToAssetPath(guid));
                if (other && other != manifest)
                    others.UnionWith(other.Registrants);
            }

            int shared = 0;
            foreach (var registrant in manifest.Registrants)
                if (registrant && others.Contains(registrant))
                    shared++;
            return shared;
        }
    }
}
