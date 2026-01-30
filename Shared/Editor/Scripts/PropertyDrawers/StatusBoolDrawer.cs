using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Editor
{
    [CustomPropertyDrawer(typeof(StatusBool))]
    internal class StatusBoolDrawer : PropertyDrawer
    {
        private const string k_ValueTooltip = "The current value of this status variable. Will automatically update depending on status effects.";

        private MethodInfo m_MethodInfo;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var statusNameProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.StatusName)}");
            var baseValueProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.BaseValue)}");
            var valueProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.Value)}");

            bool isPlaying = EditorApplication.isPlaying;

            var foldout = new Foldout();
            foldout.text = property.displayName;
            foldout.value = false;
            foldout.viewDataKey = property.propertyPath + "-foldout";
            foldout.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
            foldout.AddToClassList(StatusEffectsStyleSheet.StandardFieldSizeClassName);

            var foldoutLabel = foldout.Q<Label>(className: Foldout.textUssClassName);
            var unityCheckmark = foldout.Q(className: Foldout.checkmarkUssClassName);

            var headerPropertyObject = new PropertyField(statusNameProperty, " ");
            headerPropertyObject.style.position = Position.Absolute;
            headerPropertyObject.style.left = Length.Percent(35);
            headerPropertyObject.style.right = 0;
            headerPropertyObject.style.top = 0;
            headerPropertyObject.style.bottom = 0;
            headerPropertyObject.SetEnabled(!isPlaying);
            var headerPropertyValue = new PropertyField(isPlaying ? valueProperty : baseValueProperty, " ");
            headerPropertyValue.style.position = Position.Absolute;
            headerPropertyValue.style.left = Length.Percent(35);
            headerPropertyValue.style.right = 0;
            headerPropertyValue.style.top = 0;
            headerPropertyValue.style.bottom = 0;
            headerPropertyValue.SetEnabled(!isPlaying);
            foldout.hierarchy.Add(headerPropertyObject);
            foldout.hierarchy.Add(headerPropertyValue);

            var errorIcon = new VisualElement();
            errorIcon.AddToClassList(StatusEffectsStyleSheet.ErrorIconClassName);
            errorIcon.style.position = Position.Absolute;
            errorIcon.style.left = -12;
            errorIcon.style.top = 4;
            errorIcon.style.width = 13;
            errorIcon.style.height = 13;
            foldout.Add(errorIcon);

            var statusName = new PropertyField(statusNameProperty);
            statusName.SetEnabled(!isPlaying);
            foldout.Add(statusName);

            var baseValue = new PropertyField(baseValueProperty);
            foldout.Add(baseValue);

            var valueContainer = new VisualElement();
            valueContainer.style.flexDirection = FlexDirection.Row;
            valueContainer.style.flexGrow = 1;
            valueContainer.style.flexShrink = 1;
            foldout.Add(valueContainer);

            var valueLabel = new Label(valueProperty.displayName);
            valueLabel.style.position = Position.Absolute;
            valueLabel.style.paddingTop = 1;
            valueLabel.style.paddingLeft = 4;
            valueContainer.Add(valueLabel);

            var value = new PropertyField(isPlaying ? valueProperty : baseValueProperty, " ");
            value.style.flexGrow = 1;
            value.style.flexShrink = 1;
            value.SetEnabled(false);
            value.tooltip = k_ValueTooltip;
            valueContainer.Add(value);

            foldout.RegisterValueChangedCallback(FoldoutChanged);
            
            statusName.RegisterValueChangeCallback(StatusNameChanged);

            if (isPlaying)
                baseValue.RegisterValueChangeCallback(BaseValueChanged);
            
            value.RegisterCallback<GeometryChangedEvent>(GeometryChanged);

            foldoutLabel.AddManipulator(new ContextualMenuManipulator((ContextualMenuPopulateEvent @event) =>
            {
                @event.menu.AppendAction("Copy Property Path", (action) => EditorGUIUtility.systemCopyBuffer = property.propertyPath);

                @event.menu.AppendSeparator();

                @event.menu.AppendAction("Copy", (action) => EditorGUIUtility.systemCopyBuffer = EditorJsonUtility.ToJson(property.boxedValue), property.hasMultipleDifferentValues ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                bool canParse = true;

                try
                {
                    EditorJsonUtility.FromJsonOverwrite(EditorGUIUtility.systemCopyBuffer, new());
                }
                catch
                {
                    canParse = false;
                }

                @event.menu.AppendAction("Paste", (action) =>
                {
                    foreach (var target in property.serializedObject.targetObjects)
                    {
                        StatusBool pastedValue = new(false);
                        EditorJsonUtility.FromJsonOverwrite(EditorGUIUtility.systemCopyBuffer, pastedValue);
                        property.GetParent(target).SetValue(property.name, pastedValue);
                        EditorUtility.SetDirty(target);
                        AssetDatabase.SaveAssetIfDirty(target);
                    }
                }, canParse ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                @event.menu.AppendSeparator();
            }));

            unityCheckmark.schedule.Execute((state) => EvaluateProperties()).StartingIn(50);

            return foldout;

            void GeometryChanged(GeometryChangedEvent changeEvent)
            {
                value.UnregisterCallback<GeometryChangedEvent>(GeometryChanged);

                EvaluateProperties();
            }

            void FoldoutChanged(ChangeEvent<bool> changeEvent)
            {
                EvaluateProperties();
            }

            void StatusNameChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
            }

            void BaseValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                m_MethodInfo = property.GetPropertyType().GetMethod("BaseValueUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var statusVariable in property.serializedObject.targetObjects)
                    m_MethodInfo.Invoke(valueProperty.GetParent(statusVariable), null);
            }

            void EvaluateProperties()
            {
                bool isNull = statusNameProperty.objectReferenceValue == null;

                if (isNull)
                {
                    if (foldout.value)
                    {
                        unityCheckmark.RemoveFromClassList(StatusEffectsStyleSheet.ErrorIconClassName);
                        headerPropertyObject.style.display = DisplayStyle.None;
                        headerPropertyValue.style.display = DisplayStyle.None;
                        unityCheckmark.style.unityBackgroundImageTintColor = valueLabel.style.color.keyword is not StyleKeyword.Null ? valueLabel.style.color : Color.white;
                    }
                    else
                    {
                        unityCheckmark.AddToClassList(StatusEffectsStyleSheet.ErrorIconClassName);
                        headerPropertyObject.style.display = DisplayStyle.Flex;
                        headerPropertyValue.style.display = DisplayStyle.None;
                        unityCheckmark.style.unityBackgroundImageTintColor = Color.white;
                    }

                    foldout.RemoveFromClassList(StatusEffectsStyleSheet.StandardFieldSizeClassName);

                    errorIcon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    if (foldout.value)
                    {
                        foldout.RemoveFromClassList(StatusEffectsStyleSheet.StandardFieldSizeClassName);
                        headerPropertyObject.style.display = DisplayStyle.None;
                        headerPropertyValue.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        foldout.AddToClassList(StatusEffectsStyleSheet.StandardFieldSizeClassName);
                        headerPropertyObject.style.display = DisplayStyle.None;
                        headerPropertyValue.style.display = DisplayStyle.Flex;
                    }

                    unityCheckmark.RemoveFromClassList(StatusEffectsStyleSheet.ErrorIconClassName);
                    unityCheckmark.style.unityBackgroundImageTintColor = valueLabel.style.color.keyword is not StyleKeyword.Null ? valueLabel.style.color : Color.white;

                    errorIcon.style.display = DisplayStyle.None;
                }
            }
        }

        private SerializedProperty m_StatusName;
        private SerializedProperty m_BaseValue;
        private SerializedProperty m_Value;

        private readonly float m_FieldSize = EditorGUIUtility.singleLineHeight;
        private readonly float m_Padding = EditorGUIUtility.standardVerticalSpacing;
        private const float k_TopFix = 0.035f;
        private const int k_FieldCount = 4;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_StatusName = property.FindPropertyRelative($"m_{nameof(StatusFloat.StatusName)}");
            m_BaseValue = property.FindPropertyRelative($"m_{nameof(StatusFloat.BaseValue)}");
            m_Value = property.FindPropertyRelative($"m_{nameof(StatusFloat.Value)}");

            position.height = m_FieldSize;
            position.y -= k_TopFix;
            float width = position.width;
            position.width = property.isExpanded ? width : EditorGUIUtility.labelWidth;

            GUI.color = !m_StatusName.objectReferenceValue && !property.isExpanded ? Color.red : Color.white;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            GUI.color = Color.white;

            position.width = width;

            EditorGUI.BeginProperty(position, label, property);

            int indent = EditorGUI.indentLevel;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel = indent + 1;
                position.y += m_FieldSize + m_Padding;
                GUI.color = !m_StatusName.objectReferenceValue ? Color.red : Color.white;
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                EditorGUI.PropertyField(position, m_StatusName);
                EditorGUI.EndDisabledGroup();
                GUI.color = Color.white;
                position.y += m_FieldSize + m_Padding;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, m_BaseValue);
                if (EditorGUI.EndChangeCheck() && EditorApplication.isPlaying)
                {
                    m_MethodInfo = property.GetPropertyType().GetMethod("BaseValueUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var statusVariable in property.serializedObject.targetObjects)
                        m_MethodInfo.Invoke(m_Value.GetParent(statusVariable), null);
                }
                position.y += m_FieldSize + m_Padding;

                Rect propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(m_Value.displayName));
                EditorGUI.indentLevel = indent;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(propertyPosition, EditorApplication.isPlaying ? m_Value : m_BaseValue, GUIContent.none);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                Rect propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(" "));
                EditorGUI.PropertyField(propertyPosition, EditorApplication.isPlaying ? m_Value : m_BaseValue, GUIContent.none);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (m_FieldSize + m_Padding) * (property.isExpanded ? k_FieldCount : 1) - m_Padding;
        }
    }
}