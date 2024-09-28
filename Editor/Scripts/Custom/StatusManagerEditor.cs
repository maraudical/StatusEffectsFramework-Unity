using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [CustomEditor(typeof(StatusManager))]
    [CanEditMultipleObjects]
    public class StatusManagerEditor : Editor
    {
        private ReorderableList m_EffectList;

        private SerializedProperty m_Effects;

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        private void OnEnable()
        {
            try { m_Effects = serializedObject.FindProperty("m_EditorOnlyEffects"); }
            catch { return; }


            m_EffectList = new ReorderableList(serializedObject, m_Effects, true, true, true, true);

            m_EffectList.displayAdd = false;
            m_EffectList.displayRemove = false;
            m_EffectList.draggable = false;

            m_EffectList.drawHeaderCallback = rect => {
                EditorGUI.LabelField(rect, "Status Effects");
            };
            m_EffectList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_EffectList.serializedProperty.GetArrayElementAtIndex(index);
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
            m_EffectList?.DoLayoutList();
        }
    }
}
