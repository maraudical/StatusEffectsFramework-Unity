using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(Condition))]
    public class ConditionalDrawer : PropertyDrawer
    {
        private const float _padding = 3;
        private const float _ifSize = 15;
        private const float _searchablePropertySize = 70;
        private const float _isSize = 15;
        private const float _existsPropertySize = 70;
        private const float _thenSize = 30;
        private const float _secondsPropertySize = 20;

        SerializedProperty searchable;
        SerializedProperty exists;
        SerializedProperty add;
        SerializedProperty configurable;
        SerializedProperty duration;
        SerializedProperty timed;

        private Existence existence;
        private Configurability configurability;
        private Timing timing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            searchable = property.FindPropertyRelative("searchable");
            exists = property.FindPropertyRelative("exists");
            add = property.FindPropertyRelative("add");
            configurable = property.FindPropertyRelative("configurable");
            duration = property.FindPropertyRelative("duration");
            timed = property.FindPropertyRelative("timed");

            float width = (position.width - _ifSize 
                                          - _searchablePropertySize 
                                          - _isSize 
                                          - _existsPropertySize 
                                          - _thenSize 
                                          - (add.boolValue && timed.boolValue ? _secondsPropertySize : 0) 
                                          - _padding * (add.boolValue && timed.boolValue ? 5 : 3)) 
                                          / (add.boolValue ? 3 : 2);

            Rect offset = new Rect(position.position, new Vector2(width, position.height));

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.LabelField(new Rect(offset.position, new Vector2(_ifSize, offset.height)), "If");
            offset.x += _ifSize;

            EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_searchablePropertySize, offset.height)), searchable, GUIContent.none);
            offset.x += _searchablePropertySize + _padding;

            EditorGUI.LabelField(new Rect(offset.position, new Vector2(_ifSize, offset.height)), "is");
            offset.x += _isSize;

            existence = (Existence)Convert.ToInt32(exists.boolValue);
            exists.boolValue = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(_existsPropertySize, offset.height)), existence));
            offset.x += _existsPropertySize + _padding;

            EditorGUI.LabelField(new Rect(offset.position, new Vector2(_thenSize, offset.height)), "then");
            offset.x += _thenSize;

            configurability = (Configurability)Convert.ToInt32(add.boolValue);
            add.boolValue = Convert.ToBoolean(EditorGUI.EnumPopup(offset, configurability));
            offset.x += width + _padding;

            EditorGUI.PropertyField(offset, configurable, GUIContent.none);

            if (add.boolValue)
            {
                offset.x += width + _padding;

                if (timed.boolValue)
                {
                    EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_secondsPropertySize, offset.height)), duration, GUIContent.none);
                    offset.x += _secondsPropertySize + _padding;
                }

                timing = (Timing)Convert.ToInt32(timed.boolValue);
                timed.boolValue = Convert.ToBoolean(EditorGUI.EnumPopup(offset, timing));
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
        private enum Timing
        {
            Infinite,
            Seconds
        }
    }
}
