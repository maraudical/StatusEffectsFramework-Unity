using UnityEngine;
using System.Collections.Generic;
using System;
#if ENTITIES
using Hash128 = Unity.Entities.Hash128;
#endif
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
#endif

namespace StatusEffectsFramework
{
    /// <summary>
    /// Gives every <see cref="Registrant"/> an id, so status effects can be referenced compactly, for example over the network.
    /// </summary>
    /// <remarks>
    /// Registrants in the main build are listed in this asset. Registrants that are loaded separately, such as the ones in
    /// Addressables groups or mods, are referenced by a <see cref="StatusRegistryManifest"/>, so loading the manifest loads them
    /// and registering it gives them their ids. Every registered registrant is loaded. Ids are assigned in key order, so every
    /// registry with the same keys assigns the same ids. Rebuild only before gameplay, since a rebuild can change ids.
    /// </remarks>
    public class StatusRegistry : ScriptableObject
    {
#if UNITY_EDITOR
        /// <summary>
        /// Refreshes the registry asset before a build. Unity creates its own instance of every build
        /// callback type, so this is kept off <see cref="StatusRegistry"/> and preprocesses the asset itself.
        /// </summary>
        internal sealed class BuildPreprocessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report)
            {
                Get().Preprocess();
            }
        }

        [InitializeOnEnterPlayMode]
        private static void OnEnterPlayMode()
        {
            Get().Preprocess();
        }

        private void Preprocess()
        {
            m_Registrants = new();

            // Registrants a manifest lists load with that manifest, so they are left out of the main build.
            var listed = StatusRegistryManifest.FindListedRegistrants();

            // Every registrant type is found, including ones made outside this package.
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(Registrant)}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<Registrant>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && !listed.Contains(asset))
                    m_Registrants.Add(asset);
            }

            Debug.Log($"{nameof(StatusRegistry)}: Preprocessed {m_Registrants.Count} registrants.");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
#endif
        /// <summary>
        /// The name of the registry asset, which is also what it is loaded by from Resources.
        /// </summary>
        public const string RegistryName = "StatusRegistry";
        /// <summary>
        /// Where the registry asset is created in the editor. It must be in a Resources folder.
        /// </summary>
        public const string RegistryPath = "Assets/Settings/Resources/" + RegistryName + ".asset";
        /// <summary>
        /// Id that is never assigned to a <see cref="Registrant"/>, used to mean no reference.
        /// </summary>
        public const ushort NullId = 0;

        /// <summary>
        /// Raised at the end of <see cref="Rebuild"/>, once the ids have been reassigned.
        /// </summary>
        public event Action Rebuilt;
        /// <summary>
        /// Raised once the manifests found at startup are registered. See <see cref="IsReady"/>.
        /// </summary>
        public event Action Ready;

        /// <summary>
        /// The id of every registered key by its hash. See <see cref="Registrant.GetKeyHash"/>.
        /// </summary>
        public IReadOnlyDictionary<Hash128, ushort> KeyToId => m_KeyToId;
        /// <summary>
        /// Every registered manifest, in the order they were registered.
        /// </summary>
        public IReadOnlyList<StatusRegistryManifest> Manifests => m_Manifests;
        /// <summary>
        /// Hash of every networked <see cref="Registrant.Key"/> in id order. Two registries
        /// with the same hash assign the same ids. Updated on every <see cref="Rebuild"/>.
        /// </summary>
        public Hash128 RegistryHash { get; private set; }
        /// <summary>
        /// <see cref="RegistryHash"/> as text, so assemblies that don't reference Entities can compare registries.
        /// </summary>
        public string RegistryHashString => RegistryHash.ToString();
        /// <summary>
        /// True once the manifests found at startup are registered. Without Addressables this is true after the first rebuild.
        /// </summary>
        public bool IsReady { get; private set; }

        [SerializeField, HideInInspector]
        private List<Registrant> m_Registrants;

        // Every registered registrant, indexed by id. Index 0 is NullId's and stays null, so a default id means no reference.
        [NonSerialized] private List<Registrant> m_Entries;
        [NonSerialized] private Dictionary<Hash128, ushort> m_KeyToId;

        [NonSerialized] private List<StatusRegistryManifest> m_Manifests = new();
#if ADDRESSABLES
        // The handles of the manifests loaded with Addressables, which keep them and their registrants loaded while they are registered.
        [NonSerialized] private Dictionary<StatusRegistryManifest, AsyncOperationHandle<StatusRegistryManifest>> m_ManifestHandles = new();
