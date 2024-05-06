using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusManager))]
    [CanEditMultipleObjects]
    public class StatusManagerEditor : Editor
    {
        private ReorderableList _effectList;

        private SerializedProperty _effects;

        private void OnEnable()
        {
            _effects = serializedObject.FindProperty("_effects");

            _effectList = new ReorderableList(serializedObject, _effects, true, true, true, true);

            _effectList.displayAdd = false;
            _effectList.displayRemove = false;
            _effectList.draggable = false;

            _effectList.drawHeaderCallback = rect => {
                EditorGUI.LabelField(rect, "Status Effects");
            };
            _effectList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = _effectList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += EditorGUIUtility.standardVerticalSpacing / 1.515f;
                rect.height = EditorGUI.GetPropertyHeight(element);
                GUI.enabled = false;
                EditorGUI.PropertyField(rect, element, GUIContent.none);
                GUI.enabled = true;
            };
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(22);
            _effectList.DoLayoutList();
        }
    }
}
