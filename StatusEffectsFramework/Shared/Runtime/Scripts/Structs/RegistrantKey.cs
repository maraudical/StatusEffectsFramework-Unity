using System;
using UnityEngine;
#if ENTITIES
using Hash128 = Unity.Entities.Hash128;
#endif

namespace StatusEffectsFramework
{
    /// <summary>
    /// References a <see cref="Registrant"/> by its <see cref="Registrant.Key"/> instead of by object.
    /// Builds only contain the key, so nothing referenced this way is pulled into a build or asset bundle with
    /// the asset referencing it. Separately built content, like mods, can reference registrants it doesn't
    /// contain, and they are found in the <see cref="StatusRegistry"/> at runtime.
    /// </summary>
    /// <remarks>
    /// In the editor an asset can be assigned instead of typing the key. The asset only exists in the editor
    /// and is always read from there, and its key is baked into the serialized key before a build.
    /// </remarks>
    [Serializable]
    public struct RegistrantKey<T> : IEquatable<RegistrantKey<T>> where T : Registrant
    {
        [Tooltip("The key of the asset to reference.")]
        [SerializeField] private string m_Key;
#if UNITY_EDITOR
        [SerializeField] private T m_Asset;
#endif

        public RegistrantKey(string key)
        {
            m_Key = key;
#if UNITY_EDITOR
            m_Asset = null;
#endif
        }

        public RegistrantKey(T registrant)
        {
            m_Key = registrant ? registrant.Key : null;
#if UNITY_EDITOR
            m_Asset = registrant;
#endif
        }

        /// <summary>
        /// The referenced <see cref="Registrant.Key"/>, or an empty string when nothing is referenced.
        /// In the editor it comes from the assigned asset when there is one.
        /// </summary>
        public string Key
        {
            get
            {
#if UNITY_EDITOR
                if (m_Asset)
                    return m_Asset.Key ?? string.Empty;
#endif
                return m_Key ?? string.Empty;
            }
        }

        /// <summary>
        /// True when no key is assigned.
        /// </summary>
        public bool IsEmpty => string.IsNullOrWhiteSpace(Key);
#if UNITY_EDITOR

        /// <summary>
        /// The assigned asset, or null when the key was typed. Only exists in the editor.
        /// </summary>
        internal T Asset => m_Asset;
#endif

        /// <summary>
        /// Hashes the key the same way as <see cref="Registrant.GetKeyHash"/>.
        /// </summary>
        public Hash128 GetKeyHash()
        {
            return UnityEngine.Hash128.Compute(Key);
        }

        /// <summary>
        /// Finds the registered <typeparamref name="T"/> with this key. Returns false when the key is empty, isn't
        /// registered (e.g. the content providing it isn't loaded), or belongs to a different type of registrant.
        /// </summary>
        public bool TryResolve(out T registrant)
        {
            var registry = StatusRegistry.Instance;
            if (registry)
                return registry.TryGet(Key, out registrant);

            registrant = null;
            return false;
        }

        /// <summary>
        /// The registered <typeparamref name="T"/> with this key, or null. See <see cref="TryResolve"/>.
        /// </summary>
        public T Resolve()
        {
            return TryResolve(out var registrant) ? registrant : null;
        }

        /// <summary>
        /// Keys are equal when their <see cref="Key"/>s are the same.
        /// </summary>
        public bool Equals(RegistrantKey<T> other) => string.Equals(Key, other.Key, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is RegistrantKey<T> other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Key);

        public override string ToString() => Key;

        /// <summary>
        /// Keys are equal when their <see cref="Key"/>s are the same.
        /// </summary>
        public static bool operator ==(RegistrantKey<T> x, RegistrantKey<T> y) => x.Equals(y);

        /// <summary>
        /// Keys are different when their <see cref="Key"/>s are not the same.
        /// </summary>
        public static bool operator !=(RegistrantKey<T> x, RegistrantKey<T> y) => !x.Equals(y);

        /// <summary>
        /// References <paramref name="registrant"/> by its key. A missing registrant gives an empty key.
        /// </summary>
        public static implicit operator RegistrantKey<T>(T registrant) => new(registrant);
    }
}
