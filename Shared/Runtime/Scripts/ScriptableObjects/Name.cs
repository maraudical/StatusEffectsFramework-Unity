using System;
using UnityEngine;

namespace StatusEffectFramework
{
    public abstract class Name : ScriptableObject
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
    }
}