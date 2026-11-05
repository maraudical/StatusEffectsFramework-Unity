using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StatusEffectsFramework
{
    /// <summary>
    /// References a set of registrants that are loaded separately from the main build, such as the ones in one Addressables
    /// group, AssetBundle, content directory or mod. Loading the manifest loads them, and registering it with
    /// <see cref="StatusRegistry.RegisterManifest"/> gives each of them an id. The registrants a manifest lists are left out of
    /// the main build.
    /// </summary>
    /// <remarks>
    /// Make one with Assets > Create > Status Effects Framework > Registry Manifest and ship it with its content. In the editor
    /// it fills itself from an Addressables group, an AssetBundle or a folder, or lists registrants picked by hand. Content
    /// made in code can build one with <see cref="Create"/>. Anything heavy a registrant needs, such as a prefab in a module,
    /// can be kept out of what loads with it by referencing it through Addressables or a content directory's Loadable instead.
    /// </remarks>
    [CreateAssetMenu(fileName = "New Registry Manifest", menuName = "Status Effects Framework/Registry Manifest", order = 20)]
    public class StatusRegistryManifest : ScriptableObject
    {
        /// <summary>
        /// The Addressables label manifests filled from a group are given, which is how <see cref="StatusRegistry.LoadManifestsAsync"/> finds them.
        /// </summary>
        public const string Label = "StatusRegistryManifest";

        /// <summary>
        /// The registrants this manifest registers, sorted by key.
        /// </summary>
        public IReadOnlyList<Registrant> Registrants => m_Registrants;

        [SerializeField] private List<Registrant> m_Registrants = new();
#if UNITY_EDITOR

        /// <summary>
        /// Where the editor finds the registrants a manifest lists.
        /// </summary>
        internal enum ContentSource
        {
            /// <summary>
            /// Every registrant in a folder and its subfolders.
            /// </summary>
            Folder,
            /// <summary>
            /// Every registrant in an AssetBundle, as set at the bottom of an asset's or folder's Inspector.
            /// </summary>
            AssetBundle,
            /// <summary>
            /// Every registrant in an Addressables group.
            /// </summary>
            AddressableGroup,
            /// <summary>
            /// The registrants picked by hand.
            /// </summary>
            Explicit,
        }

        internal ContentSource Source => m_Source;
        internal bool RegenerateAutomatically => m_RegenerateAutomatically;
        internal string AddressableGroupGuid => m_AddressableGroupGuid;
        internal string AssetBundleName => m_AssetBundleName;
        /// <summary>
        /// The folder whose registrants are listed, or empty for the folder this manifest is in.
        /// </summary>
        internal string Folder => m_Folder;

        [SerializeField] private ContentSource m_Source;
        [SerializeField] private bool m_RegenerateAutomatically = true;
        [SerializeField] private string m_AddressableGroupGuid;
        [SerializeField] private string m_AssetBundleName;
        [SerializeField] private string m_Folder;

        /// <summary>
        /// Every registrant a manifest asset in the project lists.
        /// </summary>
        internal static HashSet<Registrant> FindListedRegistrants()
        {
            var listed = new HashSet<Registrant>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(StatusRegistryManifest)}"))
            {
                var manifest = AssetDatabase.LoadAssetAtPath<StatusRegistryManifest>(AssetDatabase.GUIDToAssetPath(guid));
                if (manifest)
                    foreach (var registrant in manifest.m_Registrants)
                        if (registrant)
                            listed.Add(registrant);
            }

            return listed;
        }
#endif

        /// <summary>
        /// Makes a manifest at runtime, for example for registrants a mod creates in code.
        /// </summary>
        public static StatusRegistryManifest Create(string name, IEnumerable<Registrant> registrants)
        {
            var manifest = CreateInstance<StatusRegistryManifest>();
            manifest.name = name;
            manifest.SetRegistrants(registrants);
            return manifest;
        }

        /// <summary>
        /// A hash of every key this manifest registers. Two manifests with the same hash register the same keys, which is
        /// how a <see cref="StatusRegistryPayload"/> checks that a client has the content the server uses.
        /// </summary>
        public Hash128 GetKeysHash()
        {
            var keys = new List<string>(m_Registrants.Count);
            foreach (var registrant in m_Registrants)
                if (registrant)
                    keys.Add(registrant.Key);
            return ComputeKeysHash(keys);
        }

        /// <summary>
        /// Replaces the registrants, sorted by key. Returns false when they are the same as before.
        /// </summary>
        internal bool SetRegistrants(IEnumerable<Registrant> registrants)
        {
            var sorted = new List<Registrant>();
            foreach (var registrant in registrants)
                if (registrant)
                    sorted.Add(registrant);
            sorted.Sort((x, y) => Registrant.CompareKeys(x.Key, y.Key));

            if (sorted.Count == m_Registrants.Count)
            {
                bool same = true;
                for (int i = 0; i < sorted.Count && same; i++)
                    same = sorted[i] == m_Registrants[i];
                if (same)
                    return false;
            }

            m_Registrants = sorted;
            return true;
        }

        /// <summary>
        /// Hashes the keys in key order, ignoring case, so neither the order they were listed in nor their case matters.
        /// </summary>
        internal static Hash128 ComputeKeysHash(IEnumerable<string> keys)
        {
            var sorted = new List<string>();
            foreach (var key in keys)
                sorted.Add(Registrant.NormalizeKey(key));
            sorted.Sort(string.CompareOrdinal);

            var hash = new Hash128();
            foreach (var key in sorted)
                hash.Append(key);
            return hash;
        }
    }
}