#endif
        // The hash of this asset's own registrants, which counts as one manifest in a payload.
        [NonSerialized] private UnityEngine.Hash128 m_MainHash;
        // The manifests in the applied payload, or null when none is applied, where everything is networked.
        [NonSerialized] private HashSet<UnityEngine.Hash128> m_PayloadManifests;
        [NonSerialized] private HashSet<string> m_MissingKeys = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The registry, which is loaded from Resources. In the editor it is created when missing.
        /// </summary>
        public static StatusRegistry Instance { get => s_Instance != null ? s_Instance : Get(); }
        private static StatusRegistry s_Instance;
        private static StatusRegistry Get()
        {
            s_Instance = Resources.Load<StatusRegistry>(RegistryName);

#if UNITY_EDITOR
            if (s_Instance == null)
            {
                s_Instance = CreateInstance<StatusRegistry>();
                StatusSettings.EnsureFolder(System.IO.Path.GetDirectoryName(RegistryPath));
                AssetDatabase.CreateAsset(s_Instance, RegistryPath);
                AssetDatabase.SaveAssets();
            }

#endif
            return s_Instance;
        }

        // With domain reload turned off, this asset's runtime state survives between plays.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            if (s_Instance == null)
                return;

            s_Instance.IsReady = false;
            s_Instance.m_Manifests = new();
#if ADDRESSABLES
            s_Instance.m_ManifestHandles = new();
#endif
            s_Instance.m_PayloadManifests = null;
            s_Instance.m_MissingKeys = new(StringComparer.OrdinalIgnoreCase);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            if (Instance == null)
            {
                Debug.LogError($"{nameof(StatusRegistry)} could not be found at path {RegistryPath}. Please ensure that the registry exists and is located in the Resources folder.");
                return;
            }
            Instance.Rebuild();
#if ADDRESSABLES
            _ = Instance.LoadManifestsAsync();
#else
            Instance.SetReady();
