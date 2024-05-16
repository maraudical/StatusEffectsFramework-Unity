using StatusEffects.Modules;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(ModuleContainer))]
    public class ModuleContainerDrawer : PropertyDrawer
    {
        private SerializedProperty _module;
        private SerializedProperty _moduleInstance;

        private SerializedObject _instance;
        private SerializedProperty _iterator;

        private float _fieldSize = EditorGUIUtility.singleLineHeight;
        private float _padding = EditorGUIUtility.standardVerticalSpacing;
        private float _derivedPropertyHeight;
        private float _derivedFieldCount;

        private bool _containsNullModule;
        private object _moduleReference;
        private object _moduleInstanceReference;
        private Type _previousModuleInstanceType;
        private Type _moduleInstanceType;
        private Attribute _attribute;

        private List<UnityEngine.Object> _moduleInstances;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.targetObject is not ScriptableObject)
            {
                Color color = GUI.color;
                GUI.color = Color.yellow;
                EditorGUI.LabelField(position, "The containing class must be a Scriptable Object!");
                GUI.color = color;
                return;
            }

            _module = property.FindPropertyRelative("module");
            _moduleInstance = property.FindPropertyRelative("moduleInstance");

            property.serializedObject.Update();

            EditorGUI.BeginProperty(position, label, property);

            position.height = _fieldSize;
            position.y += _padding;
            // Draw base module reference
            EditorGUI.PropertyField(position, _module, GUIContent.none);
            position.y += _fieldSize + _padding;
            
            if (_moduleInstances == null)
                _moduleInstances = new();
            else
                _moduleInstances.Clear();

            _containsNullModule = false;
            _previousModuleInstanceType = null;
            // Find all relevant module instance references
            foreach (var target in property.serializedObject.targetObjects)
            {
                _moduleReference = _module.GetParent(target).GetValue("module");
                _moduleInstanceReference = _moduleInstance.GetParent(target).GetValue("moduleInstance");
                // If the module reference was removed destroy the instance
                if (_moduleReference == null && _moduleInstanceReference != null && !_moduleInstanceReference.Equals(null))
                {
                    ScriptableObject instance = _moduleInstanceReference as ScriptableObject;
                    AssetDatabase.RemoveObjectFromAsset(instance);
                    EditorUtility.SetDirty(target);
                    UnityEngine.Object.DestroyImmediate(instance);
                }
                if (_moduleReference != null)
                {
                    _attribute = Attribute.GetCustomAttribute(_moduleReference.GetType(), typeof(AttachModuleInstanceAttribute));

                    if (_attribute == null)
                        goto EndProperty;

                    _moduleInstanceType = ((AttachModuleInstanceAttribute)_attribute).type;

                    // If the module reference was changed we add or destroy the instance, note that
                    // we also check in the StatusEffectDataEditor for when list items are removed.
                    if (_moduleInstanceReference != null && !_moduleInstanceReference.Equals(null) && _moduleInstanceType != _moduleInstanceReference.GetType())
                    {
                        ScriptableObject instance = _moduleInstanceReference as ScriptableObject;
                        AssetDatabase.RemoveObjectFromAsset(instance);
                        UnityEngine.Object.DestroyImmediate(instance);
                        EditorUtility.SetDirty(target);
                    }
                    if (_moduleInstanceReference == null || _moduleInstanceReference.Equals(null))
                    {
                        ScriptableObject instance = ScriptableObject.CreateInstance(_moduleInstanceType);
                        AssetDatabase.AddObjectToAsset(instance, target as ScriptableObject);
                        _moduleInstance.GetParent(target).SetValue("moduleInstance", instance);
                        _moduleInstanceReference = instance;
                        EditorUtility.SetDirty(target);
                    }
                    // Check if there are different module instance types
                    if (_previousModuleInstanceType != null && _previousModuleInstanceType != _moduleInstanceType)
                        goto EndProperty;
                    
                    _previousModuleInstanceType = _moduleInstanceType;
                    _moduleInstances.Add(_moduleInstanceReference as ModuleInstance);
                }
                else
                    _containsNullModule = true;

                AssetDatabase.SaveAssetIfDirty(target);
            }

            if (_containsNullModule || _moduleInstances.Count <= 0)
                goto EndProperty;
            // Iterate through the module instance to display properties even if they are derived.
            _instance = new SerializedObject(_moduleInstances.ToArray());

            _iterator = _instance.GetIterator();
            // Skip the script property
            _iterator.NextVisible(true);

            while (_iterator.NextVisible(false))
            {
                EditorGUI.PropertyField(position, _iterator);
                position.y += EditorGUI.GetPropertyHeight(_iterator) + _padding;
            }

            _instance.ApplyModifiedProperties();
            _instance.Dispose();

            EndProperty:

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _module = property.FindPropertyRelative("module");
            _moduleInstance = property.FindPropertyRelative("moduleInstance");

            if (_module.objectReferenceValue == null || _moduleInstance.objectReferenceValue == null)
                return DrawDefault();

            _attribute = Attribute.GetCustomAttribute(_module.objectReferenceValue.GetType(), typeof(AttachModuleInstanceAttribute));

            if (_attribute == null)
                return DrawDefault();

            _moduleInstanceType = ((AttachModuleInstanceAttribute)_attribute).type;

            foreach (var target in property.serializedObject.targetObjects)
            {
                _moduleInstanceReference = _moduleInstance.GetParent(target).GetValue("moduleInstance");

                if (_moduleInstanceType != _moduleInstanceReference.GetType())
                    return DrawDefault();

                _moduleInstanceType = _moduleInstanceReference.GetType();
            }

            _derivedFieldCount = 0; 
            _derivedPropertyHeight = 0;
            // Iterate through the module instance to display properties even if they are derived.
            _instance = new SerializedObject(_moduleInstance.objectReferenceValue);

            _iterator = _instance.GetIterator();
            // Skip the script property
            _iterator.NextVisible(true);

            while (_iterator.NextVisible(false))
            {
                _derivedFieldCount++;
                _derivedPropertyHeight += EditorGUI.GetPropertyHeight(_iterator);
            }
            
            _instance.Dispose();
            
            return _fieldSize + _padding * (_derivedFieldCount + 2) + _derivedPropertyHeight;

            float DrawDefault()
            {
                return _fieldSize + _padding * 2;
            }
        }
    }
}