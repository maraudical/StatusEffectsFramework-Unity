#if UNITY_2023_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#else
using UnityEngine;
#endif
using System;
using UnityEditor;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(Condition))]
    internal class ConditionDrawer : PropertyDrawer
    {
#if UNITY_2023_1_OR_NEWER
        public VisualTreeAsset VisualTree;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var searchableConfigurable = root.Q<PropertyField>("searchable-configurable");
            var searchableData = root.Q<PropertyField>("searchable-data");
            var searchableComparableName = root.Q<PropertyField>("searchable-comparable-name");
            var searchableGroup = root.Q<PropertyField>("searchable-group");
            var isLabel = root.Q<Label>("is-label");
            var existence = root.Q<EnumField>("existence");
            var thenLabel = root.Q<Label>("then-label");
            var configurability = root.Q<EnumField>("configurability");
            var stacks = root.Q<PropertyField>("stacks");
            var scaleOption = root.Q<EnumField>("scale-option");
            var removeOption = root.Q<EnumField>("remove-option");
            var stacksLabel = root.Q<Label>("stacks-label");
            var ofLabel = root.Q<Label>("of-label");
            var actionConfigurable = root.Q<PropertyField>("action-configurable");
            var actionData = root.Q<PropertyField>("action-data");
            var actionComparableName = root.Q<PropertyField>("action-comparable-name");
            var actionGroup = root.Q<PropertyField>("action-group");
            var duration = root.Q<PropertyField>("duration");
            var timing = root.Q<PropertyField>("timing");
            var dashLabel = root.Q<Label>("dash-label");

            var searchableConfigurableProperty = property.FindPropertyRelative($"m_{nameof(Condition.SearchableConfigurable)}");
            var existsProperty = property.FindPropertyRelative($"m_{nameof(Condition.Exists)}");
            var addProperty = property.FindPropertyRelative($"m_{nameof(Condition.Add)}");
            var scaledProperty = property.FindPropertyRelative($"m_{nameof(Condition.Scaled)}");
            var useStacksProperty = property.FindPropertyRelative($"m_{nameof(Condition.UseStacks)}");
            var actionConfigurableProperty = property.FindPropertyRelative($"m_{nameof(Condition.ActionConfigurable)}");
            var timingProperty = property.FindPropertyRelative($"m_{nameof(Condition.Timing)}");

            searchableConfigurable.label = string.Empty;
            searchableConfigurable.RegisterValueChangeCallback(SearchableConfigurableValueChanged);

            searchableData.label = string.Empty;

            searchableComparableName.label = string.Empty;

            searchableGroup.RegisterCallbackOnce<GeometryChangedEvent>(SearchableGroupGeometryChanged);

            existence.SetValueWithoutNotify((Existence)Convert.ToInt32(existsProperty.boolValue));
            existence.showMixedValue = existsProperty.hasMultipleDifferentValues;
            existence.RegisterValueChangedCallback(ExistenceValueChanged);

            configurability.SetValueWithoutNotify((Configurability)Convert.ToInt32(addProperty.boolValue));
            configurability.showMixedValue = addProperty.hasMultipleDifferentValues;
            configurability.RegisterValueChangedCallback(ConfigurabilityValueChanged);

            scaleOption.SetValueWithoutNotify((ScaleOption)Convert.ToInt32(scaledProperty.boolValue));
            scaleOption.showMixedValue = scaledProperty.hasMultipleDifferentValues;
            scaleOption.RegisterValueChangedCallback(ScaleOptionValueChanged);

            removeOption.SetValueWithoutNotify((RemoveOption)Convert.ToInt32(useStacksProperty.boolValue));
            removeOption.showMixedValue = useStacksProperty.hasMultipleDifferentValues;
            removeOption.RegisterValueChangedCallback(RemoveOptionValueChanged);

            actionConfigurable.label = string.Empty;
            actionConfigurable.RegisterValueChangeCallback(ActionConfigurableValueChanged);

            actionData.label = string.Empty;

            actionComparableName.label = string.Empty;

            actionGroup.RegisterCallbackOnce<GeometryChangedEvent>(ActionGroupGeometryChanged);

            duration.label = string.Empty;

            timing.label = string.Empty;
            timing.RegisterValueChangeCallback(TimingValueChanged);

            EvaluateProperties();

            return root;

            void SearchableConfigurableValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
            }

            void SearchableGroupGeometryChanged(GeometryChangedEvent changeEvent)
            {
                searchableGroup.Q<MaskField>("mask-field").label = string.Empty;
            }

            void ExistenceValueChanged(ChangeEvent<Enum> changeEvent)
            {
                existsProperty.boolValue = Convert.ToBoolean((int)(Existence)changeEvent.newValue);
                existsProperty.serializedObject.ApplyModifiedProperties();
            }

            void ConfigurabilityValueChanged(ChangeEvent<Enum> changeEvent)
            {
                addProperty.boolValue = Convert.ToBoolean((int)(Configurability)changeEvent.newValue);
                addProperty.serializedObject.ApplyModifiedProperties();
                EvaluateProperties();
            }

            void ScaleOptionValueChanged(ChangeEvent<Enum> changeEvent)
            {
                scaledProperty.boolValue = Convert.ToBoolean((int)(ScaleOption)changeEvent.newValue);
                scaledProperty.serializedObject.ApplyModifiedProperties();
            }

            void RemoveOptionValueChanged(ChangeEvent<Enum> changeEvent)
            {
                useStacksProperty.boolValue = Convert.ToBoolean((int)(RemoveOption)changeEvent.newValue);
                useStacksProperty.serializedObject.ApplyModifiedProperties();
                EvaluateProperties();
            }

            void ActionConfigurableValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
            }

            void ActionGroupGeometryChanged(GeometryChangedEvent changeEvent)
            {
                actionGroup.Q<MaskField>("mask-field").label = string.Empty;
            }

            void TimingValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
            }

            void EvaluateProperties()
            {
                var searchableConfigurableValue = (ConditionalConfigurable)searchableConfigurableProperty.enumValueIndex;
                var actionConfigurableValue = (ConditionalConfigurable)actionConfigurableProperty.enumValueIndex;
                var conditionalTiming = (ConditionalTiming)timingProperty.enumValueIndex;

                bool searchableDifference = searchableConfigurableProperty.hasMultipleDifferentValues;
                bool addDifference = searchableDifference || addProperty.hasMultipleDifferentValues;
                bool useStacksDifference = addDifference || (!addProperty.boolValue && useStacksProperty.hasMultipleDifferentValues);
                bool actionDifference = addDifference || (!addProperty.boolValue && actionConfigurableProperty.hasMultipleDifferentValues);
                bool timingDifference = addDifference || (addProperty.boolValue && timingProperty.hasMultipleDifferentValues);

                bool anyDifference = searchableDifference || addDifference || useStacksDifference || actionDifference;

                searchableData.style.display = !searchableDifference && searchableConfigurableValue is ConditionalConfigurable.Data ? DisplayStyle.Flex : DisplayStyle.None;
                searchableComparableName.style.display = !searchableDifference && searchableConfigurableValue is ConditionalConfigurable.Name ? DisplayStyle.Flex : DisplayStyle.None;
                searchableGroup.style.display = !searchableDifference && searchableConfigurableValue is ConditionalConfigurable.Group ? DisplayStyle.Flex : DisplayStyle.None;
                isLabel.style.display = !searchableDifference ? DisplayStyle.Flex : DisplayStyle.None;
                existence.style.display = !searchableDifference ? DisplayStyle.Flex : DisplayStyle.None;
                thenLabel.style.display = !searchableDifference ? DisplayStyle.Flex : DisplayStyle.None;
                configurability.style.display = !searchableDifference ? DisplayStyle.Flex : DisplayStyle.None;
                stacks.style.display = !addDifference && !useStacksDifference && (addProperty.boolValue || useStacksProperty.boolValue) ? DisplayStyle.Flex : DisplayStyle.None;
                scaleOption.style.display = !addDifference && !useStacksDifference && (addProperty.boolValue || useStacksProperty.boolValue) ? DisplayStyle.Flex : DisplayStyle.None;
                removeOption.style.display = !addDifference && !addProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                stacksLabel.style.display = !addDifference && addProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                ofLabel.style.display = !addDifference && !useStacksDifference ? DisplayStyle.Flex : DisplayStyle.None;
                actionConfigurable.style.display = !useStacksDifference && !addProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                actionData.style.display = !addDifference && !actionDifference && (addProperty.boolValue || actionConfigurableValue is ConditionalConfigurable.Data) ? DisplayStyle.Flex : DisplayStyle.None;
                actionComparableName.style.display = !addDifference && !actionDifference && !addProperty.boolValue && actionConfigurableValue is ConditionalConfigurable.Name ? DisplayStyle.Flex : DisplayStyle.None;
                actionGroup.style.display = !addDifference && !actionDifference && !addProperty.boolValue && actionConfigurableValue is ConditionalConfigurable.Group ? DisplayStyle.Flex : DisplayStyle.None;
                duration.style.display = !timingDifference && addProperty.boolValue && conditionalTiming is ConditionalTiming.Duration ? DisplayStyle.Flex : DisplayStyle.None;
                timing.style.display = !addDifference && addProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                dashLabel.style.display = anyDifference ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
#else
        private SerializedProperty m_SearchableConfigurable;
        private SerializedProperty m_SearchableReference;
        private SerializedProperty m_Exists;
        private SerializedProperty m_Add;
        private SerializedProperty m_Scaled;
        private SerializedProperty m_UseStacks;
        private SerializedProperty m_Stacks;
        private SerializedProperty m_ActionConfigurable;
        private SerializedProperty m_ActionReference;
        private SerializedProperty m_Duration;
        private SerializedProperty m_Timing;

        private readonly float m_FieldSize = EditorGUIUtility.singleLineHeight;

        private const float k_MinimumSize = 40;

        private const float k_Padding = 3;
        private const float k_IfSize = 10;
        private const float k_IsSize = 12;
        private const float k_ExistsPropertySize = 70;
        private const float k_ThenSize = 28;
        private const float k_AddRemoveSize = 70;
        private const float k_ScaledSize = 77;
        private const float k_RemoveOptionStackSize = 98;
        private const float k_RemoveOptionAllSize = 40;
        private const float k_AddOptionSize = 70;
        private const float k_StacksSize = 28;
        private const float k_ConfigurableSize = 70;
        private const float k_SecondsPropertySize = 28;
        private const float k_TimingSize = 70;
        private const float k_ExpandWindowSize = 115;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_SearchableConfigurable = property.FindPropertyRelative($"m_{nameof(Condition.SearchableConfigurable)}");
            m_SearchableReference = property.FindPropertyRelative(m_SearchableConfigurable.enumValueIndex == (int)ConditionalConfigurable.Data ? $"m_{nameof(Condition.SearchableData)}"
                                                                : m_SearchableConfigurable.enumValueIndex == (int)ConditionalConfigurable.Name ? $"m_{nameof(Condition.SearchableComparableName)}"
                                                                                                                                               : $"m_{nameof(Condition.SearchableGroup)}");
            m_Exists = property.FindPropertyRelative($"m_{nameof(Condition.Exists)}");
            m_Add = property.FindPropertyRelative($"m_{nameof(Condition.Add)}");
            m_Scaled = property.FindPropertyRelative($"m_{nameof(Condition.Scaled)}");
            m_UseStacks = property.FindPropertyRelative($"m_{nameof(Condition.UseStacks)}");
            m_Stacks = property.FindPropertyRelative($"m_{nameof(Condition.Stacks)}");
            m_ActionConfigurable = property.FindPropertyRelative($"m_{nameof(Condition.ActionConfigurable)}");
            m_ActionReference = property.FindPropertyRelative(m_ActionConfigurable.enumValueIndex == (int)ConditionalConfigurable.Data || m_Add.boolValue ? $"m_{nameof(Condition.ActionData)}"
                                                            : m_ActionConfigurable.enumValueIndex == (int)ConditionalConfigurable.Name ? $"m_{nameof(Condition.ActionComparableName)}"
                                                                                                                                       : $"m_{nameof(Condition.ActionGroup)}");
            m_Duration = property.FindPropertyRelative($"m_{nameof(Condition.Duration)}");
            m_Timing = property.FindPropertyRelative($"m_{nameof(Condition.Timing)}");

            var positionWidth = 0f;

            if (position.width > 0)
                positionWidth = position.width;

            position.height = m_FieldSize;

            float width = (position.width - k_IfSize
                                          - k_ConfigurableSize
                                          - k_IsSize
                                          - k_ExistsPropertySize
                                          - k_ThenSize
                                          - k_AddRemoveSize
                                          - (m_Add.boolValue || m_UseStacks.boolValue ? k_ScaledSize : 0)
                                          - (m_Add.boolValue ? k_AddOptionSize : m_UseStacks.boolValue ? k_RemoveOptionStackSize : k_RemoveOptionAllSize)
                                          - (m_Add.boolValue && m_Timing.enumValueIndex == (int)ConditionalTiming.Duration ? k_SecondsPropertySize : m_Add.boolValue ? 0 : k_ConfigurableSize)
                                          - (m_Add.boolValue ? k_TimingSize : 0)
                                          - k_Padding * (m_Add.boolValue && m_Timing.enumValueIndex == (int)ConditionalTiming.Duration ? 11 : m_Add.boolValue || m_UseStacks.boolValue ? 10 : 9));

            float currentWidth = 0;
            Rect offset = new Rect(position.position, new Vector2(Mathf.Max(k_MinimumSize, width / 2), position.height));

            EditorGUI.BeginProperty(position, label, property);

            if (!CheckForSpace(k_IfSize + k_ExpandWindowSize))
                return;
            EditorGUI.LabelField(new Rect(offset.position, new Vector2(k_IfSize, offset.height)), "If");
            offset.x += k_IfSize + k_Padding;
            currentWidth += k_IfSize + k_Padding;

            if (!CheckForSpace(k_ConfigurableSize + k_ExpandWindowSize))
                return;
            EditorGUI.PropertyField(new Rect(offset.position, new Vector2(k_ConfigurableSize, offset.height)), m_SearchableConfigurable, GUIContent.none);
            offset.x += k_ConfigurableSize + k_Padding;
            currentWidth += k_ConfigurableSize + k_Padding;

            if (!CheckForSpace(offset.size.x + k_ExpandWindowSize))
                return;
            EditorGUI.PropertyField(offset, m_SearchableReference, GUIContent.none);
            offset.x += offset.width + k_Padding;
            currentWidth += offset.width + k_Padding;

            if (!CheckForSpace(k_IsSize + k_ExpandWindowSize))
                return;
            EditorGUI.LabelField(new Rect(offset.position, new Vector2(k_IsSize, offset.height)), "is");
            offset.x += k_IsSize + k_Padding;
            currentWidth += k_IsSize + k_Padding;

            if (!CheckForSpace(k_ExistsPropertySize + k_ExpandWindowSize))
                return;
            var existence = (Existence)Convert.ToInt32(m_Exists.boolValue);
            var restoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = m_Exists.hasMultipleDifferentValues;
            var value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(k_ExistsPropertySize, offset.height)), existence));
            if (value != m_Exists.boolValue)
                m_Exists.boolValue = value;
            EditorGUI.showMixedValue = restoreShowMixedValue;
            offset.x += k_ExistsPropertySize + k_Padding;
            currentWidth += k_ExistsPropertySize + k_Padding;

            if (!CheckForSpace(k_ThenSize + k_ExpandWindowSize))
                return;
            EditorGUI.LabelField(new Rect(offset.position, new Vector2(k_ThenSize, offset.height)), "then");
            offset.x += k_ThenSize + k_Padding;
            currentWidth += k_ThenSize + k_Padding;

            if (!CheckForSpace(k_AddRemoveSize + k_ExpandWindowSize))
                return;
            var configurability = (Configurability)Convert.ToInt32(m_Add.boolValue);
            restoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = m_Add.hasMultipleDifferentValues;
            value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(k_AddRemoveSize, offset.height)), configurability));
            if (value != m_Add.boolValue)
            {
                m_Add.boolValue = value;
                if (value)
                    m_UseStacks.boolValue = true;
            }
            EditorGUI.showMixedValue = restoreShowMixedValue;
            offset.x += k_AddRemoveSize + k_Padding;
            currentWidth += k_AddRemoveSize + k_Padding;

            if (m_Add.hasMultipleDifferentValues)
            {
                if (!CheckForSpace(k_ExpandWindowSize + 20))
                    return;

                EditorGUI.LabelField(new Rect(offset.position, new Vector2(positionWidth - currentWidth, offset.height)), "(Different add/remove)");
                return;
            }

            if (m_Add.boolValue || m_UseStacks.boolValue)
            {
                if (!CheckForSpace(k_ScaledSize + k_ExpandWindowSize))
                    return;
                var scaleOption = (ScaleOption)Convert.ToInt32(m_Scaled.boolValue);
                restoreShowMixedValue = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = m_Scaled.hasMultipleDifferentValues;
                value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(k_ScaledSize, offset.height)), scaleOption));
                if (value != m_Scaled.boolValue)
                    m_Scaled.boolValue = value;
                EditorGUI.showMixedValue = restoreShowMixedValue;
                offset.x += k_ScaledSize + k_Padding;
                currentWidth += k_ScaledSize + k_Padding;
            }

            if (!m_Add.boolValue)
            {
                if (!CheckForSpace((m_UseStacks.boolValue ? k_RemoveOptionStackSize : k_RemoveOptionAllSize) + k_ExpandWindowSize + 2))
                    return;
                if (m_UseStacks.boolValue)
                {
                    EditorGUI.PropertyField(new Rect(offset.position, new Vector2(k_StacksSize, offset.height)), m_Stacks, GUIContent.none);
                    offset.x += k_StacksSize + k_Padding;
                }
                var removeOption = (RemoveOption)Convert.ToInt32(m_UseStacks.boolValue);
                restoreShowMixedValue = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = m_Add.hasMultipleDifferentValues;
                value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(m_UseStacks.boolValue ? k_RemoveOptionStackSize - k_StacksSize - k_Padding : k_RemoveOptionAllSize, offset.height)), removeOption));
                if (value != m_UseStacks.boolValue)
                    m_UseStacks.boolValue = value;
                EditorGUI.showMixedValue = restoreShowMixedValue;
                offset.x += m_UseStacks.boolValue ? k_RemoveOptionStackSize - k_StacksSize : k_RemoveOptionAllSize + k_Padding;
                EditorGUI.PropertyField(new Rect(offset.position, new Vector2(k_ConfigurableSize, offset.height)), m_ActionConfigurable, GUIContent.none);
                offset.x += k_ConfigurableSize + k_Padding;
            }
            else
            {
                if (!CheckForSpace(k_AddOptionSize + k_ExpandWindowSize))
                    return;
                EditorGUI.PropertyField(new Rect(offset.position, new Vector2(k_StacksSize, offset.height)), m_Stacks, GUIContent.none);
                offset.x += k_StacksSize + k_Padding;
                EditorGUI.LabelField(new Rect(offset.position, new Vector2(k_AddOptionSize - k_StacksSize - k_Padding, offset.height)), "stacks");
                offset.x += k_AddOptionSize - k_StacksSize;
                currentWidth += k_AddOptionSize + k_Padding;

                if (!CheckForSpace(offset.size.x + k_Padding + (m_Timing.enumValueIndex == (int)ConditionalTiming.Duration ? k_SecondsPropertySize + k_Padding + k_TimingSize : k_TimingSize) - 1))
                    return;
            }

            EditorGUI.PropertyField(offset, m_ActionReference, GUIContent.none);

            if (m_Add.boolValue)
            {
                offset.x += offset.width + k_Padding;

                if (m_Timing.enumValueIndex == (int)ConditionalTiming.Duration)
                {
                    EditorGUI.PropertyField(new Rect(offset.position, new Vector2(k_SecondsPropertySize, offset.height)), m_Duration, GUIContent.none);
                    offset.x += k_SecondsPropertySize + k_Padding;
                }

                EditorGUI.PropertyField(new Rect(offset.position, new Vector2(k_TimingSize, offset.height)), m_Timing, GUIContent.none);
            }

            EditorGUI.EndProperty();

            bool CheckForSpace(float roomNeeded, bool debug = false)
            {
                if (currentWidth + roomNeeded > positionWidth)
                {
                    GUI.color = Color.yellow;
                    EditorGUI.LabelField(new Rect(offset.position, new Vector2(positionWidth - currentWidth, offset.height)), "(Expand window...)");
                    GUI.color = Color.white;
                    return false;
                }
                return true;
            }
        }
#endif

        public enum Existence
        {
            Inactive,
            Active
        }

        public enum Configurability
        {
            Remove,
            Add
        }

        public enum RemoveOption
        {
            All,
            Stacks,
        }

        public enum ScaleOption
        {
            Unscaled,
            Scaled
        }
    }
}
