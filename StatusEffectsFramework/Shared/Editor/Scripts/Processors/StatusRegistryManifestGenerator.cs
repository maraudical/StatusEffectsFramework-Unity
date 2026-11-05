using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
#if ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using ModificationEvent = UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent;
#endif
using ContentSource = StatusEffectsFramework.StatusRegistryManifest.ContentSource;

namespace StatusEffectsFramework.Editor
{
    /// <summary>
    /// Fills each <see cref="StatusRegistryManifest"/> from its source. Manifests set to regenerate automatically are refreshed
    /// when their content changes and before player and Scriptable Build Pipeline builds. AssetBundle and content directory
    /// builds have no callback to refresh from, so refreshing on changes is what keeps those manifests current.
    /// </summary>
    [InitializeOnLoad]
    internal static class StatusRegistryManifestGenerator
    {
        private static bool s_Queued;
        private static bool s_Generating;

        static StatusRegistryManifestGenerator()
        {
#if ADDRESSABLES
            AddressableAssetSettings.OnModificationGlobal += OnModification;
#endif
        }
#if ADDRESSABLES

        private static void OnModification(AddressableAssetSettings settings, ModificationEvent modification, object data)
        {
            if (!s_Generating && ShouldRegenerate(modification))
                Queue();
        }

        // The changes that can add, move or remove a registrant's entry, or rename or remove its group.
        private static bool ShouldRegenerate(ModificationEvent modification)
        {
            switch (modification)
            {
                case ModificationEvent.EntryAdded:
                case ModificationEvent.EntryCreated:
                case ModificationEvent.EntryMoved:
                case ModificationEvent.EntryRemoved:
                case ModificationEvent.EntryModified:
                case ModificationEvent.GroupRemoved:
                case ModificationEvent.GroupRenamed:
                case ModificationEvent.BatchModification:
                    return true;
                default:
                    return false;
            }
        }
#endif

        /// <summary>
        /// Regenerates the automatic manifests once on the next editor update. Changes usually arrive in bursts, such as one
        /// event per moved asset.
        /// </summary>
        internal static void Queue()
        {
            if (s_Queued || s_Generating)
                return;

            // Not delayCall, which waits for a repaint and so never runs while the editor is in the background.
            s_Queued = true;
            EditorApplication.update += GenerateQueued;
        }

        private static void GenerateQueued()
        {
            EditorApplication.update -= GenerateQueued;
            s_Queued = false;
            GenerateAll(automaticOnly: true, allowGroupChanges: true);
        }

        [MenuItem("Tools/Status Effects Framework/Regenerate Registry Manifests")]
        private static void GenerateFromMenu() => GenerateAll(automaticOnly: false, allowGroupChanges: true);

        /// <summary>
        /// Regenerates every manifest that fills itself from a source.
        /// </summary>
        /// <param name="automaticOnly">Only the manifests set to regenerate automatically.</param>
        /// <param name="allowGroupChanges">False during a build that has already collected its content, so manifests are
        /// refreshed but not moved into a group or AssetBundle.</param>
        internal static void GenerateAll(bool automaticOnly, bool allowGroupChanges)
        {
            if (s_Generating)
                return;

            s_Generating = true;
            try
            {
                foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(StatusRegistryManifest)}"))
                {
                    var manifest = AssetDatabase.LoadAssetAtPath<StatusRegistryManifest>(AssetDatabase.GUIDToAssetPath(guid));
                    if (manifest && (!automaticOnly || manifest.RegenerateAutomatically))
                        GenerateCore(manifest, allowGroupChanges);
                }
            }
            finally
            {
                s_Generating = false;
            }
        }

        /// <summary>
        /// Fills <paramref name="manifest"/> from its source. Does nothing for one that lists its registrants explicitly.
        /// </summary>
        internal static void Generate(StatusRegistryManifest manifest)
        {
            if (s_Generating || !manifest)
                return;

            s_Generating = true;
            try
            {
                GenerateCore(manifest, allowGroupChanges: true);
            }
            finally
            {
                s_Generating = false;
            }
        }

        private static void GenerateCore(StatusRegistryManifest manifest, bool allowGroupChanges)
        {
            // A source that can't be found, such as a deleted group, leaves the registrants as they were.
            if (!TryFindRegistrants(manifest, out var registrants))
                return;

            if (manifest.SetRegistrants(registrants))
            {
                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssetIfDirty(manifest);
            }

            if (allowGroupChanges)
                ShipWithContent(manifest);
        }

        /// <summary>
        /// The registrants <paramref name="manifest"/>'s source holds. Fails for an explicit manifest, or a source that
        /// isn't set or can't be found.
        /// </summary>
        internal static bool TryFindRegistrants(StatusRegistryManifest manifest, out List<Registrant> registrants)
        {
            registrants = new List<Registrant>();
            switch (manifest.Source)
            {
                case ContentSource.Folder:
                {
                    var folder = GetFolder(manifest);
                    if (!AssetDatabase.IsValidFolder(folder))
                        return false;

                    foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(Registrant)}", new[] { folder }))
                        AddRegistrant(registrants, AssetDatabase.GUIDToAssetPath(guid));
                    return true;
                }
                case ContentSource.AssetBundle:
                {
                    if (string.IsNullOrEmpty(manifest.AssetBundleName))
                        return false;

                    // The implicit name includes AssetBundle names set on a parent folder.
                    foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(Registrant)}"))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (AssetDatabase.GetImplicitAssetBundleName(path) == manifest.AssetBundleName)
                            AddRegistrant(registrants, path);
                    }
                    return true;
                }
