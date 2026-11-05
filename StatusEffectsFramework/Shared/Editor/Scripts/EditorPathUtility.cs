using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffectsFramework.Editor
{
    /// <summary>
    /// The folder picker that path fields share.
    /// </summary>
    internal static class EditorPathUtility
    {
        /// <summary>
        /// A small folder button for the end of a path field. It opens a folder dialog at <paramref name="startFolder"/> and
        /// passes the selected folder, relative to the Assets folder, to <paramref name="onSelected"/>. Folders outside Assets
        /// are refused.
        /// </summary>
        internal static Button CreateFolderButton(Func<string> startFolder, Action<string> onSelected)
        {
            var button = new Button(EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D, () =>
            {
                var selected = EditorUtility.OpenFolderPanel("Select Path", startFolder(), string.Empty);
                if (string.IsNullOrWhiteSpace(selected))
                    return;
                if (!TryGetPathRelativeToAssets(selected, out var relativePath))
                {
                    EditorUtility.DisplayDialog("Invalid path", "Please select a folder within the Assets directory.\n", "Ok");
                    return;
                }
                onSelected(relativePath);
            });
            StyleIconButton(button);
            return button;
        }

        /// <summary>
        /// Sizes a button with only an icon to sit at the end of a field, like <see cref="CreateFolderButton"/>.
        /// </summary>
        internal static void StyleIconButton(Button button)
        {
            button.style.width = 19;
            button.style.marginLeft = -2;
            button.style.marginRight = 3;
            button.style.marginBottom = 1;
            button.style.marginTop = 1;
            button.style.paddingBottom = 0;
            button.style.paddingLeft = 0;
            button.style.paddingRight = 2;
            button.style.paddingTop = 0;
            var image = button.Q<Image>();
            image.style.position = Position.Absolute;
            image.style.width = Length.Percent(100);
            image.style.height = Length.Percent(100);
        }

        /// <summary>
        /// Turns a folder on disk into a path relative to the Assets folder, using forward slashes.
        /// Fails if the folder isn't inside Assets. The Assets folder itself gives an empty path.
        /// </summary>
        internal static bool TryGetPathRelativeToAssets(string absolutePath, out string relativePath)
        {
            relativePath = null;
            var assets = Application.dataPath.Replace('\\', '/').TrimEnd('/');
            var path = absolutePath.Replace('\\', '/').TrimEnd('/');

            if (path.Equals(assets, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = string.Empty;
                return true;
            }

            if (!path.StartsWith(assets + "/", StringComparison.OrdinalIgnoreCase))
                return false;

            relativePath = path.Substring(assets.Length + 1);
            return true;
        }
    }
}
