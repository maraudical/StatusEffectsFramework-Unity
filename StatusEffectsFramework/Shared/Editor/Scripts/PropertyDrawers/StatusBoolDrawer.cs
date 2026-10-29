using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace StatusEffectsFramework.Editor
{
    [CustomPropertyDrawer(typeof(StatusBool))]
    internal class StatusBoolDrawer : PropertyDrawer
    {
        /// <summary>
        /// Main USS class name of the StatusBool.
        /// </summary>
        public const string ussClassName = "sef-status-bool";

        /// <summary>
        /// USS class name of the status name field shown in the header while collapsed.
        /// </summary>
        public const string headerStatusNameUssClassName = ussClassName + "__header-status-name";

        /// <summary>
        /// USS class name of the value field shown in the header while collapsed.
        /// </summary>
        public const string headerValueUssClassName = ussClassName + "__header-value";

        /// <summary>
        /// USS class name of the error icon shown while the status name is empty.
        /// </summary>
        public const string errorIconUssClassName = ussClassName + "__error-icon";

        /// <summary>
        /// USS class name of the status name field.
        /// </summary>
        public const string statusNameUssClassName = ussClassName + "__status-name";

        /// <summary>
        /// USS class name of the base value field.
        /// </summary>
        public const string baseValueUssClassName = ussClassName + "__base-value";

        /// <summary>
        /// USS class name of the value container.
        /// </summary>
        public const string valueContainerUssClassName = ussClassName + "__value-container";

        /// <summary>
        /// USS class name of the value label.
        /// </summary>
        public const string valueLabelUssClassName = ussClassName + "__value-label";

        /// <summary>
        /// USS class name of the value field.
        /// </summary>
        public const string valueUssClassName = ussClassName + "__value";

        private const string k_ValueTooltip = "The current value of this status variable. Will automatically update depending on status effects.";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var statusNameProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.StatusName)}");
            var baseValueProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.BaseValue)}");
            var valueProperty = property.FindPropertyRelative($"<{nameof(StatusBool.PostEvaluationValue)}>k__BackingField");

            bool isPlaying = EditorApplication.isPlaying;

            var foldout = new Foldout
            {
                name = ussClassName,
                text = property.displayName,
                value = false,
                viewDataKey = property.propertyPath + "-foldout"
            };
            foldout.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
            foldout.AddToClassList(ussClassName);
            foldout.AddToClassList(StatusEffectsStyleSheet.StandardFieldSizeClassName);

            var foldoutLabel = foldout.Q<Label>(className: Foldout.textUssClassName);
            var unityCheckmark = foldout.Q(className: Foldout.checkmarkUssClassName);

            var headerStatusName = new PropertyField(statusNameProperty, StatusEffectsStyleSheet.EmptyPropertyLabel)
            {
                name = headerStatusNameUssClassName
            };
            headerStatusName.AddToClassList(headerStatusNameUssClassName);
            headerStatusName.SetEnabled(!isPlaying);
            foldout.hierarchy.Add(headerStatusName);

            var headerValue = new PropertyField(isPlaying ? valueProperty : baseValueProperty, StatusEffectsStyleSheet.EmptyPropertyLabel)
            {
                name = headerValueUssClassName
            };
            headerValue.AddToClassList(headerValueUssClassName);
            headerValue.SetEnabled(!isPlaying);
            foldout.hierarchy.Add(headerValue);

            var errorIcon = new VisualElement
            {
                name = errorIconUssClassName
            };
            errorIcon.AddToClassList(StatusEffectsStyleSheet.ErrorIconClassName);
            errorIcon.AddToClassList(errorIconUssClassName);
            foldout.Add(errorIcon);

            var statusName = new PropertyField(statusNameProperty)
            {
                name = statusNameUssClassName
            };
            statusName.AddToClassList(statusNameUssClassName);
            statusName.SetEnabled(!isPlaying);
            foldout.Add(statusName);

            var baseValue = new PropertyField(baseValueProperty)
            {
                name = baseValueUssClassName
            };
            baseValue.AddToClassList(baseValueUssClassName);
            foldout.Add(baseValue);

            var valueContainer = new VisualElement
            {
                name = valueContainerUssClassName
            };
            valueContainer.AddToClassList(valueContainerUssClassName);
            foldout.Add(valueContainer);

            var valueLabel = new Label("Value")
            {
                name = valueLabelUssClassName
            };
            valueLabel.AddToClassList(valueLabelUssClassName);
            valueContainer.Add(valueLabel);

            var value = new PropertyField(isPlaying ? valueProperty : baseValueProperty, StatusEffectsStyleSheet.EmptyPropertyLabel)
            {
                name = valueUssClassName,
                tooltip = k_ValueTooltip
            };
            value.AddToClassList(valueUssClassName);
            value.SetEnabled(false);
            valueContainer.Add(value);

            foldout.RegisterValueChangedCallback(FoldoutChanged);
            
            // The status name is picked or typed inside its own drawer, so its property is tracked directly.
            foldout.TrackPropertyValue(statusNameProperty, StatusNameChanged);

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

            // Waits for packages such as EditorAttributes to color the value label, which the checkmark's tint follows.
            unityCheckmark.schedule.Execute((state) => EvaluateProperties()).StartingIn(StatusEffectsStyleSheet.ExternalColorDelay);

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

            void StatusNameChanged(SerializedProperty _)
            {
                EvaluateProperties();
            }

            void BaseValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                baseValue.schedule.Execute(() => InvokeUpdateBaseValue(valueProperty));
            }

            void EvaluateProperties()
            {
                bool isNull = RegistrantKeyDrawer.IsEmpty(statusNameProperty);
                bool isCollapsed = !foldout.value;

                // Only the collapsed header with a value is limited to a single line.
                foldout.EnableInClassList(StatusEffectsStyleSheet.StandardFieldSizeClassName, !isNull && isCollapsed);

                headerStatusName.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !(isNull && isCollapsed));
                headerValue.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !(!isNull && isCollapsed));
                errorIcon.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !isNull);

                // The checkmark becomes the error icon while collapsed, which keeps its own colors.
                bool showErrorCheckmark = isNull && isCollapsed;
                unityCheckmark.EnableInClassList(StatusEffectsStyleSheet.ErrorIconClassName, showErrorCheckmark);
                unityCheckmark.style.unityBackgroundImageTintColor = !showErrorCheckmark && valueLabel.style.color.keyword is not StyleKeyword.Null
                                                                   ? valueLabel.style.color
                                                                   : Color.white;
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
            m_StatusName = property.FindPropertyRelative($"m_{nameof(StatusBool.StatusName)}");
            m_BaseValue = property.FindPropertyRelative($"m_{nameof(StatusBool.BaseValue)}");
            m_Value = property.FindPropertyRelative($"<{nameof(StatusBool.PostEvaluationValue)}>k__BackingField");

            bool isPlaying = EditorApplication.isPlaying;

            position.height = m_FieldSize;
            position.y -= k_TopFix;
            float width = position.width;
            position.width = property.isExpanded ? width : EditorGUIUtility.labelWidth;

            GUI.color = RegistrantKeyDrawer.IsEmpty(m_StatusName) && !property.isExpanded ? Color.red : Color.white;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            GUI.color = Color.white;

            position.width = width;

            EditorGUI.BeginProperty(position, label, property);

            int indent = EditorGUI.indentLevel;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel = indent + 1;
                position.y += m_FieldSize + m_Padding;
                GUI.color = RegistrantKeyDrawer.IsEmpty(m_StatusName) ? Color.red : Color.white;
                EditorGUI.BeginDisabledGroup(isPlaying);
                EditorGUI.PropertyField(position, m_StatusName);
                EditorGUI.EndDisabledGroup();
                GUI.color = Color.white;
                position.y += m_FieldSize + m_Padding;

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, m_BaseValue);
                if (EditorGUI.EndChangeCheck() && isPlaying)
                {
                    var valueProperty = m_Value.Copy();
                    EditorApplication.delayCall += () => InvokeUpdateBaseValue(valueProperty);
                }
                position.y += m_FieldSize + m_Padding;

                Rect propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(m_Value.displayName));
                EditorGUI.indentLevel = indent;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(propertyPosition, isPlaying ? m_Value : m_BaseValue, GUIContent.none);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(isPlaying);
                Rect propertyPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(StatusEffectsStyleSheet.EmptyPropertyLabel));
                EditorGUI.PropertyField(propertyPosition, isPlaying ? m_Value : m_BaseValue, GUIContent.none);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (m_FieldSize + m_Padding) * (property.isExpanded ? k_FieldCount : 1) - m_Padding;
        }

        private static void InvokeUpdateBaseValue(SerializedProperty valueProperty)
        {
            foreach (var target in valueProperty.serializedObject.targetObjects)
                if (valueProperty.GetParent(target) is StatusBool statusBool)
                    statusBool.InternalUpdateBaseValue();
        }
    }
}