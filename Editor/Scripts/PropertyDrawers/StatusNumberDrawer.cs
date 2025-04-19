using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusFloat))]
    [CustomPropertyDrawer(typeof(StatusInt))]
    internal class StatusNumberDrawer : PropertyDrawer
    {
        public VisualTreeAsset VisualTree;

        private MethodInfo m_MethodInfo;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            VisualTree.CloneTree(root);

            var foldout = root.Q<Foldout>("foldout");
            var unityCheckmark = root.Q("unity-checkmark");
            var errorIcon = root.Q("error-icon");
            var headerProperty = root.Q<PropertyField>("header-property");
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

            headerProperty.SetEnabled(!isPlaying);

            statusName.SetEnabled(!isPlaying);
            statusName.RegisterValueChangeCallback(StatusNameChanged);

            baseValue.RegisterValueChangeCallback(BaseValueChanged);
            
            valueLabel.text = valueProperty.displayName;

            signProtected.RegisterValueChangeCallback(SignProtectedChanged);
            signProtected.RegisterCallbackOnce<GeometryChangedEvent>(SignProtectedGeometryChanged);

            value.BindProperty(isPlaying ? valueProperty : baseValueProperty);
            value.RegisterCallbackOnce<GeometryChangedEvent>(ValueGeometryChanged);
            
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
                        headerProperty.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        unityCheckmark.AddToClassList("error-icon");
                        headerProperty.style.display = DisplayStyle.Flex;
                    }
                    
                    headerProperty.BindProperty(statusNameProperty);

                    errorIcon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    if (foldout.value)
                        headerProperty.style.display = DisplayStyle.None;
                    else
                        headerProperty.style.display = DisplayStyle.Flex;

                    unityCheckmark.RemoveFromClassList("error-icon");

                    if (EditorApplication.isPlaying)
                        headerProperty.BindProperty(valueProperty);
                    else
                        headerProperty.BindProperty(baseValueProperty);

                    errorIcon.style.display = DisplayStyle.None;
                }
            }
        }
    }
}