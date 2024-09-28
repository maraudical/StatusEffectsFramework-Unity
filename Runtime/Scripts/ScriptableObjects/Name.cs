using System;
using UnityEngine;

namespace StatusEffects
{
    public abstract class Name : ScriptableObject, IEquatable<Name>
    {
        public string Id => m_Id;
        [SerializeField] private string m_Id;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(m_Id))
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
            m_Id = Guid.NewGuid().ToString();
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