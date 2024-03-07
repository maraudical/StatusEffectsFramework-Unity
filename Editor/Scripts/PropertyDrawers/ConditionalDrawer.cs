using System;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(Condition))]
    public class ConditionalDrawer : PropertyDrawer
    {
        private SerializedProperty _searchable;
        private SerializedProperty _exists;
        private SerializedProperty _add;
        private SerializedProperty _configurable;
        private SerializedProperty _configurableReference;
        private SerializedProperty _duration;
        private SerializedProperty _timing;

        private Existence _existence;
        private Configurability _configurability;

        private float _positionWidth;

        private bool _restoreShowMixedValue;
        private bool _value;

        private const float _padding = 3;
        private const float _ifSize = 10;
        private const float _searchablePropertySize = 70;
        private const float _isSize = 10;
        private const float _existsPropertySize = 70;
        private const float _thenSize = 27;
        private const float _addRemoveSize = 70;
        private const float _configurableSize = 70;
        private const float _secondsPropertySize = 28;
        private const float _timingSize = 70;
        private const float _expandWindowSize = 115;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _searchable = property.FindPropertyRelative("searchable");
            _exists = property.FindPropertyRelative("exists");
            _add = property.FindPropertyRelative("add");
            _configurable = property.FindPropertyRelative("configurable");
            _configurableReference = property.FindPropertyRelative(_configurable.enumValueIndex == (int)ConditionalConfigurable.Data || _add.boolValue ? "data" 
                                                                : _configurable.enumValueIndex == (int)ConditionalConfigurable.Name                  ? "comparableName"
                                                                                                                                                    : "group");
            _duration = property.FindPropertyRelative("duration");
            _timing = property.FindPropertyRelative("timing");

            if (position.width > 0)
                _positionWidth = position.width;

            float width = (position.width - _ifSize 
                                          - _searchablePropertySize 
                                          - _isSize 
                                          - _existsPropertySize 
                                          - _thenSize 
                                          - _addRemoveSize
                                          - (_add.boolValue && _timing.enumValueIndex == (int)ConditionalTiming.Duration ? _secondsPropertySize : _add.boolValue ? 0 : _configurableSize) 
                                          - (_add.boolValue ? _timingSize : 0)
                                          - _padding * (_add.boolValue && _timing.enumValueIndex == (int)ConditionalTiming.Duration ? 8 : 7));

            float currentWidth = 0;
            Rect offset = new Rect(position.position, new Vector2(width, position.height));

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.LabelField(new Rect(offset.position, new Vector2(_ifSize, offset.height)), "If");
            offset.x += _ifSize + _padding;
            currentWidth += _ifSize + _padding;

            if (!CheckForSpace(_searchablePropertySize + _expandWindowSize))
                return;
            EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_searchablePropertySize, offset.height)), _searchable, GUIContent.none);
            offset.x += _searchablePropertySize + _padding;
            currentWidth += _searchablePropertySize + _padding;

            if (!CheckForSpace(_isSize + _expandWindowSize))
                return;
            EditorGUI.LabelField(new Rect(offset.position, new Vector2(_ifSize, offset.height)), "is");
            offset.x += _isSize + _padding;
            currentWidth += _isSize + _padding;

            if (!CheckForSpace(_existsPropertySize + _expandWindowSize))
                return;
            _existence = (Existence)Convert.ToInt32(_exists.boolValue);
            _restoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = _exists.hasMultipleDifferentValues;
            _value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(_existsPropertySize, offset.height)), _existence));
            if (_value != _exists.boolValue)
                _exists.boolValue = _value;
            EditorGUI.showMixedValue = _restoreShowMixedValue;
            offset.x += _existsPropertySize + _padding;
            currentWidth += _existsPropertySize + _padding;

            if (!CheckForSpace(_thenSize + _expandWindowSize))
                return;
            EditorGUI.LabelField(new Rect(offset.position, new Vector2(_thenSize, offset.height)), "then");
            offset.x += _thenSize + _padding;
            currentWidth += _thenSize + _padding;

            if (!CheckForSpace(_addRemoveSize + _expandWindowSize))
                return;
            _configurability = (Configurability)Convert.ToInt32(_add.boolValue);
            _restoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = _add.hasMultipleDifferentValues;
            _value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(_addRemoveSize, offset.height)), _configurability));
            if (_value != _add.boolValue)
                _add.boolValue = _value;
            EditorGUI.showMixedValue = _restoreShowMixedValue;
            offset.x += _addRemoveSize + _padding;
            currentWidth += _addRemoveSize + _padding;

            if (_add.hasMultipleDifferentValues)
            {
                if (!CheckForSpace(_expandWindowSize + 5))
                    return;

                EditorGUI.LabelField(new Rect(offset.position, new Vector2(_positionWidth - currentWidth, offset.height)), "(Different add/remove)");
                return;
            }

            if (!_add.boolValue)
            {
                if (!CheckForSpace(_configurableSize + _expandWindowSize - 60))
                    return;
                EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_configurableSize, offset.height)), _configurable, GUIContent.none);
                offset.x += _configurableSize + _padding;
            }
            else if (!CheckForSpace(_expandWindowSize + 20))
                return;

            EditorGUI.PropertyField(offset, _configurableReference, GUIContent.none);

            if (_add.boolValue)
            {
                offset.x += width + _padding;

                if (_timing.enumValueIndex == (int)ConditionalTiming.Duration)
                {
                    EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_secondsPropertySize, offset.height)), _duration, GUIContent.none);
                    offset.x += _secondsPropertySize + _padding;
                }
                
                EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_timingSize, offset.height)), _timing, GUIContent.none);
            }
            
            EditorGUI.EndProperty();

            bool CheckForSpace(float roomNeeded)
            {
                if (currentWidth + roomNeeded > _positionWidth)
                {
                    GUI.color = Color.yellow;
                    EditorGUI.LabelField(new Rect(offset.position, new Vector2(_positionWidth - currentWidth, offset.height)), "(Expand window...)");
                    GUI.color = Color.white;
                    return false;
                }
                return true;
            }
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
