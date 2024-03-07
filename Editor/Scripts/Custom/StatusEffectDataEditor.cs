using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusEffectData))]
    [CanEditMultipleObjects]
    public class StatusEffectDataEditor : Editor
    {
        private SerializedProperty _group;
        private SerializedProperty _comparableName;
        private SerializedProperty _baseValue;
        private SerializedProperty _icon;
        private SerializedProperty _statusEffectName;
        private SerializedProperty _description;
        private SerializedProperty _allowEffectStacking;
        private SerializedProperty _nonStackingBehaviour;
        private SerializedProperty _maxStack;
        private SerializedProperty _effects;
        private SerializedProperty _conditions;
        private SerializedProperty _customEffect;

        private SerializedProperty _enableIcon;
        private SerializedProperty _enableName;
        private SerializedProperty _enableDescription;
        
        private Condition _condition;

        private const string _baseValueWarningMessage = "Base value cannot be 0!";

        private bool _displayConditionsWarning;
        private const string _conditionsWarningMessage = "Do not recursively add status datas! " +
                                                         "Avoid adding a status data to itself! " +
                                                         "Make sure there aren't two that add each other!";
        
        public override void OnInspectorGUI()
        {
            // Retrieve properties
            _group = serializedObject.FindProperty("group");
            _comparableName = serializedObject.FindProperty("comparableName");
            _baseValue = serializedObject.FindProperty("baseValue");

            _statusEffectName = serializedObject.FindProperty("statusEffectName");
            _description = serializedObject.FindProperty("description");
            _icon = serializedObject.FindProperty("icon");

            _allowEffectStacking = serializedObject.FindProperty("allowEffectStacking");
            _nonStackingBehaviour = serializedObject.FindProperty("nonStackingBehaviour");
            _maxStack = serializedObject.FindProperty("maxStack");

            _effects = serializedObject.FindProperty("effects");
            _conditions = serializedObject.FindProperty("conditions");
            _customEffect = serializedObject.FindProperty("customEffect");

            _enableIcon = serializedObject.FindProperty("enableIcon");
            _enableName = serializedObject.FindProperty("enableName");
            _enableDescription = serializedObject.FindProperty("enableDescription");

            serializedObject.Update();
            
            EditorGUILayout.BeginVertical("groupbox");
            EditorGUILayout.LabelField("Required Fields", EditorStyles.boldLabel);
            HorizontalLine();
            EditorGUILayout.PropertyField(_group);
            EditorGUILayout.PropertyField(_comparableName);
            if (_baseValue.floatValue == 0)
                EditorGUILayout.HelpBox(_baseValueWarningMessage, MessageType.Warning);
            EditorGUILayout.PropertyField(_baseValue);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical("groupbox");
            EditorGUILayout.LabelField("Optional Fields", EditorStyles.boldLabel);
            HorizontalLine();
            EditorGUILayout.BeginHorizontal();
            _enableIcon.boolValue = EditorGUILayout.Toggle(_enableIcon.boolValue, GUILayout.MaxWidth(18));
            if (_enableIcon.boolValue)
                EditorGUILayout.PropertyField(_icon);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            _enableName.boolValue = EditorGUILayout.Toggle(_enableName.boolValue, GUILayout.MaxWidth(18));
            if (_enableName.boolValue)
                EditorGUILayout.PropertyField(_statusEffectName, new GUIContent("Name"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            _enableDescription.boolValue = EditorGUILayout.Toggle(_enableDescription.boolValue, GUILayout.MaxWidth(18));
            if (_enableDescription.boolValue)
                EditorGUILayout.PropertyField(_description);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("groupbox");
            EditorGUILayout.LabelField("Stacking", EditorStyles.boldLabel);
            HorizontalLine();
            EditorGUILayout.PropertyField(_allowEffectStacking);
            if (!_allowEffectStacking.boolValue)
                EditorGUILayout.PropertyField(_nonStackingBehaviour);
            if (_allowEffectStacking.boolValue)
                EditorGUILayout.PropertyField(_maxStack);
            EditorGUILayout.EndVertical();

            EditorGUILayout.PropertyField(_effects);
            EditorGUILayout.Space(5);

            _displayConditionsWarning = false;

            for (int i = 0; i < _conditions.arraySize; i++)
            {
                _condition = (target as StatusEffectData).conditions.ElementAt(i);

                if (!_condition.add || _condition.data != target)
                    continue;

                _displayConditionsWarning = true;
                break;
            }

            if (_displayConditionsWarning)
                EditorGUILayout.HelpBox(_conditionsWarningMessage, MessageType.Warning);

            EditorGUILayout.PropertyField(_conditions);
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(_customEffect);

            serializedObject.ApplyModifiedProperties();
        }

        private void HorizontalLine()
        {
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f), new Color(.5f, .5f, .5f, 0.3f));

            GUILayout.Space(4f);
        }
    }
}
