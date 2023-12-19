using System;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(Condition))]
    public class ConditionalDrawer : PropertyDrawer
    {
        private const float _padding = 3;
        private const float _ifSize = 10;
        private const float _searchablePropertySize = 70;
        private const float _isSize = 10;
        private const float _existsPropertySize = 70;
        private const float _thenSize = 27;
        private const float _secondsPropertySize = 28;

        private SerializedProperty searchable;
        private SerializedProperty exists;
        private SerializedProperty add;
        private SerializedProperty configurable;
        private SerializedProperty duration;
        private SerializedProperty timing;

        private Existence existence;
        private Configurability configurability;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            searchable = property.FindPropertyRelative("searchable");
            exists = property.FindPropertyRelative("exists");
            add = property.FindPropertyRelative("add");
            configurable = property.FindPropertyRelative("configurable");
            duration = property.FindPropertyRelative("duration");
            timing = property.FindPropertyRelative("timing");

            float width = (position.width - _ifSize 
                                          - _searchablePropertySize 
                                          - _isSize 
                                          - _existsPropertySize 
                                          - _thenSize 
                                          - (add.boolValue && timing.enumValueIndex == (int)Timing.Duration ? _secondsPropertySize : 0) 
                                          - _padding * (add.boolValue && timing.enumValueIndex == (int)Timing.Duration ? 8 : add.boolValue ? 7 : 6)) 
                                          / (add.boolValue ? 3 : 2);

            Rect offset = new Rect(position.position, new Vector2(width, position.height));

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.LabelField(new Rect(offset.position, new Vector2(_ifSize, offset.height)), "If");
            offset.x += _ifSize + _padding;

            EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_searchablePropertySize, offset.height)), searchable, GUIContent.none);
            offset.x += _searchablePropertySize + _padding;

            EditorGUI.LabelField(new Rect(offset.position, new Vector2(_ifSize, offset.height)), "is");
            offset.x += _isSize + _padding;

            existence = (Existence)Convert.ToInt32(exists.boolValue);
            exists.boolValue = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(_existsPropertySize, offset.height)), existence));
            offset.x += _existsPropertySize + _padding;

            EditorGUI.LabelField(new Rect(offset.position, new Vector2(_thenSize, offset.height)), "then");
            offset.x += _thenSize + _padding;

            configurability = (Configurability)Convert.ToInt32(add.boolValue);
            add.boolValue = Convert.ToBoolean(EditorGUI.EnumPopup(offset, configurability));
            offset.x += width + _padding;

            EditorGUI.PropertyField(offset, configurable, GUIContent.none);

            if (add.boolValue)
            {
                offset.x += width + _padding;

                if (timing.enumValueIndex == (int)Timing.Duration)
                {
                    EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_secondsPropertySize, offset.height)), duration, GUIContent.none);
                    offset.x += _secondsPropertySize + _padding;
                }

                EditorGUI.PropertyField(offset, timing, GUIContent.none);
            }

            EditorGUI.EndProperty();
        }

        private enum Existence
        {
            Inactive,
            Active
        }

        private enum Configurability
        {
            Remove,
            Add
        }
    }
}
