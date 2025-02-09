#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Example.Inspector
{
    public static class ExamplePlayerInspector
    {
        public static void DrawInspector(Action baseOnInspectorGUI, IExamplePlayer player)
        {
            EditorGUILayout.HelpBox("Please view the code in this script for example implementation!", MessageType.Info);
            EditorGUILayout.Space(1);

            baseOnInspectorGUI?.Invoke();

            GUIStyle style = new GUIStyle(EditorStyles.largeLabel);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 16;
            style.fontStyle = FontStyle.Bold;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(nameof(player.Health));
            EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
            player.Health = EditorGUILayout.FloatField(player.Health);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Debug Buttons", style);
            EditorGUILayout.BeginVertical("groupbox");
            if (GUILayout.Button("Add Effect"))
                player.DebugAddStatusEffect();
            if (GUILayout.Button("Add Effect Timed"))
                player.DebugAddStatusEffectTimed();
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Add Effect Timed Event"))
                player.DebugAddStatusEffectTimedEvent();
            if (GUILayout.Button("Invoke Event"))
                player.InvokeEvent();
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Add Effect Predicate"))
                player.DebugAddStatusEffectPredicate();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("groupbox");
            if (GUILayout.Button("Remove Effect"))
                player.DebugRemoveStatusEffect();
            if (GUILayout.Button("Remove Effect Group"))
                player.DebugRemoveStatusEffectGroup();
            EditorGUILayout.EndVertical();
        }
    }
}
#endif