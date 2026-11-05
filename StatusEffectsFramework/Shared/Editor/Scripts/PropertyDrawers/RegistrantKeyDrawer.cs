using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace StatusEffectsFramework.Editor
{
    /// <summary>
    /// Draws a <see cref="RegistrantKey{T}"/> as a text field for the key with an object field's picker on its right.
    /// Picking or dragging in an asset swaps the text field for a normal object field until the asset is set back to None.
    /// </summary>
    [CustomPropertyDrawer(typeof(RegistrantKey<>))]
    internal class RegistrantKeyDrawer : PropertyDrawer
    {
        internal const string KeyPropertyName = "m_" + nameof(RegistrantKey<Registrant>.Key);
        internal const string AssetPropertyName = "m_" + nameof(RegistrantKey<Registrant>.Asset);

        /// <summary>
        /// Main USS class name of the RegistrantKey.
        /// </summary>
        public const string ussClassName = "sef-registrant-key";

        /// <summary>
        /// USS class name of the RegistrantKey's object field.
        /// </summary>
        public const string objectFieldUssClassName = ussClassName + "__object-field";

        /// <summary>
        /// USS class name of the RegistrantKey's key field.
        /// </summary>
        public const string keyFieldUssClassName = ussClassName + "__key-field";

        private const string k_Placeholder = "Unique key";

        /// <summary>
        /// Whether a <see cref="RegistrantKey{T}"/> property has neither an asset nor a key.
        /// </summary>
        internal static bool IsEmpty(SerializedProperty property)
        {
            return property.FindPropertyRelative(AssetPropertyName).objectReferenceValue == null
                && string.IsNullOrEmpty(property.FindPropertyRelative(KeyPropertyName).stringValue);
        }

        /// <summary>
        /// Whether a prefab instance takes a <see cref="RegistrantKey{T}"/>'s key from its source prefab: it inherits the
        /// asset, and its key isn't overridden or the override was only baked. A key is baked from the assigned asset, which
        /// it isn't read from, or from <paramref name="previousAsset"/> when that asset was just reverted. A key typed over
        /// the source prefab's on purpose differs from the previous asset's, so it is kept.
        /// </summary>
        internal static bool InheritsKey(SerializedProperty keyProperty, SerializedProperty assetProperty, Object previousAsset)
        {
            if (assetProperty.serializedObject.isEditingMultipleObjects || !assetProperty.isInstantiatedPrefab || assetProperty.prefabOverride)
                return false;

            return !keyProperty.prefabOverride
                || assetProperty.objectReferenceValue != null
                || (previousAsset is Registrant registrant && keyProperty.stringValue == registrant.Key);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var keyProperty = property.FindPropertyRelative(KeyPropertyName);
            var assetProperty = property.FindPropertyRelative(AssetPropertyName);
            var registrantType = GetRegistrantType(fieldInfo.FieldType);

            var root = new VisualElement
            {
                name = ussClassName,
                tooltip = this.GetTooltip()
            };
            root.styleSheets.Add(StatusEffectsStyleSheet.instance.StyleSheet);
            root.AddToClassList(ussClassName);

            var objectField = new ObjectField(preferredLabel)
            {
                name = objectFieldUssClassName,
                objectType = registrantType,
                allowSceneObjects = false
            };
            objectField.AddToClassList(objectFieldUssClassName);
            objectField.AddToClassList(BaseField<Object>.alignedFieldUssClassName);
            objectField.BindProperty(assetProperty);
            root.Add(objectField);

            var keyField = new TextField
            {
                name = keyFieldUssClassName,
                isDelayed = true
            };
            keyField.textEdition.placeholder = k_Placeholder;
            keyField.AddToClassList(keyFieldUssClassName);
            keyField.BindProperty(keyProperty);

            // Without an asset the object field's display is swapped for the key's text field, so it reads
            // as a text field with the object field's own picker on the right. The object field draws the
            // box around both, so the text field's own box is hidden.
            var input = objectField.Q(className: ObjectField.inputUssClassName);
            var display = objectField.Q(className: ObjectField.objectUssClassName);
            input.Insert(0, keyField);

            // Packages such as EditorAttributes tint every field input's background inline after a delay, which
            // would stack the key's tint on top of the object field's and darken it. Inline styles win over the
            // style sheet, so it is cleared inline again afterwards.
            var keyInput = keyField.Q(className: TextField.inputUssClassName);
            keyInput?.schedule.Execute(() => keyInput.style.backgroundColor = StyleKeyword.Null)
                             .StartingIn(StatusEffectsStyleSheet.ExternalColorDelay);

            keyField.RegisterValueChangedCallback(changeEvent =>
            {
                // A stray space would make the key never match, so typed keys are trimmed.
                var trimmed = changeEvent.newValue?.Trim();
                if (trimmed == changeEvent.newValue)
                    return;

                keyProperty.stringValue = trimmed;
                keyProperty.serializedObject.ApplyModifiedProperties();
            });

            // The display handles assets being dragged in, but it is hidden while the key is shown.
            keyField.RegisterCallback<DragUpdatedEvent>(dragEvent =>
            {
                if (GetDraggedAsset(registrantType) == null)
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                dragEvent.StopPropagation();
            }, TrickleDown.TrickleDown);
            keyField.RegisterCallback<DragPerformEvent>(dragEvent =>
            {
                var asset = GetDraggedAsset(registrantType);
                if (asset == null)
                    return;

                DragAndDrop.AcceptDrag();
                objectField.value = asset;
                dragEvent.StopPropagation();
            }, TrickleDown.TrickleDown);

            // Reverting the asset on its own leaves the key that was baked from it overridden, and with no asset that key
            // would be used instead of the source prefab's. The IMGUI drawer reverts the whole field, so it needs no check.
            var previousAsset = assetProperty.objectReferenceValue;
            root.TrackPropertyValue(assetProperty, _ =>
            {
                if (keyProperty.prefabOverride && InheritsKey(keyProperty, assetProperty, previousAsset))
                    PrefabUtility.RevertPropertyOverride(keyProperty, InteractionMode.UserAction);

                previousAsset = assetProperty.objectReferenceValue;
                UpdateMode();
            });
            UpdateMode();

            return root;

            void UpdateMode()
            {
                // Mixed assets keep the object field so the mixed value shows.
                bool hasAsset = assetProperty.hasMultipleDifferentValues || assetProperty.objectReferenceValue != null;
                display.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, !hasAsset);
                keyField.EnableInClassList(StatusEffectsStyleSheet.HiddenClassName, hasAsset);
            }
        }

        // Same size as the object field's own picker.
        private const float k_PickerWidth = 19;

        private static readonly int s_PickerHint = "RegistrantKeyPicker".GetHashCode();
        private static readonly MethodInfo s_ShowObjectPicker = typeof(EditorGUIUtility).GetMethod(nameof(EditorGUIUtility.ShowObjectPicker));
        private static GUIStyle s_KeyFieldStyle;
        private static GUIStyle s_PickerStyle;

        private static GUIStyle KeyFieldStyle => s_KeyFieldStyle ??= new GUIStyle(EditorStyles.textField) { padding = { right = EditorStyles.textField.padding.right + (int)k_PickerWidth } };
        private static GUIStyle PickerStyle => s_PickerStyle ??= GUI.skin.FindStyle("ObjectFieldButton") ?? EditorStyles.miniButton;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var keyProperty = property.FindPropertyRelative(KeyPropertyName);
            var assetProperty = property.FindPropertyRelative(AssetPropertyName);
            var registrantType = GetRegistrantType(fieldInfo.FieldType);

            label = EditorGUI.BeginProperty(position, this.WithTooltip(label), property);

            if (assetProperty.hasMultipleDifferentValues || assetProperty.objectReferenceValue != null)
            {
                EditorGUI.showMixedValue = assetProperty.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                var asset = EditorGUI.ObjectField(position, label, assetProperty.objectReferenceValue, registrantType, false);
                if (EditorGUI.EndChangeCheck())
                    assetProperty.objectReferenceValue = asset;
                EditorGUI.showMixedValue = false;
            }
            else
            {
                int pickerId = GUIUtility.GetControlID(s_PickerHint, FocusType.Passive, position);
                position = EditorGUI.PrefixLabel(position, pickerId, label);
                var pickerPosition = PickerStyle.margin.Remove(new Rect(position.xMax - k_PickerWidth, position.y, k_PickerWidth, position.height));
                var current = Event.current;

                // Handled before the text field, which would otherwise take clicks on the picker.
                switch (current.type)
                {
                    case EventType.MouseDown when current.button == 0 && pickerPosition.Contains(current.mousePosition):
                        s_ShowObjectPicker.MakeGenericMethod(registrantType).Invoke(null, new object[] { null, false, string.Empty, pickerId });
                        current.Use();
                        break;
                    case EventType.ExecuteCommand when current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == pickerId:
                        assetProperty.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                        current.Use();
                        break;
                    case EventType.DragUpdated or EventType.DragPerform when position.Contains(current.mousePosition) && GetDraggedAsset(registrantType) is { } draggedAsset:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        if (current.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            assetProperty.objectReferenceValue = draggedAsset;
                        }
                        current.Use();
                        break;
                }

                EditorGUI.showMixedValue = keyProperty.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                var key = EditorGUI.DelayedTextField(position, keyProperty.stringValue, KeyFieldStyle);
                if (EditorGUI.EndChangeCheck())
                    keyProperty.stringValue = key.Trim();
                EditorGUI.showMixedValue = false;

                if (current.type == EventType.Repaint)
                    PickerStyle.Draw(pickerPosition, GUIContent.none, pickerPosition.Contains(current.mousePosition), false, false, false);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// The registrant type a <see cref="RegistrantKey{T}"/> field references.
        /// </summary>
        private static Type GetRegistrantType(Type fieldType)
        {
            // Elements of a list or array are drawn with the collection's field info.
            if (fieldType.IsArray)
                fieldType = fieldType.GetElementType();
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                fieldType = fieldType.GetGenericArguments()[0];

            return fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(RegistrantKey<>)
                 ? fieldType.GetGenericArguments()[0]
                 : typeof(Registrant);
        }

        private static Object GetDraggedAsset(Type registrantType)
        {
            return DragAndDrop.objectReferences.FirstOrDefault(dragged => registrantType.IsInstanceOfType(dragged) && EditorUtility.IsPersistent(dragged));
        }
    }
}
