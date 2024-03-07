using UnityEditor;
using UnityEngine;
using StatusEffects.Example;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(ExampleEntity))]
    [CanEditMultipleObjects]
    public class ExampleEntityEditor : Editor
    {
        private ExampleEntity _exampleEntity;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Please view the code in this script for example implementation!", MessageType.Info);

            base.OnInspectorGUI();

            _exampleEntity = (ExampleEntity)target;

            GUIStyle style = new GUIStyle(EditorStyles.largeLabel);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 16;
            style.fontStyle = FontStyle.Bold;

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Debug Buttons", style);
            EditorGUILayout.BeginVertical("groupbox");
            if (GUILayout.Button("Add Effect"))
                _exampleEntity.DebugAddStatusEffect();
            if (GUILayout.Button("Add Effect Timed"))
                _exampleEntity.DebugAddStatusEffectTimed();
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Add Effect Timed Event"))
                _exampleEntity.DebugAddStatusEffectTimedEvent();
            if (GUILayout.Button("Invoke Event"))
                _exampleEntity.InvokeEvent();
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Add Effect Predicate"))
                _exampleEntity.DebugAddStatusEffectPredicate();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("groupbox");
            if (GUILayout.Button("Remove Effect"))
                _exampleEntity.DebugRemoveStatusEffect();
            if (GUILayout.Button("Remove Effect Group"))
                _exampleEntity.DebugRemoveStatusEffectGroup();
            EditorGUILayout.EndVertical();
        }
    }
}