#if ADDRESSABLES
                case ContentSource.AddressableGroup:
                {
                    var settings = AddressableAssetSettingsDefaultObject.Settings;
                    var group = FindGroup(settings, manifest.AddressableGroupGuid);
                    if (!group)
                        return false;

                    foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(Registrant)}"))
                        if (settings.FindAssetEntry(guid, includeImplicit: true)?.parentGroup == group)
                            AddRegistrant(registrants, AssetDatabase.GUIDToAssetPath(guid));
                    return true;
                }
#endif
                default:
                    return false;
            }

            static void AddRegistrant(List<Registrant> registrants, string path)
            {
                var registrant = AssetDatabase.LoadAssetAtPath<Registrant>(path);
                if (registrant && !string.IsNullOrEmpty(registrant.Key))
                    registrants.Add(registrant);
            }
        }

        // Puts the manifest where its content is built, so it ships, or is left out of a build, together with it.
        private static void ShipWithContent(StatusRegistryManifest manifest)
        {
            var path = AssetDatabase.GetAssetPath(manifest);
            switch (manifest.Source)
            {
                case ContentSource.AssetBundle:
                {
                    if (AssetDatabase.GetImplicitAssetBundleName(path) == manifest.AssetBundleName)
                        return;

                    var importer = AssetImporter.GetAtPath(path);
                    importer.assetBundleName = manifest.AssetBundleName;
                    importer.SaveAndReimport();
                    return;
                }
#if ADDRESSABLES
                case ContentSource.AddressableGroup:
                {
                    // The label is how LoadManifestsAsync finds it.
                    var settings = AddressableAssetSettingsDefaultObject.Settings;
                    var group = FindGroup(settings, manifest.AddressableGroupGuid);
                    if (!group)
                        return;

                    settings.AddLabel(StatusRegistryManifest.Label, postEvent: false);
                    var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group, postEvent: false);
                    entry.SetLabel(StatusRegistryManifest.Label, true, force: true, postEvent: false);
                    return;
                }
#endif
            }
        }

        /// <summary>
        /// The folder a folder manifest lists the registrants of, starting with Assets: its own setting, or else the folder
        /// the manifest is in.
        /// </summary>
        internal static string GetFolder(StatusRegistryManifest manifest)
        {
            return string.IsNullOrEmpty(manifest.Folder) ? GetManifestFolder(manifest) : manifest.Folder;
        }

        internal static string GetManifestFolder(StatusRegistryManifest manifest)
        {
            return Path.GetDirectoryName(AssetDatabase.GetAssetPath(manifest))?.Replace('\\', '/') ?? "Assets";
        }
#if ADDRESSABLES

        internal static AddressableAssetGroup FindGroup(AddressableAssetSettings settings, string guid)
        {
            return settings && !string.IsNullOrEmpty(guid) ? settings.FindGroup(group => group && group.Guid == guid) : null;
        }
#endif

        // A registrant that ships separately but no manifest lists is also built into the main registry, which duplicates it.
        private static void WarnAboutUnlistedContent()
        {
            var listed = StatusRegistryManifest.FindListedRegistrants();
#if ADDRESSABLES
            var settings = AddressableAssetSettingsDefaultObject.Settings;
#endif
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(Registrant)}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var bundle = AssetDatabase.GetImplicitAssetBundleName(path);
                var container = string.IsNullOrEmpty(bundle) ? null : $"the AssetBundle {bundle}";
#if ADDRESSABLES
                if (container == null && settings && settings.FindAssetEntry(guid, includeImplicit: true)?.parentGroup is { } group)
                    container = $"the Addressables group {group.Name}";
#endif
                if (container == null)
                    continue;

                var registrant = AssetDatabase.LoadAssetAtPath<Registrant>(path);
                if (registrant && !listed.Contains(registrant))
                    Debug.LogWarning($"{path} is in {container}, but no {nameof(StatusRegistryManifest)} lists it, so the main build includes a copy of it too. List it in a manifest that ships with that content.", registrant);
            }
        }

        /// <summary>
        /// Regenerates before player builds, ahead of Addressables building its content as part of them.
        /// </summary>
        internal sealed class PlayerBuildProcessor : BuildPlayerProcessor
        {
            // Addressables builds its content at order 1.
            public override int callbackOrder => 0;

            public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
            {
                GenerateAll(automaticOnly: true, allowGroupChanges: true);
                WarnAboutUnlistedContent();
            }
        }
    }

    /// <summary>
    /// Regenerates when a registrant is created, edited, moved or deleted, or an AssetBundle name changes, none of which
    /// raise an Addressables event.
    /// </summary>
    internal sealed class StatusRegistryManifestPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (deletedAssets.Length > 0 || HasRegistrant(importedAssets) || HasRegistrant(movedAssets))
                StatusRegistryManifestGenerator.Queue();
        }

        private void OnPostprocessAssetbundleNameChanged(string assetPath, string previousAssetBundleName, string newAssetBundleName)
        {
            StatusRegistryManifestGenerator.Queue();
        }

        private static bool HasRegistrant(string[] paths)
        {
            foreach (var path in paths)
                if (typeof(Registrant).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path)))
                    return true;
            return false;
        }
    }
}