#endif
        }

        #region Lookups
        /// <summary>
        /// Gets the id assigned to a <see cref="Registrant"/>. Returns false if it isn't registered.
        /// </summary>
        public bool TryGetId(Registrant registrant, out ushort id)
        {
            id = NullId;
            return registrant && TryGetId(registrant.GetKeyHash(), out id);
        }

        /// <summary>
        /// Gets the id assigned to a <see cref="Registrant.Key"/>. Returns false if it isn't registered.
        /// </summary>
        public bool TryGetId(string key, out ushort id)
        {
            id = NullId;
            return !string.IsNullOrEmpty(key) && TryGetId(Registrant.ComputeKeyHash(key), out id);
        }

        /// <summary>
        /// Gets the id assigned to the key with <paramref name="keyHash"/>. Returns false if it isn't registered.
        /// </summary>
        public bool TryGetId(Hash128 keyHash, out ushort id)
        {
            id = NullId;
            return m_KeyToId != null && m_KeyToId.TryGetValue(keyHash, out id);
        }

        /// <summary>
        /// Whether <paramref name="id"/> is registered.
        /// </summary>
        public bool IsRegistered(ushort id) => m_Entries != null && id != NullId && id < m_Entries.Count;

        /// <summary>
        /// Gets the registered <typeparamref name="T"/> with a <see cref="Registrant.Key"/>. Returns false if nothing
        /// is registered with the key, or the registrant with it isn't a <typeparamref name="T"/>.
        /// </summary>
        public bool TryGet<T>(string key, out T registrant) where T : Registrant
        {
            registrant = null;
            if (string.IsNullOrEmpty(key) || m_KeyToId == null)
                return false;

            if (TryGetId(key, out var id))
                return TryGet(id, out registrant);

            // The same missing reference is usually looked up repeatedly, so it is only reported once.
            if (m_MissingKeys.Add(key))
                Debug.LogWarning($"Nothing is registered in the {nameof(StatusRegistry)} with the key \"{key}\". If it comes from separately built content, like a mod, make sure its manifest is registered.");
            return false;
        }

        /// <summary>
        /// Gets the registered <typeparamref name="T"/> whose key has <paramref name="keyHash"/>. Returns false if nothing
        /// is registered with the key, or the registrant with it isn't a <typeparamref name="T"/>.
        /// </summary>
        public bool TryGet<T>(Hash128 keyHash, out T registrant) where T : Registrant
        {
            registrant = null;
            return TryGetId(keyHash, out var id) && TryGet(id, out registrant);
        }

        /// <summary>
        /// Gets the registered <typeparamref name="T"/> with <paramref name="id"/>. Returns false if nothing is registered
        /// with it, or the registrant with it isn't a <typeparamref name="T"/>.
        /// </summary>
        public bool TryGet<T>(ushort id, out T registrant) where T : Registrant
        {
            registrant = IsRegistered(id) ? m_Entries[id] as T : null;
            return registrant != null;
        }

        /// <summary>
        /// Every registered registrant that is a <typeparamref name="T"/>, with its id, in id order.
        /// </summary>
        public IEnumerable<(ushort Id, T Registrant)> GetAll<T>() where T : Registrant
        {
            // A rebuild swaps the list, so one started while enumerating doesn't change what this lists.
            var entries = m_Entries;
            if (entries == null)
                yield break;

            for (int i = NullId + 1; i < entries.Count; i++)
                if (entries[i] is T registrant && registrant)
                    yield return ((ushort)i, registrant);
        }

        /// <summary>
        /// The registered instance with the same key as <paramref name="registrant"/>, or <paramref name="registrant"/> itself
        /// when that key isn't registered. Addressables can load a second copy of an asset that the main build also references,
        /// so use it where the instance matters, such as subscribing to a <see cref="StatusEvent"/>.
        /// </summary>
        public T GetRegistered<T>(T registrant) where T : Registrant
        {
            return registrant && TryGet<T>(registrant.GetKeyHash(), out var registered) ? registered : registrant;
        }
        #endregion

        #region Manifests
        /// <summary>
        /// Registers the registrants a manifest references and rebuilds the ids. Returns false if a manifest with the same keys
        /// is already registered. Registrants made in code, for example by a mod, are registered with one made by
        /// <see cref="StatusRegistryManifest.Create"/>. Call it before gameplay, since the rebuild can change ids, and keep the
        /// manifest, since every rebuild reads it.
        /// </summary>
        public bool RegisterManifest(StatusRegistryManifest manifest) => RegisterManifests(new[] { manifest }) > 0;

        /// <summary>
        /// <see cref="RegisterManifest"/> for several manifests with one rebuild, for example every one in a loaded AssetBundle
        /// (<c>bundle.LoadAllAssets&lt;StatusRegistryManifest&gt;()</c>) or content directory
        /// (<c>ContentLoadManager.GetRootAssets&lt;StatusRegistryManifest&gt;(handle)</c>). Returns how many were registered.
        /// </summary>
        public int RegisterManifests(IEnumerable<StatusRegistryManifest> manifests)
        {
            int registered = 0;
            if (manifests != null)
                foreach (var manifest in manifests)
                    if (manifest && IndexOfManifest(manifest) < 0)
                    {
                        m_Manifests.Add(manifest);
                        registered++;
                    }

            if (registered > 0)
                Rebuild();
            return registered;
        }

        /// <summary>
        /// Unregisters the manifest with the same keys as <paramref name="manifest"/> and rebuilds the ids, for example when a
        /// mod is turned off. One that <see cref="LoadManifestsAsync"/> loaded is released, so it and its registrants can unload;
        /// unload anything else, such as an AssetBundle, after this. Returns false if no such manifest is registered. Call it
        /// before gameplay, since the rebuild can change ids.
        /// </summary>
        public bool UnregisterManifest(StatusRegistryManifest manifest) => UnregisterManifests(new[] { manifest }) > 0;

        /// <summary>
        /// <see cref="UnregisterManifest"/> for several manifests with one rebuild. Returns how many were unregistered.
        /// </summary>
        public int UnregisterManifests(IEnumerable<StatusRegistryManifest> manifests)
        {
            var unregistered = new List<StatusRegistryManifest>();
            if (manifests != null)
                foreach (var manifest in manifests)
                {
                    int index = manifest ? IndexOfManifest(manifest) : -1;
                    if (index < 0)
                        continue;

                    unregistered.Add(m_Manifests[index]);
                    m_Manifests.RemoveAt(index);
                }

            if (unregistered.Count == 0)
                return 0;

            // Rebuilt before the release, so nothing here references the registrants when they unload.
            Rebuild();
#if ADDRESSABLES
            foreach (var manifest in unregistered)
                if (m_ManifestHandles.Remove(manifest, out var handle))
                    Addressables.Release(handle);
#endif
            return unregistered.Count;
        }

        // The index of the registered manifest with the same keys, or -1.
        private int IndexOfManifest(StatusRegistryManifest manifest)
        {
            var hash = manifest.GetKeysHash();
            for (int i = 0; i < m_Manifests.Count; i++)
                if (m_Manifests[i].GetKeysHash() == hash)
                    return i;
            return -1;
        }
