using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace StatusEffectsFramework.Editor
{
    // The shared sample resources aren't listed in package.json, so the Package Manager never offers them.
    // Instead, whenever one of our samples is imported, they're copied out of Samples~ right next to it.
    internal class SharedSampleImporter : AssetPostprocessor
    {
        private const string SharedSourceFolder = "Samples~/Shared";
        private const string SharedImportName = "Shared Resources";

        private static bool s_ImportQueued;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Runs on every import, so bail out early unless something landed in Assets/Samples.
            if (s_ImportQueued || !importedAssets.Any(path => path.StartsWith("Assets/Samples/")))
                return;

            var package = PackageInfo.FindForAssembly(typeof(SharedSampleImporter).Assembly);
            if (package == null)
                return;

            var imported = Sample.FindByPackage(package.name, package.version).FirstOrDefault(sample => WasImported(sample, importedAssets));
            if (imported.displayName == null)
                return;

            // Samples are imported side by side into Assets/Samples/<Package>/<Version>/, so put Shared there too.
            string sharedPath = Path.Combine(Path.GetDirectoryName(imported.importPath), SharedImportName);
            if (Directory.Exists(sharedPath))
                return;

            // Copying more assets mid-import isn't safe, so wait until this import finishes.
            s_ImportQueued = true;
            EditorApplication.delayCall += () =>
            {
                s_ImportQueued = false;
                ImportShared(package, sharedPath);
            };
        }

        private static void ImportShared(PackageInfo package, string destination)
        {
            if (Directory.Exists(destination))
                return;

            string source = Path.Combine(package.resolvedPath, SharedSourceFolder);
            if (!Directory.Exists(source))
            {
                Debug.LogError($"Could not find shared sample resources at '{source}'.");
                return;
            }

            // The same asset GUIDs in two version folders would collide, so older copies have to go first.
            string samplesRoot = Path.GetDirectoryName(Path.GetDirectoryName(destination));
            var previous = Directory.GetDirectories(samplesRoot)
                .Select(version => Path.Combine(version, SharedImportName))
                .Where(Directory.Exists)
                .ToList();
            if (previous.Count > 0)
            {
                if (!EditorUtility.DisplayDialog(package.displayName, $"An older version of \"{SharedImportName}\" is already imported. Replace it with {package.version}?", "Replace", "Keep"))
                    return;
                foreach (var path in previous)
                    AssetDatabase.MoveAssetToTrash(FileUtil.GetProjectRelativePath(path.Replace('\\', '/')));
            }

            FileUtil.CopyFileOrDirectory(source, destination);
            AssetDatabase.Refresh();
        }

        private static bool WasImported(Sample sample, string[] importedAssets)
        {
            string root = FileUtil.GetProjectRelativePath(sample.importPath.Replace('\\', '/')) + "/";
            return importedAssets.Any(path => path.StartsWith(root));
        }
    }
}
