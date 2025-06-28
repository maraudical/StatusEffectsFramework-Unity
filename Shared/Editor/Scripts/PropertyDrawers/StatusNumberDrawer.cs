#if UNITY_2023_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusFloat))]
    [CustomPropertyDrawer(typeof(StatusInt))]
    internal class StatusNumberDrawer :
#if EDITOR_ATTRIBUTES
        EditorAttributes.Editor.PropertyDrawerBase
#else
        PropertyDrawer
#endif
    {
        private MethodInfo m_MethodInfo;

        private const string k_SignProtectedTooltip =
        "Toggles whether the value of this variable should limit " +
        "itself to being positive or negative. If the base value is " +
        "positive, the value will be prevented from going below 0. " +
        "If the base value is negative, the value will be prevented " +
        "from going above 0.";

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
            var signLabel = root.Q<Label>("sign-label");
            var signProtected = root.Q<PropertyField>("sign-protected");
            var value = root.Q<PropertyField>("value");

            var statusNameProperty = property.FindPropertyRelative($"m_{nameof(StatusFloat.StatusName)}");
            var baseValueProperty = property.FindPropertyRelative($"m_{nameof(StatusFloat.BaseValue)}");
            var valueProperty = property.FindPropertyRelative($"m_{nameof(StatusFloat.Value)}");
            var signProtectedProperty = property.FindPropertyRelative($"m_{nameof(StatusFloat.SignProtected)}");

            foldout.text = property.displayName;
            foldout.viewDataKey = property.propertyPath + "-foldout";
            foldout.RegisterValueChangedCallback(FoldoutChanged);

            bool isPlaying = EditorApplication.isPlaying;

            headerPropertyObject.SetEnabled(!isPlaying);
            headerPropertyValue.SetEnabled(!isPlaying);
            headerPropertyValue.BindProperty(isPlaying ? valueProperty : baseValueProperty);

            statusName.SetEnabled(!isPlaying);
            statusName.RegisterValueChangeCallback(StatusNameChanged);

            baseValue.RegisterValueChangeCallback(BaseValueChanged);
            
            valueLabel.text = valueProperty.displayName;
            valueLabel.style.unityFontStyleAndWeight = isPlaying ? FontStyle.Bold : FontStyle.Normal;

            signProtected.tooltip = k_SignProtectedTooltip;
            signProtected.RegisterValueChangeCallback(SignProtectedChanged);
            signProtected.RegisterCallbackOnce<GeometryChangedEvent>(SignProtectedGeometryChanged);

            value.BindProperty(isPlaying ? valueProperty : baseValueProperty);
            value.RegisterCallbackOnce<GeometryChangedEvent>(ValueGeometryChanged);

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
                        if (property.GetPropertyType() == typeof(StatusFloat))
                        {
                            StatusFloat pastedValue = new(0);
                            EditorJsonUtility.FromJsonOverwrite(EditorGUIUtility.systemCopyBuffer, pastedValue);
                            property.GetParent(target).SetValue(property.name, pastedValue);
                        }
                        else
                        {
                            StatusInt pastedValue = new(0);
                            EditorJsonUtility.FromJsonOverwrite(EditorGUIUtility.systemCopyBuffer, pastedValue);
                            property.GetParent(target).SetValue(property.name, pastedValue);
                        }
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

            void IgnoreExcept(VisualElement root, string exception)
            {
                if (root.name == exception)
                {
                    root.pickingMode = PickingMode.Position;
                    return;
                }

                root.pickingMode = PickingMode.Ignore;

                foreach (var child in root.Children())
                    IgnoreExcept(child, exception);
            }

            void SignProtectedGeometryChanged(GeometryChangedEvent changeEvent)
            {
                IgnoreExcept(signProtected, "unity-checkmark");
            }

            void ValueGeometryChanged(GeometryChangedEvent changeEvent)
            {
                var element = value.Q("unity-text-input");
                if (element != null)
                {
                    element.style.marginRight = 18;
                    var styleTranslate = element.style.translate;
                    var translate = styleTranslate.value;
                    translate.x = 18;
                    styleTranslate.value = translate;
                    element.style.translate = styleTranslate;
                }

                EvaluateProperties();
            }

            void BaseValueChanged(SerializedPropertyChangeEvent changeEvent)
            {
                if (EditorApplication.isPlaying)
                {
                    m_MethodInfo = property.GetPropertyType().GetMethod("BaseValueUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var statusVariable in property.serializedObject.targetObjects)
                        m_MethodInfo.Invoke(valueProperty.GetParent(statusVariable), null);
                }
                
                EvaluateSignLabel();
            }

            void SignProtectedChanged(SerializedPropertyChangeEvent changeEvent)
            {
                if (EditorApplication.isPlaying)
                {
                    m_MethodInfo = property.GetPropertyType().GetMethod("SignProtectedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var statusVariable in property.serializedObject.targetObjects)
                        m_MethodInfo.Invoke(valueProperty.GetParent(statusVariable), null);
                }
                
                EvaluateSignLabel();
            }

            void EvaluateSignLabel()
            {
                if (signProtectedProperty.boolValue || signProtectedProperty.hasMultipleDifferentValues)
                {
                    var sign = System.Convert.ToDouble(baseValueProperty.GetParent(baseValueProperty.serializedObject.targetObject).GetValue($"m_{nameof(StatusFloat.BaseValue)}")) >= 0;
                    Color color = signProtectedProperty.hasMultipleDifferentValues || baseValueProperty.hasMultipleDifferentValues ? Color.white : sign ? Color.green : Color.red;
                    signLabel.text = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>({(signProtectedProperty.hasMultipleDifferentValues || baseValueProperty.hasMultipleDifferentValues ? "?" : sign ? "+" : "-")})";
                }
                else
                {
                    signLabel.text = string.Empty;
                }
            }

            void FoldoutChanged(ChangeEvent<bool> changeEvent)
            {
                EvaluateProperties();
            }

            void StatusNameChanged(SerializedPropertyChangeEvent changeEvent)
            {
                EvaluateProperties();
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

                    errorIcon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    if (foldout.value)
                    {
                        headerPropertyObject.style.display = DisplayStyle.None;
                        headerPropertyValue.style.display = DisplayStyle.None;
                    }
                    else
                    {
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
        private SerializedProperty m_SignProtected;
        private SerializedProperty m_Value;

        private readonly float m_FieldSize = EditorGUIUtility.singleLineHeight;
        private readonly float m_Padding = EditorGUIUtility.standardVerticalSpacing;
        private const float k_TopFix = 0.035f;
        private const float k_HorizontalPadding = 3;
        private const int k_FieldCount = 4;
        private const int k_ToggleSize = 15;
        private const int k_SignSize = 20;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_StatusName = property.FindPropertyRelative($"m_{nameof(StatusFloat.StatusName)}");
            m_BaseValue = property.FindPropertyRelative($"m_{nameof(StatusFloat.BaseValue)}");
            m_SignProtected = property.FindPropertyRelative($"m_{nameof(StatusFloat.SignProtected)}");
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
                Rect  offset = new Rect(propertyPosition.x, propertyPosition.y, k_ToggleSize, propertyPosition.height);
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(offset, m_SignProtected, GUIContent.none);
                GUI.Label(offset, new GUIContent("", k_SignProtectedTooltip));
                if (EditorGUI.EndChangeCheck() && EditorApplication.isPlaying)
                {
                    m_MethodInfo = property.GetPropertyType().GetMethod("SignProtectedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var statusVariable in property.serializedObject.targetObjects)
                        m_MethodInfo.Invoke(m_Value.GetParent(statusVariable), null);
                }
                if (m_SignProtected.boolValue)
                {
                    offset = new Rect(propertyPosition.x - k_SignSize, propertyPosition.y, k_SignSize, propertyPosition.height);
                    bool sign = Convert.ToInt32(m_BaseValue.GetParent(m_BaseValue.serializedObject.targetObject).GetValue($"m_{nameof(StatusFloat.BaseValue)}")) >= 0;
                    GUI.color = m_SignProtected.hasMultipleDifferentValues || m_BaseValue.hasMultipleDifferentValues ? Color.white : sign ? Color.green : Color.red;
                    EditorGUI.LabelField(offset, $"({(m_SignProtected.hasMultipleDifferentValues || m_BaseValue.hasMultipleDifferentValues ? "?" : sign ? "+" : "-")})");
                    GUI.color = Color.white;
                }
                offset = new Rect(propertyPosition.x + k_ToggleSize + k_HorizontalPadding, propertyPosition.y, propertyPosition.width - k_ToggleSize - k_HorizontalPadding, propertyPosition.height);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(offset, EditorApplication.isPlaying ? m_Value : m_BaseValue, GUIContent.none);
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