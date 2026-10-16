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
#if UNITY_EDITOR && ADDRESSABLES
        internal StatusRegistryDependency OptionalRegistryDependency => m_OptionalRegistryDependency;
#endif
        /// <summary>
        /// The unique key, for example <c>base:poison</c>. It identifies this registrant in the <see cref="StatusRegistry"/>
        /// and in a <see cref="RegistrantKey{T}"/>.
        /// </summary>
        public string Key => m_Key;

#if UNITY_EDITOR && ADDRESSABLES
        [SerializeField] private StatusRegistryDependency m_OptionalRegistryDependency = null;
#endif
        [Tooltip("Should be a unique key identifier.")]
        [SerializeField] private string m_Key;

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
                        m_Key = $"{StatusSettings.Instance.DefaultKeyNamespace}:{name.ToKebabCase()}";
                        EditorUtility.SetDirty(this);
                    }
                };
            }
        }
#endif
        /// <summary>
        /// Hashes the <see cref="Key"/>.
        /// </summary>
        public Hash128 GetKeyHash()
        {
            return UnityEngine.Hash128.Compute(m_Key);
        }
    }
}
