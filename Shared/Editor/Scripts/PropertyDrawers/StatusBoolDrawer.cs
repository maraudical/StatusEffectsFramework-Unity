#if UNITY_2023_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusBool))]
    internal class StatusBoolDrawer :
#if EDITOR_ATTRIBUTES
        EditorAttributes.Editor.PropertyDrawerBase
#else
        PropertyDrawer
#endif
    {
        private MethodInfo m_MethodInfo;

#if UNITY_2023_1_OR_NEWER
        public VisualTreeAsset VisualTree;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var foldout = root.Q<Foldout>("foldout");
            var foldoutLabel = root.Q<Label>(className: "unity-foldout__text");
            var unityCheckmark = root.Q("unity-checkmark");
            var errorIcon = root.Q("error-icon");
            var headerPropertyObject = root.Q<PropertyField>("header-property-object");
            var headerPropertyValue = root.Q<PropertyField>("header-property-value");
            var statusName = root.Q<PropertyField>("status-name");
            var baseValue = root.Q<PropertyField>("base-value");
            var valueLabel = root.Q<Label>("value-label");
            var value = root.Q<PropertyField>("value");

            var statusNameProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.StatusName)}");
            var baseValueProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.BaseValue)}");
            var valueProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.Value)}");

            foldout.text = property.displayName;
            foldout.viewDataKey = property.propertyPath + "-foldout";
            foldout.RegisterValueChangedCallback(FoldoutChanged);

            bool isPlaying = EditorApplication.isPlaying;

            headerPropertyObject.SetEnabled(!isPlaying);
            headerPropertyValue.SetEnabled(!isPlaying);
            headerPropertyValue.BindProperty(isPlaying ? valueProperty : baseValueProperty);

            statusName.SetEnabled(!isPlaying);
            statusName.RegisterValueChangeCallback(StatusNameChanged);

            if (isPlaying)
                baseValue.RegisterValueChangeCallback(BaseValueChanged);

            valueLabel.text = valueProperty.displayName;
            valueLabel.style.unityFontStyleAndWeight = isPlaying ? FontStyle.Bold : FontStyle.Normal;

            value.BindProperty(isPlaying ? valueProperty : baseValueProperty);
            value.RegisterCallbackOnce<GeometryChangedEvent>(GeometryChanged);

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
#if EDITOR_ATTRIBUTES
            
            ExecuteLater(root, () =>
            {
                EvaluateProperties();
            }, 50);
#endif

            return root;

            void GeometryChanged(GeometryChangedEvent changeEvent)
            {
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
                        unityCheckmark.RemoveFromClassList("error-icon");
                        headerPropertyObject.style.display = DisplayStyle.None;
                        headerPropertyValue.style.display = DisplayStyle.None;
#if EDITOR_ATTRIBUTES
                        unityCheckmark.style.unityBackgroundImageTintColor = valueLabel.style.color.keyword is not StyleKeyword.Null ? valueLabel.style.color : Color.white;
#endif
                    }
                    else
                    {
                        unityCheckmark.AddToClassList("error-icon");
                        headerPropertyObject.style.display = DisplayStyle.Flex;
                        headerPropertyValue.style.display = DisplayStyle.None;
#if EDITOR_ATTRIBUTES
                        unityCheckmark.style.unityBackgroundImageTintColor = Color.white;
#endif
                    }

                    foldout.RemoveFromClassList("standard-field-size");

                    errorIcon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    if (foldout.value)
                    {
                        foldout.RemoveFromClassList("standard-field-size");
                        headerPropertyObject.style.display = DisplayStyle.None;
                        headerPropertyValue.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        foldout.AddToClassList("standard-field-size");
                        headerPropertyObject.style.display = DisplayStyle.None;
                        headerPropertyValue.style.display = DisplayStyle.Flex;
                    }

                    unityCheckmark.RemoveFromClassList("error-icon");
#if EDITOR_ATTRIBUTES
                    unityCheckmark.style.unityBackgroundImageTintColor = valueLabel.style.color.keyword is not StyleKeyword.Null ? valueLabel.style.color : Color.white;
#endif

                    errorIcon.style.display = DisplayStyle.None;
                }
            }
        }

#endif
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