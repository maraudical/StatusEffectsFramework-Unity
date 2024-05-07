using StatusEffects.Modules;
using System;
using Unity.VisualScripting;
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

        private Type _moduleInstanceType;
        private Attribute _attribute;

        private bool _changeCheck;

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

            EditorGUI.BeginProperty(position, label, property);

            position.height = _fieldSize;
            position.y += _padding;
            // Draw base module reference
            EditorGUI.PropertyField(position, _module, GUIContent.none);
            position.y += _fieldSize + _padding;

            if (!_module.hasMultipleDifferentValues)
            {
                var sp = property.serializedObject.GetIterator();
                // If the module reference was removed destroy the instance
                if (_module.objectReferenceValue == null && _moduleInstance.objectReferenceValue != null)
                {
                    ScriptableObject instance = _moduleInstance.objectReferenceValue as ScriptableObject;
                    AssetDatabase.RemoveObjectFromAsset(instance);
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                if (_module.objectReferenceValue != null)
                {
                    _attribute = Attribute.GetCustomAttribute(_module.objectReferenceValue.GetType(), typeof(AttachModuleInstanceAttribute));

                    if (_attribute == null)
                        return;

                    _moduleInstanceType = ((AttachModuleInstanceAttribute)_attribute).type;
                    // If the module reference was changed we add or destroy the instance, note that
                    // we also check in the StatusEffectDataEditor for when list items are removed.
                    if (_moduleInstance.objectReferenceValue != null && _moduleInstanceType != _moduleInstance.objectReferenceValue.GetType())
                    {
                        ScriptableObject instance = _moduleInstance.objectReferenceValue as ScriptableObject;
                        AssetDatabase.RemoveObjectFromAsset(instance);
                        EditorUtility.SetDirty(property.serializedObject.targetObject);
                        UnityEngine.Object.DestroyImmediate(instance);
                    }
                    if (_moduleInstance.objectReferenceValue == null)
                    {
                        ScriptableObject instance = ScriptableObject.CreateInstance(_moduleInstanceType);
                        AssetDatabase.AddObjectToAsset(instance, property.serializedObject.targetObject as ScriptableObject);
                        EditorUtility.SetDirty(property.serializedObject.targetObject);
                        _moduleInstance.SetUnderlyingValue(instance);
                    }
                    // Iterate through the module instance to display properties even if they are derived.
                    _instance = new SerializedObject(_moduleInstance.objectReferenceValue as ModuleInstance);
                    _instance.Update();

                    _iterator = _instance.GetIterator();
                    // Skip the script property
                    _iterator.NextVisible(true);

                    _changeCheck = false;

                    while (_iterator.NextVisible(false))
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(position, _iterator);
                        if (EditorGUI.EndChangeCheck())
                            _changeCheck = true;
                        position.y += EditorGUI.GetPropertyHeight(_iterator) + _padding;
                    }

                    if (_changeCheck)
                        _instance.ApplyModifiedProperties();

                    _instance.Dispose();
                }
            }

            EditorGUI.EndProperty();

            AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _module = property.FindPropertyRelative("module");
            _moduleInstance = property.FindPropertyRelative("moduleInstance");

            if (_module.hasMultipleDifferentValues || _moduleInstance.objectReferenceValue == null)
                return _fieldSize + _padding * 2;

            _derivedFieldCount = 0; 
            _derivedPropertyHeight = 0;
            // Iterate through the module instance to display properties even if they are derived.
            _instance = new SerializedObject(_moduleInstance.objectReferenceValue as ModuleInstance);
            _instance.Update();

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
        }
    }
}