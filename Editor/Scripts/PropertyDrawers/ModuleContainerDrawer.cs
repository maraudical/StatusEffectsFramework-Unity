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
        private float _fieldSize = EditorGUIUtility.singleLineHeight;
        private float _padding = EditorGUIUtility.standardVerticalSpacing;
        private int _fieldCount = 1;

        private SerializedProperty _module;
        private SerializedProperty _moduleInstance;

        private SerializedObject instance;
        private SerializedProperty iterator;

        private Type _moduleInstanceType;
        private Attribute _attribute;

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
            // Multi-editing is not allowed
            if (property.hasMultipleDifferentValues)
            {
                Color color = GUI.color;
                GUI.color = Color.yellow;
                EditorGUI.LabelField(position, "Cannot multi-edit modules!");
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
                    AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
                    UnityEngine.Object.DestroyImmediate(instance);
                }
                if (_moduleInstance.objectReferenceValue == null)
                {
                    ScriptableObject instance = ScriptableObject.CreateInstance(_moduleInstanceType);
                    AssetDatabase.AddObjectToAsset(instance, property.serializedObject.targetObject as ScriptableObject);
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
                    _moduleInstance.SetUnderlyingValue(instance);
                }
                // Iterate through the module instance to display properties even if they are derived.
                instance = new SerializedObject(_moduleInstance.objectReferenceValue as ModuleInstance);
                instance.Update();

                iterator = instance.GetIterator();
                // Skip the script property
                iterator.NextVisible(true);

                while (iterator.NextVisible(false))
                {
                    position.y += _fieldSize + _padding;
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(position, iterator);
                    if (EditorGUI.EndChangeCheck())
                        instance.ApplyModifiedProperties();
                }
                instance.Dispose();
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _moduleInstance = property.FindPropertyRelative("moduleInstance");
            if (property.hasMultipleDifferentValues || _moduleInstance.GetUnderlyingValue() == null)
                return _fieldSize + _padding * 2;
            _fieldCount = _moduleInstance.GetUnderlyingValue().GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Length + 1;
            return (_fieldSize + _padding) * _fieldCount + _padding;
        }
    }
}