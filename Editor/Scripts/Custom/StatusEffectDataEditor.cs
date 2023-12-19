using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectData))]
    [CanEditMultipleObjects]
    public class StatusEffectDataEditor : Editor
    {
        SerializedProperty group;
        SerializedProperty comparableName;
        SerializedProperty baseValue;
        SerializedProperty icon;
        new SerializedProperty name;
        SerializedProperty description;
        SerializedProperty allowEffectStacking;
        SerializedProperty nonStackingBehaviour;
        SerializedProperty effects;
        SerializedProperty conditions;
        SerializedProperty customEffect;

        SerializedProperty enableIcon;
        SerializedProperty enableName;
        SerializedProperty enableDescription;
        
        private Condition condition;

        private const string baseValueWarningMessage = "Base value cannot be 0!";

        private bool displayConditionsWarning;
        private const string conditionsWarningMessage = "Do not recursively add status datas! " +
                                                        "Avoid adding a status data to itself! " +
                                                        "Make sure there aren't two that add each other!";
        
        public override void OnInspectorGUI()
        {
            // Retrieve properties
            group = serializedObject.FindProperty("group");
            comparableName = serializedObject.FindProperty("comparableName");
            baseValue = serializedObject.FindProperty("baseValue");

            name = serializedObject.FindProperty("statusEffectName");
            description = serializedObject.FindProperty("description");
            icon = serializedObject.FindProperty("icon");

            allowEffectStacking = serializedObject.FindProperty("allowEffectStacking");
            nonStackingBehaviour = serializedObject.FindProperty("nonStackingBehaviour");

            effects = serializedObject.FindProperty("effects");
            conditions = serializedObject.FindProperty("conditions");
            customEffect = serializedObject.FindProperty("customEffect");

            enableIcon = serializedObject.FindProperty("enableIcon");
            enableName = serializedObject.FindProperty("enableName");
            enableDescription = serializedObject.FindProperty("enableDescription");

            serializedObject.Update();
            
            EditorGUILayout.BeginVertical("groupbox");
            EditorGUILayout.LabelField("Required Fields", EditorStyles.boldLabel);
            HorizontalLine();
            EditorGUILayout.PropertyField(group);
            EditorGUILayout.PropertyField(comparableName);
            if (baseValue.floatValue == 0)
                EditorGUILayout.HelpBox(baseValueWarningMessage, MessageType.Warning);
            EditorGUILayout.PropertyField(baseValue);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical("groupbox");
            EditorGUILayout.LabelField("Optional Fields", EditorStyles.boldLabel);
            HorizontalLine();
            EditorGUILayout.BeginHorizontal();
            enableIcon.boolValue = EditorGUILayout.Toggle(enableIcon.boolValue, GUILayout.MaxWidth(18));
            if (enableIcon.boolValue)
                EditorGUILayout.PropertyField(icon);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            enableName.boolValue = EditorGUILayout.Toggle(enableName.boolValue, GUILayout.MaxWidth(18));
            if (enableName.boolValue)
                EditorGUILayout.PropertyField(name, new GUIContent("Name"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            enableDescription.boolValue = EditorGUILayout.Toggle(enableDescription.boolValue, GUILayout.MaxWidth(18));
            if (enableDescription.boolValue)
                EditorGUILayout.PropertyField(description);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("groupbox");
            EditorGUILayout.LabelField("Stacking", EditorStyles.boldLabel);
            HorizontalLine();
            EditorGUILayout.PropertyField(allowEffectStacking);
            if (!allowEffectStacking.boolValue)
                EditorGUILayout.PropertyField(nonStackingBehaviour);
            EditorGUILayout.EndVertical();

            EditorGUILayout.PropertyField(effects);
            EditorGUILayout.Space(5);

            displayConditionsWarning = false;

            for (int i = 0; i < conditions.arraySize; i++)
            {
                condition = (target as StatusEffectData).conditions.ElementAt(i);

                if (!condition.add || condition.configurable != target)
                    continue;

                displayConditionsWarning = true;
                break;
            }

            if (displayConditionsWarning)
                EditorGUILayout.HelpBox(conditionsWarningMessage, MessageType.Warning);

            EditorGUILayout.PropertyField(conditions);
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(customEffect);

            serializedObject.ApplyModifiedProperties();
        }

        private void HorizontalLine()
        {
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f), new Color(.5f, .5f, .5f, 0.3f));

            GUILayout.Space(4f);
        }
    }
}
