using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace StatusEffectsFramework.Editor
{
    internal static class PropertyDrawerExtensions
    {
        /// <summary>
        /// The text of the <see cref="TooltipAttribute"/> on the field being drawn. A custom drawer doesn't
        /// get the field's tooltip applied, so drawers pass it on themselves.
        /// </summary>
        internal static string GetTooltip(this PropertyDrawer drawer)
        {
            return drawer.fieldInfo?.GetCustomAttribute<TooltipAttribute>()?.tooltip;
        }

        /// <summary>
        /// Returns <paramref name="label"/> with the field's tooltip, unless the label already has one.
        /// </summary>
        internal static GUIContent WithTooltip(this PropertyDrawer drawer, GUIContent label)
        {
            var tooltip = drawer.GetTooltip();
            if (string.IsNullOrEmpty(tooltip) || !string.IsNullOrEmpty(label.tooltip))
                return label;

            return new GUIContent(label) { tooltip = tooltip };
        }
    }
}
