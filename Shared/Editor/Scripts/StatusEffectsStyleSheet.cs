using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffects.Editor
{
    public class StatusEffectsStyleSheet : ScriptableSingleton<StatusEffectsStyleSheet>
    {
        public const string ErrorIconClassName = "error-icon";
        public const string MaskFieldSizeClassName = "mask-field-size";
        public const string StandardFieldSizeClassName = "standard-field-size";
        public const string BoxGroupClassName = "box-group";
        public const string DarkBoxGroupClassName = "dark-box-group";
        public const string HeaderTextColorClassName = "header-text-color";
        public const string LineColorClassName = "line-color";
        public const string BackgroundColorClassName = "background-color";

        public StyleSheet StyleSheet => m_StyleSheet;
        [SerializeField]
        private StyleSheet m_StyleSheet;

        public Texture2D StatusEffectDataIcon => m_StatusEffectDataIcon;
        [SerializeField]
        private Texture2D m_StatusEffectDataIcon;
    }
}