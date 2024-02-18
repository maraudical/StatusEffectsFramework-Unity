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
        private const float _addRemoveSize = 70;
        private const float _configurableSize = 70;
        private const float _secondsPropertySize = 28;
        private const float _timingSize = 70;

        private const float _expandWindowSize = 115;

        private SerializedProperty searchable;
        private SerializedProperty exists;
        private SerializedProperty add;
        private SerializedProperty configurable;
        private SerializedProperty configurableReference;
        private SerializedProperty duration;
        private SerializedProperty timing;

        private Existence existence;
        private Configurability configurability;

        private float _positionWidth;

        private bool _restoreShowMixedValue;
        private bool _value;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            searchable = property.FindPropertyRelative("searchable");
            exists = property.FindPropertyRelative("exists");
            add = property.FindPropertyRelative("add");
            configurable = property.FindPropertyRelative("configurable");
            configurableReference = property.FindPropertyRelative(configurable.enumValueIndex == (int)ConditionalConfigurable.Data || add.boolValue ? "data" 
                                                                : configurable.enumValueIndex == (int)ConditionalConfigurable.Name                  ? "comparableName"
                                                                                                                                                    : "group");
            duration = property.FindPropertyRelative("duration");
            timing = property.FindPropertyRelative("timing");

            if (position.width > 0)
                _positionWidth = position.width;

            float width = (position.width - _ifSize 
                                          - _searchablePropertySize 
                                          - _isSize 
                                          - _existsPropertySize 
                                          - _thenSize 
                                          - _addRemoveSize
                                          - (add.boolValue && timing.enumValueIndex == (int)ConditionalTiming.Duration ? _secondsPropertySize : add.boolValue ? 0 : _configurableSize) 
                                          - (add.boolValue ? _timingSize : 0)
                                          - _padding * (add.boolValue && timing.enumValueIndex == (int)ConditionalTiming.Duration ? 8 : 7));

            float currentWidth = 0;
            Rect offset = new Rect(position.position, new Vector2(width, position.height));

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.LabelField(new Rect(offset.position, new Vector2(_ifSize, offset.height)), "If");
            offset.x += _ifSize + _padding;
            currentWidth += _ifSize + _padding;

            if (!CheckForSpace(_searchablePropertySize + _expandWindowSize))
                return;
            EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_searchablePropertySize, offset.height)), searchable, GUIContent.none);
            offset.x += _searchablePropertySize + _padding;
            currentWidth += _searchablePropertySize + _padding;

            if (!CheckForSpace(_isSize + _expandWindowSize))
                return;
            EditorGUI.LabelField(new Rect(offset.position, new Vector2(_ifSize, offset.height)), "is");
            offset.x += _isSize + _padding;
            currentWidth += _isSize + _padding;

            if (!CheckForSpace(_existsPropertySize + _expandWindowSize))
                return;
            existence = (Existence)Convert.ToInt32(exists.boolValue);
            _restoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = exists.hasMultipleDifferentValues;
            _value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(_existsPropertySize, offset.height)), existence));
            if (_value != exists.boolValue)
                exists.boolValue = _value;
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
            configurability = (Configurability)Convert.ToInt32(add.boolValue);
            _restoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = add.hasMultipleDifferentValues;
            _value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(_addRemoveSize, offset.height)), configurability));
            if (_value != add.boolValue)
                add.boolValue = _value;
            EditorGUI.showMixedValue = _restoreShowMixedValue;
            offset.x += _addRemoveSize + _padding;
            currentWidth += _addRemoveSize + _padding;

            if (add.hasMultipleDifferentValues)
            {
                if (!CheckForSpace(_expandWindowSize + 5))
                    return;

                EditorGUI.LabelField(new Rect(offset.position, new Vector2(_positionWidth - currentWidth, offset.height)), "(Different add/remove)");
                return;
            }

            if (!add.boolValue)
            {
                if (!CheckForSpace(_configurableSize + _expandWindowSize - 60))
                    return;
                EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_configurableSize, offset.height)), configurable, GUIContent.none);
                offset.x += _configurableSize + _padding;
            }
            else if (!CheckForSpace(_expandWindowSize + 20))
                return;

            EditorGUI.PropertyField(offset, configurableReference, GUIContent.none);

            if (add.boolValue)
            {
                offset.x += width + _padding;

                if (timing.enumValueIndex == (int)ConditionalTiming.Duration)
                {
                    EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_secondsPropertySize, offset.height)), duration, GUIContent.none);
                    offset.x += _secondsPropertySize + _padding;
                }
                
                EditorGUI.PropertyField(new Rect(offset.position, new Vector2(_timingSize, offset.height)), timing, GUIContent.none);
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
