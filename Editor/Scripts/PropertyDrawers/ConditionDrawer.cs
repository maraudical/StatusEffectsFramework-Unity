using System;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(Condition))]
    public class ConditionDrawer : PropertyDrawer
    {
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

        private Existence m_Existence;
        private Configurability m_Configurability;
        private RemoveOption m_RemoveOption;
        private ScaleOption m_ScaleOption;

        private float m_PositionWidth;

        private bool m_RestoreShowMixedValue;
        private bool m_Value;

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
            m_SearchableConfigurable = property.FindPropertyRelative(nameof(Condition.SearchableConfigurable));
            m_SearchableReference = property.FindPropertyRelative(m_SearchableConfigurable.enumValueIndex == (int)ConditionalConfigurable.Data ?
#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
                                                                                                                                                 nameof(Condition.SearchableDataReference)
#else
                                                                                                                                                 nameof(Condition.SearchableData)
#endif
                                                                : m_SearchableConfigurable.enumValueIndex == (int)ConditionalConfigurable.Name ? nameof(Condition.SearchableComparableName)
                                                                                                                                               : nameof(Condition.SearchableGroup));
            m_Exists = property.FindPropertyRelative(nameof(Condition.Exists));
            m_Add = property.FindPropertyRelative(nameof(Condition.Add));
            m_Scaled = property.FindPropertyRelative(nameof(Condition.Scaled));
            m_UseStacks = property.FindPropertyRelative(nameof(Condition.UseStacks));
            m_Stacks = property.FindPropertyRelative(nameof(Condition.Stacks));
            m_ActionConfigurable = property.FindPropertyRelative(nameof(Condition.ActionConfigurable));
            m_ActionReference = property.FindPropertyRelative(m_ActionConfigurable.enumValueIndex == (int)ConditionalConfigurable.Data || m_Add.boolValue ?
#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
                                                                                                                                                            nameof(Condition.ActionDataReference)
#else
                                                                                                                                                            nameof(Condition.ActionData)
#endif
                                                            : m_ActionConfigurable.enumValueIndex == (int)ConditionalConfigurable.Name                    ? nameof(Condition.ActionComparableName)
                                                                                                                                                          : nameof(Condition.ActionGroup));
            m_Duration = property.FindPropertyRelative(nameof(Condition.Duration));
            m_Timing = property.FindPropertyRelative(nameof(Condition.Timing));

            if (position.width > 0)
                m_PositionWidth = position.width;

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
            m_Existence = (Existence)Convert.ToInt32(m_Exists.boolValue);
            m_RestoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = m_Exists.hasMultipleDifferentValues;
            m_Value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(k_ExistsPropertySize, offset.height)), m_Existence));
            if (m_Value != m_Exists.boolValue)
                m_Exists.boolValue = m_Value;
            EditorGUI.showMixedValue = m_RestoreShowMixedValue;
            offset.x += k_ExistsPropertySize + k_Padding;
            currentWidth += k_ExistsPropertySize + k_Padding;

            if (!CheckForSpace(k_ThenSize + k_ExpandWindowSize))
                return;
            EditorGUI.LabelField(new Rect(offset.position, new Vector2(k_ThenSize, offset.height)), "then");
            offset.x += k_ThenSize + k_Padding;
            currentWidth += k_ThenSize + k_Padding;

            if (!CheckForSpace(k_AddRemoveSize + k_ExpandWindowSize))
                return;
            m_Configurability = (Configurability)Convert.ToInt32(m_Add.boolValue);
            m_RestoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = m_Add.hasMultipleDifferentValues;
            m_Value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(k_AddRemoveSize, offset.height)), m_Configurability));
            if (m_Value != m_Add.boolValue)
            {
                m_Add.boolValue = m_Value;
                if (m_Value)
                    m_UseStacks.boolValue = true;
            }
            EditorGUI.showMixedValue = m_RestoreShowMixedValue;
            offset.x += k_AddRemoveSize + k_Padding;
            currentWidth += k_AddRemoveSize + k_Padding;

            if (m_Add.hasMultipleDifferentValues)
            {
                if (!CheckForSpace(k_ExpandWindowSize + 20))
                    return;

                EditorGUI.LabelField(new Rect(offset.position, new Vector2(m_PositionWidth - currentWidth, offset.height)), "(Different add/remove)");
                return;
            }

            if (m_Add.boolValue || m_UseStacks.boolValue)
            {
                if (!CheckForSpace(k_ScaledSize + k_ExpandWindowSize))
                    return;
                m_ScaleOption = (ScaleOption)Convert.ToInt32(m_Scaled.boolValue);
                m_RestoreShowMixedValue = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = m_Scaled.hasMultipleDifferentValues;
                m_Value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(k_ScaledSize, offset.height)), m_ScaleOption));
                if (m_Value != m_Scaled.boolValue)
                    m_Scaled.boolValue = m_Value;
                EditorGUI.showMixedValue = m_RestoreShowMixedValue;
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
                m_RemoveOption = (RemoveOption)Convert.ToInt32(m_UseStacks.boolValue);
                m_RestoreShowMixedValue = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = m_Add.hasMultipleDifferentValues;
                m_Value = Convert.ToBoolean(EditorGUI.EnumPopup(new Rect(offset.position, new Vector2(m_UseStacks.boolValue ? k_RemoveOptionStackSize - k_StacksSize - k_Padding : k_RemoveOptionAllSize, offset.height)), m_RemoveOption));
                if (m_Value != m_UseStacks.boolValue)
                    m_UseStacks.boolValue = m_Value;
                EditorGUI.showMixedValue = m_RestoreShowMixedValue;
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
                if (currentWidth + roomNeeded > m_PositionWidth)
                {
                    GUI.color = Color.yellow;
                    EditorGUI.LabelField(new Rect(offset.position, new Vector2(m_PositionWidth - currentWidth, offset.height)), "(Expand window...)");
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
