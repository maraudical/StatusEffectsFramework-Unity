using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(ExampleEntity))]
    public class ExampleEntityEditor : Editor
    {
        ExampleEntity exampleEntity;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            exampleEntity = (ExampleEntity)target;

            GUIStyle style = new GUIStyle(EditorStyles.largeLabel);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 16;
            style.fontStyle = FontStyle.Bold;

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Debug Buttons", style);
            EditorGUILayout.BeginVertical("groupbox");
            if (GUILayout.Button("Add Effect"))
                exampleEntity.DebugAddStatusEffect();
            if (GUILayout.Button("Add Effect Timed"))
                exampleEntity.DebugAddStatusEffectTimed();
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Add Effect Timed Event"))
                exampleEntity.DebugAddStatusEffectTimedEvent();
            if (GUILayout.Button("Invoke Event"))
                exampleEntity.InvokeEvent();
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Add Effect Predicate"))
                exampleEntity.DebugAddStatusEffectPredicate();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("groupbox");
            if (GUILayout.Button("Remove Effect"))
                exampleEntity.DebugRemoveStatusEffect();
            if (GUILayout.Button("Remove Effect Group"))
                exampleEntity.DebugRemoveStatusEffectGroup();
            EditorGUILayout.EndVertical();
        }
    }
}
