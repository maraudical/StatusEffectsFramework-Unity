using StatusEffects.Modules;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Inspector
{
    // Script inspired by https://github.com/unity-atoms/unity-atoms/blob/master/Packages/Core/Editor/PostProcessors/EditorIconPostProcessor.cs
    public class ModuleIconPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[]movedAssets, string[] movedFromAssetPaths)
        {
            var metaChangedForAssets = new List<string>();
            foreach (string assetPath in importedAssets)
            {
                var metaPath = $"{assetPath}.meta";
                if (File.Exists(assetPath) && File.Exists(metaPath) && assetPath.EndsWith(".cs"))
                {
                    // Hack, hack, hack away....
                    string scriptText = File.ReadAllText(assetPath);

                    if (!scriptText.Contains("namespace StatusEffects.Modules"))
                        continue;

                    if (scriptText.Contains("ModuleIconPostProcessor : AssetPostprocessor"))
                        continue;

                    if (scriptText.Contains(": ModuleInstance"))
                        WriteIconToMeta(AssetDatabase.FindAssets("ModuleInstance l:Modules t:monoScript"));
                    else if (scriptText.Contains(": Module"))
                        WriteIconToMeta(AssetDatabase.FindAssets("Module l:Modules t:monoScript"));

                    void WriteIconToMeta(string[] monoScriptGuids)
                    {
                        var monoScriptGuidsList = monoScriptGuids.ToList();
                        var monoScriptGuid = monoScriptGuidsList.FirstOrDefault();
                        
                        if (!string.IsNullOrEmpty(monoScriptGuid))
                        {
                            var baseClassPath = AssetDatabase.GUIDToAssetPath(monoScriptGuid);
                            var baseClassMetaTextLines = File.ReadAllLines($"{baseClassPath}.meta");
                            string metaIconLine = default;
                            // Read meta for base class script
                            for (var i = 0; i < baseClassMetaTextLines.Length; ++i)
                            {
                                var line = baseClassMetaTextLines[i];
                                // Find base class icon line to copy
                                if (line.Contains("icon: "))
                                {
                                    metaIconLine = line;
                                    break;
                                }
                            }
                            // Read meta for script new script
                            var scriptMetaTextLines = File.ReadAllLines(metaPath);
                            for (var i = 0; i < scriptMetaTextLines.Length; ++i)
                            {
                                var line = scriptMetaTextLines[i];
                                // Find icon line
                                if (line.Contains("icon: ") && !line.Contains(metaIconLine))
                                {
                                    var indexIconKeyName = line.IndexOf("icon: ");
                                    var indexAfterClosingBrace = line.IndexOf("}", indexIconKeyName) + 1;
                                    var newLine = line.Replace(line.Substring(indexIconKeyName, indexAfterClosingBrace - indexIconKeyName), metaIconLine);
                                    scriptMetaTextLines[i] = newLine;
                                    File.WriteAllLines(metaPath, scriptMetaTextLines);
                                    metaChangedForAssets.Add(assetPath);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // We need to reimport all assets where the meta was changed
            if (metaChangedForAssets.Count > 0)
            {
                foreach (var assetPath in metaChangedForAssets)
                {
                    AssetDatabase.ImportAsset(assetPath);
                }
            }
        }
    }
}
