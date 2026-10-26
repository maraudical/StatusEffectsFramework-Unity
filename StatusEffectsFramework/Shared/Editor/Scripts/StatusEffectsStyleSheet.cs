using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffectsFramework.Editor
{
    public class StatusEffectsStyleSheet : ScriptableSingleton<StatusEffectsStyleSheet>
    {
        public const string HiddenClassName = "sef-hidden";
        public const string ErrorIconClassName = "sef-error-icon";
        public const string MaskFieldSizeClassName = "sef-mask-field-size";
        public const string StandardFieldSizeClassName = "sef-standard-field-size";
        public const string BoxGroupClassName = "sef-box-group";
        public const string DarkBoxGroupClassName = "sef-dark-box-group";
        public const string HeaderTextColorClassName = "sef-header-text-color";
        public const string LineColorClassName = "sef-line-color";
        public const string BackgroundColorClassName = "sef-background-color";
        public const string EmptyPropertyLabel = " ";

        public StyleSheet StyleSheet => m_StyleSheet;
        [SerializeField]
        private StyleSheet m_StyleSheet;

        public Texture2D StatusEffectDataIcon => m_StatusEffectDataIcon;
        [SerializeField]
        private Texture2D m_StatusEffectDataIcon;
    }
}