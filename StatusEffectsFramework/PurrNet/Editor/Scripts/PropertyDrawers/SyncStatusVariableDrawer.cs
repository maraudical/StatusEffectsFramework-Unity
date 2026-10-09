using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffectsFramework.PurrNet.Editor
{
    /// <summary>
    /// PurrNet status variables wrap a regular status variable, so draw that one with its own drawer.
    /// </summary>
    [CustomPropertyDrawer(typeof(SyncStatusFloat))]
    [CustomPropertyDrawer(typeof(SyncStatusInt))]
    [CustomPropertyDrawer(typeof(SyncStatusBool))]
    internal class SyncStatusVariableDrawer : PropertyDrawer
    {
        private const string k_InnerName = "m_Inner";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new PropertyField(property.FindPropertyRelative(k_InnerName), property.displayName);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative(k_InnerName), label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_InnerName), label, true);
        }
    }
}
