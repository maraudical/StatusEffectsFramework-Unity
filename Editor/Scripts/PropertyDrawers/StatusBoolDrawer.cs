using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffects.Inspector
{
    [CustomPropertyDrawer(typeof(StatusBool))]
    internal class StatusBoolDrawer : PropertyDrawer
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
            var value = root.Q<PropertyField>("value");

            var statusNameProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.StatusName)}");
            var baseValueProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.BaseValue)}");
            var valueProperty = property.FindPropertyRelative($"m_{nameof(StatusBool.Value)}");

            foldout.text = property.displayName;
            foldout.viewDataKey = property.propertyPath + "-foldout";
            foldout.RegisterValueChangedCallback(FoldoutChanged);

            bool isPlaying = EditorApplication.isPlaying;

            headerProperty.SetEnabled(!isPlaying);

            statusName.SetEnabled(!isPlaying);
            statusName.RegisterValueChangeCallback(StatusNameChanged);

            if (isPlaying)
                baseValue.RegisterValueChangeCallback(BaseValueChanged);

            valueLabel.text = valueProperty.displayName;
            valueLabel.style.unityFontStyleAndWeight = isPlaying ? FontStyle.Bold : FontStyle.Normal;

            value.BindProperty(isPlaying ? valueProperty : baseValueProperty);
            value.RegisterCallbackOnce<GeometryChangedEvent>(GeometryChanged);

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
                        headerProperty.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        unityCheckmark.AddToClassList("error-icon");
                        headerProperty.style.display = DisplayStyle.Flex;
                    }

                    foldout.RemoveFromClassList("standard-field-size");

                    headerProperty.BindProperty(statusNameProperty);

                    errorIcon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    if (foldout.value)
                    {
                        foldout.RemoveFromClassList("standard-field-size");
                        headerProperty.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        foldout.AddToClassList("standard-field-size");
                        headerProperty.style.display = DisplayStyle.Flex;
                    }

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