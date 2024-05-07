using StatusEffects.Modules;
using System;
using System.Collections.Generic;
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

        private bool _displayConditionsWarning;

        private void OnEnable()
        {
            _modules = serializedObject.FindProperty("modules");
            
            int objectCount = serializedObject.targetObjects.Length;
            // Remove any loose nested scriptable objects and
            // iterate in reverse, to match the selected object order
            for (int i = objectCount - 1; i >= 0; i--)
            {
                // Get the property on the corresponding serializedObject
                var targetObject = serializedObject.targetObjects[i];
                IEnumerable<ModuleInstance> modules = (targetObject as StatusEffectData).modules.Select(m => m.moduleInstance);

                foreach (ModuleInstance nestedModule in AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(targetObject)))
                    // If there is somehow a loose module we need to clean it up
                    if (!modules.Contains(nestedModule))
                    {
                        AssetDatabase.RemoveObjectFromAsset(nestedModule);
                        DestroyImmediate(nestedModule);
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    }
            }
            AssetDatabase.SaveAssetIfDirty(serializedObject.targetObject);
            // Setup the reorderable module list
            _modulesList = new ReorderableList(serializedObject, _modules, true, true, true, true);

            _modulesList.drawHeaderCallback = rect => {
                EditorGUI.LabelField(rect, "Modules");
            };
            _modulesList.onAddCallback = list =>
            {
                var index = list.serializedProperty.arraySize;
                // Add one element
                list.serializedProperty.InsertArrayElementAtIndex(index);
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
                        DestroyObjectAt(index);
                else
                    DestroyObjectAt(list.serializedProperty.minArraySize - 1);

                EditorUtility.SetDirty(serializedObject.targetObject);
                AssetDatabase.SaveAssetIfDirty(serializedObject.targetObject);

                void DestroyObjectAt(int index)
                {
                    // Get the module instance
                    ScriptableObject moduleInstance = (serializedObject.targetObject as StatusEffectData).modules.ElementAt(index)?.moduleInstance;
                    // Remove the scriptable object from nested assets
                    if (moduleInstance != null)
                    {
                        AssetDatabase.RemoveObjectFromAsset(moduleInstance);
                        DestroyImmediate(moduleInstance);
                    }
                }

                // Remove selected or default to the last element
                if (list.selectedIndices.Count > 0)
                    foreach (int index in list.selectedIndices.OrderByDescending(x => x))
                        list.serializedProperty.DeleteArrayElementAtIndex(index);
                else
                    list.serializedProperty.DeleteArrayElementAtIndex(list.serializedProperty.minArraySize - 1);
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
                EditorGUILayout.HelpBox("Base value cannot be 0!", MessageType.Warning);
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
                EditorGUILayout.HelpBox("Do not recursively add status datas! " +
                                        "Avoid adding a status data to itself! " +
                                        "Make sure there aren't two that add each other!", MessageType.Warning);

            EditorGUILayout.PropertyField(_conditions);

            EditorGUILayout.Space(7.5f);

            if (Application.isPlaying)
                GUI.enabled = false;
            if (!serializedObject.isEditingMultipleObjects)
                _modulesList?.DoLayoutList();
            else
            {
                EditorGUILayout.BeginVertical("groupbox");
                EditorGUILayout.LabelField("Cannot multi-edit modules!");
                EditorGUILayout.EndVertical();
            }
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }

        private void HorizontalLine()
        {
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f), new Color(.5f, .5f, .5f, 0.3f));

            GUILayout.Space(4f);
        }
    }
}
