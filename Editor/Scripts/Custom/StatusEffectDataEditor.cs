using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
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
        private SerializedProperty _modules;

        private SerializedProperty _enableIcon;
        private SerializedProperty _enableName;
        private SerializedProperty _enableDescription;

        private ReorderableList _modulesList;
        
        private Condition _condition;

        private const string _baseValueWarningMessage = "Base value cannot be 0!";

        private bool _displayConditionsWarning;
        private const string _conditionsWarningMessage = "Do not recursively add status datas! " +
                                                         "Avoid adding a status data to itself! " +
                                                         "Make sure there aren't two that add each other!";

        private void OnEnable()
        {
            _modules = serializedObject.FindProperty("modules");

            _modulesList = new ReorderableList(serializedObject, _modules, true, true, true, true);

            _modulesList.drawHeaderCallback = rect => {
                EditorGUI.LabelField(rect, "Modules");
            };
            _modulesList.onAddCallback = list =>
            {
                var index = list.serializedProperty.arraySize;
                // Add one element
                list.serializedProperty.arraySize++;
                // Get that element
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                // Reset all properties
                element.FindPropertyRelative("module").SetUnderlyingValue(null);
                element.FindPropertyRelative("moduleInstance").SetUnderlyingValue(null);
            };
            _modulesList.onRemoveCallback = list =>
            {
                // Remove selected or default to the last element
                if (list.selectedIndices.Count > 0)
                    foreach (int index in list.selectedIndices.OrderByDescending(x => x))
                    {
                        RemoveElementAt(index);
                    }
                else
                    RemoveElementAt(list.serializedProperty.arraySize - 1);

                EditorUtility.SetDirty(serializedObject.targetObject);
                AssetDatabase.SaveAssetIfDirty(serializedObject.targetObject);

                void RemoveElementAt(int index)
                {
                    // Get the element to remove
                    var element = _modulesList.serializedProperty.GetArrayElementAtIndex(index);
                    // Remove the scriptable object from nested assets
                    var moduleInstance = element.FindPropertyRelative("moduleInstance");
                    if (moduleInstance.objectReferenceValue != null)
                    {
                        ScriptableObject instance = moduleInstance.objectReferenceValue as ScriptableObject;
                        AssetDatabase.RemoveObjectFromAsset(instance);
                        DestroyImmediate(instance);
                    }
                    // Remove one element
                    _modulesList.serializedProperty.DeleteArrayElementAtIndex(index);
                }
            };
            _modulesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = _modulesList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += EditorGUIUtility.standardVerticalSpacing / 1.515f;
                rect.height = EditorGUI.GetPropertyHeight(element);
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };
            _modulesList.elementHeightCallback = index => 
            {
                var element = _modulesList.serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element);
            };
        }

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
            _modules = serializedObject.FindProperty("modules");

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

            _modulesList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void HorizontalLine()
        {
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f), new Color(.5f, .5f, .5f, 0.3f));

            GUILayout.Space(4f);
        }
    }
}
