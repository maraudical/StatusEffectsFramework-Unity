using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENTITIES
using Hash128 = Unity.Entities.Hash128;
#endif

namespace StatusEffectsFramework
{
    /// <summary>
    /// The unique key identifier use to register with the <see cref="StatusRegistry"/>.
    /// </summary>
    public class Registrant : ScriptableObject
    {
        /// <summary>
        /// The unique key, for example <c>Base/Poison</c>. It identifies this registrant in the <see cref="StatusRegistry"/>
        /// and in a <see cref="RegistrantKey{T}"/>. Case is ignored, so <c>base/poison</c> is the same key.
        /// </summary>
        public string Key => m_Key;

        /// <summary>
        /// Separates a key's namespace from its name, for example <c>Base/Poison</c>.
        /// </summary>
        public const char KeySeparator = '/';

        [Tooltip("Should be a unique key identifier.")]
        [SerializeField] private string m_Key;

        // The hash of the key, and the key it was computed from so a changed key is hashed again.
        [System.NonSerialized] private string m_CachedKey;
        [System.NonSerialized] private Hash128? m_KeyHash;
#if UNITY_EDITOR

        private void Reset()
        {
            CheckKey();
        }

        private void OnEnable()
        {
            CheckKey();
        }

        private void CheckKey()
        {
            if (string.IsNullOrEmpty(m_Key))
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return; // asset could've been deleted before this runs
                    if (string.IsNullOrEmpty(m_Key) && !string.IsNullOrEmpty(name))
                    {
                        m_Key = CreateKey(StatusSettings.Instance.DefaultKeyNamespace, name);
                        EditorUtility.SetDirty(this);
                    }
                };
            }
        }
#endif
        /// <summary>
        /// Hashes the <see cref="Key"/>. The hash is computed once and reused until the key changes.
        /// </summary>
        public Hash128 GetKeyHash()
        {
            // Strings can't change in place, so editing or deserializing the key always assigns a new string.
            // Comparing references is enough to tell the cached hash is stale, without comparing characters.
            if (!m_KeyHash.HasValue || !ReferenceEquals(m_CachedKey, m_Key))
            {
                m_KeyHash = ComputeKeyHash(m_Key);
                m_CachedKey = m_Key;
            }

            return m_KeyHash.Value;
        }

        /// <summary>
        /// Builds a key from a namespace and a name, with each part in PascalCase, for example <c>base</c> and
        /// <c>Max Health</c> give <c>Base/MaxHealth</c>. An empty namespace gives just the name.
        /// </summary>
        public static string CreateKey(string keyNamespace, string name)
        {
            var pascalName = name.ToPascalCase();
            var pascalNamespace = keyNamespace.ToPascalCase();
            return string.IsNullOrEmpty(pascalNamespace) ? pascalName : pascalNamespace + KeySeparator + pascalName;
        }

        /// <summary>
        /// The form of <paramref name="key"/> that is hashed, sorted and compared, which ignores case.
        /// </summary>
        public static string NormalizeKey(string key) => key?.ToLowerInvariant() ?? string.Empty;

        /// <summary>
        /// Hashes <paramref name="key"/> ignoring case. Every key hash in the framework comes from here.
        /// </summary>
        public static Hash128 ComputeKeyHash(string key) => UnityEngine.Hash128.Compute(NormalizeKey(key));

        /// <summary>
        /// Whether two keys are the same, ignoring case. Agrees with <see cref="NormalizeKey"/> without allocating.
        /// </summary>
        public static bool KeysEqual(string x, string y)
        {
            x ??= string.Empty;
            y ??= string.Empty;
            return x.Length == y.Length && CompareKeys(x, y) == 0;
        }

        /// <summary>
        /// Orders keys the same way on every machine, ignoring case. It gives the same order as comparing the keys from
        /// <see cref="NormalizeKey"/> ordinally, without allocating. Ids are assigned in this order.
        /// </summary>
        public static int CompareKeys(string x, string y)
        {
            x ??= string.Empty;
            y ??= string.Empty;

            int length = System.Math.Min(x.Length, y.Length);
            for (int i = 0; i < length; i++)
            {
                int comparison = char.ToLowerInvariant(x[i]).CompareTo(char.ToLowerInvariant(y[i]));
                if (comparison != 0)
                    return comparison;
            }

            return x.Length.CompareTo(y.Length);
        }
    }
}
