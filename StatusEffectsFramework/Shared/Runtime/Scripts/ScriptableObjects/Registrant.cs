using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
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
#if UNITY_EDITOR && ADDRESSABLES
        internal StatusRegistryDependency OptionalRegistryDependency => m_OptionalRegistryDependency;
#endif
        public string UniqueKey => m_UniqueKey;

#if UNITY_EDITOR && ADDRESSABLES
        [Tooltip("When the game is built or played in the editor, all instances will be automatically added to the main registry. Only use a dependency if the instance should be excluded from the main build's registry.")]
        [SerializeField] private StatusRegistryDependency m_OptionalRegistryDependency = null;
#endif
        [Tooltip("Should be a unique key identifier.")]
        [SerializeField] private string m_UniqueKey;

#if UNITY_EDITOR
        private void Reset()
        {
            CheckUniqueKey();
        }

        private void OnEnable()
        {
            CheckUniqueKey();
        }

        private void CheckUniqueKey()
        {
            if (string.IsNullOrEmpty(m_UniqueKey))
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return; // asset could've been deleted before this runs
                    if (string.IsNullOrEmpty(m_UniqueKey) && !string.IsNullOrEmpty(name))
                    {
                        m_UniqueKey = $"{StatusSettings.GetOrCreateSettings().DefaultUniqueKeyNamespace}:{ToKebabCase(name)}";
                        EditorUtility.SetDirty(this);
                    }
                };
            }
        }
#endif
        public Hash128 GetUniqueKeyHash()
        {
            return UnityEngine.Hash128.Compute(m_UniqueKey);
        }

        private static string ToKebabCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Insert a dash between a lowercase/digit and a following uppercase letter
            // e.g. abcDef -> abc-Def
            string result = Regex.Replace(input, "(?<=[a-z0-9])(?=[A-Z])", "-");

            // Insert a dash before the last uppercase letter of a run when it's
            // followed by a lowercase letter (splits acronyms from the word after them)
            // e.g. HTTPServer -> HTTP-Server
            result = Regex.Replace(result, "(?<=[A-Z])(?=[A-Z][a-z])", "-");

            // Collapse any whitespace runs into a single dash
            result = Regex.Replace(result, @"\s+", "-");

            // Collapse multiple consecutive dashes into one
            result = Regex.Replace(result, "-+", "-");

            // Trim leading/trailing dashes and lowercase everything
            return result.Trim('-').ToLowerInvariant();
        }
    }
}