#if ADDRESSABLES

        /// <summary>
        /// Loads and registers every <see cref="StatusRegistryManifest"/> labeled <see cref="StatusRegistryManifest.Label"/> in
        /// the loaded catalogs that isn't registered yet, which loads their registrants too, then rebuilds once. It runs at
        /// startup; call it again after loading another catalog, such as a mod's, and before gameplay, since the rebuild can
        /// change ids.
        /// </summary>
        /// <remarks>
        /// A registered manifest can't be replaced while it is loaded, so a content update that changes one applies the next
        /// time the game starts. Addressables checks for catalog updates at startup by default, before this first runs.
        /// Updates found later can add groups, which this picks up.
        /// </remarks>
        public async Awaitable LoadManifestsAsync()
        {
            await Addressables.InitializeAsync().Task;

            var locationsHandle = Addressables.LoadResourceLocationsAsync(StatusRegistryManifest.Label, typeof(StatusRegistryManifest));
            var locations = await locationsHandle.Task;

            // Every manifest starts loading before any is waited on, so their content loads in parallel.
            var handles = new List<AsyncOperationHandle<StatusRegistryManifest>>();
            if (locations != null)
                foreach (var location in locations)
                    handles.Add(Addressables.LoadAssetAsync<StatusRegistryManifest>(location));

            bool added = false;
            foreach (var handle in handles)
            {
                // Manifests are told apart by their keys, not their location, since a mod's can have the same path as one of
                // the game's. Registered ones are already loaded, so loading them again is instant, and one that failed to
                // load is tried again.
                var manifest = await handle.Task;
                if (manifest && IndexOfManifest(manifest) < 0)
                {
                    m_Manifests.Add(manifest);
                    m_ManifestHandles[manifest] = handle;
                    added = true;
                }
                else
                {
                    Addressables.Release(handle);
                }
            }

            Addressables.Release(locationsHandle);

            if (added)
                Rebuild();
            SetReady();
        }
