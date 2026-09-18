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
        [SerializeField] private string m_UniqueKey = "namespace:name";

        public Hash128 GetUniqueKeyHash()
        {
            return UnityEngine.Hash128.Compute(m_UniqueKey);
        }
    }
}
