using System;
using UnityEngine;

namespace StatusEffects
{
    public abstract class Name : ScriptableObject, IEquatable<Name>
    {
        public Hash128 Id => m_Id;
        [SerializeField] private Hash128 m_Id;

        private void OnValidate()
        {
            if (m_Id == default)
                GenerateId();
        }

        private void Reset()
        {
            GenerateId();
        }
        /// <summary>
        /// This will be called automatically from a post processor.
        /// </summary>
        public void GenerateId()
        {
#if UNITY_EDITOR
            m_Id = Hash128.Compute(Guid.NewGuid().ToString("N"));
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public static bool operator ==(Name name1, Name name2) => (name1?.Id ?? default) == (name2?.Id ?? default);
        public static bool operator !=(Name name1, Name name2) => (name1?.Id ?? default) != (name2?.Id ?? default);
        public bool Equals(Name other) => other == this;
        public override bool Equals(object obj) => Equals(obj as Name);
        public override int GetHashCode() => m_Id.GetHashCode();
    }
}