#endif

        private void SetReady()
        {
            if (IsReady)
                return;

            IsReady = true;
            Ready?.Invoke();
        }
        #endregion

        #region Payload
        /// <summary>
        /// What a server sends each client before gameplay: the hash of every set of registrants it has registered.
        /// A client applies it with <see cref="TryApplyPayload"/>.
        /// </summary>
        public StatusRegistryPayload CreatePayload()
        {
            var hashes = new List<UnityEngine.Hash128> { m_MainHash };
            foreach (var manifest in m_Manifests)
                if (IsNetworked(manifest))
                    hashes.Add(manifest.GetKeysHash());
            return new StatusRegistryPayload(hashes.ToArray());
        }

        /// <summary>
        /// On a client, checks it has every set of registrants in the server's payload, then rebuilds so they get the same ids as
        /// on the server. Anything only this client has gets ids after them, which never go over the network. Returns false,
        /// without changing anything, when <paramref name="missing"/> sets in the payload aren't here.
        /// </summary>
        public bool TryApplyPayload(in StatusRegistryPayload payload, out int missing)
        {
            missing = 0;
            var serverManifests = new HashSet<UnityEngine.Hash128>(payload.ManifestHashes ?? Array.Empty<UnityEngine.Hash128>());

            var localManifests = new HashSet<UnityEngine.Hash128> { m_MainHash };
            foreach (var manifest in m_Manifests)
                localManifests.Add(manifest.GetKeysHash());

            foreach (var hash in serverManifests)
                if (!localManifests.Contains(hash))
                    missing++;

            if (missing > 0)
                return false;

            m_PayloadManifests = serverManifests;
            Rebuild();
            return true;
        }

        /// <summary>
        /// Removes the payload applied with <see cref="TryApplyPayload"/>, for example after leaving a server, so everything
        /// registered here is networked again.
        /// </summary>
        public void ClearPayload()
        {
            if (m_PayloadManifests == null)
                return;

            m_PayloadManifests = null;
            Rebuild();
        }

        private bool IsNetworked(StatusRegistryManifest manifest) => m_PayloadManifests == null || m_PayloadManifests.Contains(manifest.GetKeysHash());
        #endregion

        /// <summary>
        /// Rebuilds the ids from this asset's registrants and every registered manifest.
        /// </summary>
        /// <remarks>
        /// Every key is collected into one list and sorted by key, so ids don't depend on the order assets were found or
        /// manifests were registered. With a payload applied, the registrants in it come first and anything only this client
        /// has comes after them. A key registered twice is an error, and only its first registrant is kept.
        /// </remarks>
        public void Rebuild()
        {
            // A manifest made in code and then destroyed counts as unregistered.
            m_Manifests.RemoveAll(manifest => !manifest);

            var candidates = new List<Candidate>();

            if (m_Registrants != null)
                foreach (var registrant in m_Registrants)
                    if (registrant != null)
                        candidates.Add(new Candidate
                        {
                            Key = registrant.Key ?? string.Empty,
                            SortKey = Registrant.NormalizeKey(registrant.Key),
                            Registrant = registrant,
                            Main = true,
                            Networked = true,
                            Source = registrant.name,
                            Order = candidates.Count,
                        });

            var mainKeys = new List<string>(candidates.Count);
            foreach (var candidate in candidates)
                mainKeys.Add(candidate.Key);
            m_MainHash = StatusRegistryManifest.ComputeKeysHash(mainKeys);

            foreach (var manifest in m_Manifests)
            {
                bool networked = IsNetworked(manifest);
                foreach (var registrant in manifest.Registrants)
                    if (registrant != null)
                        candidates.Add(new Candidate
                        {
                            Key = registrant.Key ?? string.Empty,
                            SortKey = Registrant.NormalizeKey(registrant.Key),
                            Registrant = registrant,
                            Networked = networked,
                            Source = manifest.name,
                            Order = candidates.Count,
                        });
            }

            // Networked keys come first so they get the same ids as on the server. Duplicates are tie broken by the main
            // build first, then collection order. A duplicate never takes an id, so the ids are the same either way.
            candidates.Sort((x, y) =>
            {
                int comparison = y.Networked.CompareTo(x.Networked);
                if (comparison != 0)
                    return comparison;
                comparison = string.CompareOrdinal(x.SortKey, y.SortKey);
                if (comparison != 0)
                    return comparison;
                comparison = y.Main.CompareTo(x.Main);
                if (comparison != 0)
                    return comparison;
                return x.Order.CompareTo(y.Order);
            });

            m_Entries = new() { null };
            m_KeyToId = new();
            m_MissingKeys.Clear();

            var registryHash = new UnityEngine.Hash128();

            foreach (var candidate in candidates)
            {
                if (m_Entries.Count > ushort.MaxValue)
                {
                    Debug.LogError($"The {nameof(StatusRegistry)} ran out of ids. Skipping registration for \"{candidate.Key}\" from {candidate.Source}.");
                    continue;
                }

                var id = (ushort)m_Entries.Count;
                if (!m_KeyToId.TryAdd(candidate.Registrant.GetKeyHash(), id))
                {
                    // Keys ignore case, so the registered one can be spelled differently. Both spellings are named so the clash is clear.
                    var registered = m_Entries[m_KeyToId[candidate.Registrant.GetKeyHash()]];
                    var registeredKey = registered ? registered.Key : null;
                    if (registeredKey != null && !string.Equals(registeredKey, candidate.Key, StringComparison.Ordinal))
                        Debug.LogError($"The key \"{candidate.Key}\" from {candidate.Source} is the same as the registered key \"{registeredKey}\" from {registered.name}, since keys ignore case. Skipping it.", candidate.Registrant);
                    else
                        Debug.LogError($"The key \"{candidate.Key}\" from {candidate.Source} is already registered. Skipping it.", candidate.Registrant);
                    continue;
                }

                m_Entries.Add(candidate.Registrant);

                // Ids are assigned in order, so hashing the networked keys in order covers which key every networked id maps to.
                if (candidate.Networked)
                    registryHash.Append(candidate.SortKey);
            }

            RegistryHash = registryHash;
            Rebuilt?.Invoke();
        }

        private struct Candidate
        {
            public string Key;
            /// <summary>
            /// The key as it is sorted and hashed, ignoring case. See <see cref="Registrant.NormalizeKey"/>.
            /// </summary>
            public string SortKey;
            public Registrant Registrant;
            /// <summary>
            /// Whether it is from this asset's own list rather than a manifest.
            /// </summary>
            public bool Main;
            public bool Networked;
            /// <summary>
            /// Where it came from, for error messages.
            /// </summary>
            public string Source;
            /// <summary>
            /// The order the candidate was collected in, only used as a last tie break.
            /// </summary>
            public int Order;
        }
    }
}
