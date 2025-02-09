using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectDatabase))]
    [CanEditMultipleObjects]
    public class StatusEffectDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            base.OnInspectorGUI();
            EditorGUI.EndDisabledGroup();
        }
    }
}
