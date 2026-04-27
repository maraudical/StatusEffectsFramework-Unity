using System;
using UnityEngine;

namespace StatusEffectFramework
{
    public abstract class Name : ScriptableObject, IEquatable<Name>
    {
        public Hash128 Id => m_Id;
        [SerializeField] private Hash128 m_Id;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_Id == default)
            {
                GenerateAndImport();
            }
        }

        private void Reset()
        {
            GenerateAndImport();
        }

        [ContextMenu("Generate New ID")]
        private void GenerateAndImport()
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrWhiteSpace(path))
                return;
            GenerateId();
            UnityEditor.AssetDatabase.ImportAsset(path);
        }
        /// <summary>
        /// This will be called automatically from a post processor.
        /// </summary>
        public void GenerateId()
        {
            m_Id = StatusEffectsUtility.GenerateId();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }

#endif
        fix this shit
        public static bool operator ==(Name name1, Name name2) { Debug.Log($"{name1} is null? ({(name1 as UnityEngine.Object) == null}) {name2} is null? ({(name2 as UnityEngine.Object) == null})"); return (name1?.Id ?? default) == (name2?.Id ?? default); }
        public static bool operator !=(Name name1, Name name2) => (name1?.Id ?? default) != (name2?.Id ?? default);
        public bool Equals(Name other) => other == this;
        public override bool Equals(object obj) => Equals(obj as Name);
        public override int GetHashCode() => m_Id.GetHashCode();
    }
}