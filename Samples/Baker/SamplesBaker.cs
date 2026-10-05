using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace StatusEffectsFramework.Editor
{
    // Bakes every folder in Samples/CombinedSamples into the package's Samples~ folder. Each folder other
    // than Shared becomes a standalone sample, and its name doubles as a scripting define (spaces become
    // underscores), so code wrapped in #if UniTask only ships in the UniTask sample. Shared is imported
    // alongside every sample, so Shared scripts that test a sample define are moved out of it and every
    // sample gets its own resolved copy instead.
    internal static class SamplesBaker
    {
        private const string SharedFolder = "Shared";

        // Samples built on top of other sample folders. Every other sample only bakes its own folder.
        private static readonly Dictionary<string, string[]> BaseFolders = new()
        {
            ["Netcode for GameObjects"] = new[] { "Default" },
            ["Netcode for Entities"] = new[] { "Entities" },
        };

        private class Script
        {
            public string Text;
            public bool Bom;
            public HashSet<string> Tests;                   // Sample defines it checks
            public HashSet<string> BakedWith = new();       // Sample defines that were on in some sample it was baked into
        }

        private class BakedFile
        {
            public string Source;
            public string FolderMetaRoot;   // Where folder .meta files come from, or null to let Unity generate them
            public string Content;          // Resolved script text, or null to copy the source as is
            public bool Bom;
        }

        [Serializable]
        private class PackageManifest
        {
            public SampleEntry[] samples = Array.Empty<SampleEntry>();
        }

        [Serializable]
        private class SampleEntry
        {
            public string path = "";
        }

        [MenuItem("Tools/Status Effects Framework/Bake Samples")]
        private static void Bake()
        {
            string bakerFolder = Path.GetDirectoryName(CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("StatusEffectsFramework.SamplesBaker"));
            string sourceRoot = Path.GetFullPath(Path.Combine(bakerFolder, "..", "CombinedSamples"));
            string packageRoot = Path.GetFullPath(Path.Combine(bakerFolder, "..", "..", "StatusEffectsFramework"));
            string outputRoot = Path.Combine(packageRoot, "Samples~");

            // Folders ending in ~ are hidden from this project but still get baked.
            var folders = Directory.GetDirectories(sourceRoot).ToDictionary(path => Path.GetFileName(path).TrimEnd('~'));
            if (!folders.TryGetValue(SharedFolder, out string sharedRoot))
            {
                Debug.LogError($"Samples Baker: there is no {SharedFolder} folder in {sourceRoot}.");
                return;
            }

            var samples = folders.Keys.Where(name => name != SharedFolder).OrderBy(name => name).ToList();
            var sampleDefines = samples.Select(ToDefine).ToHashSet();
            var outputs = new Dictionary<string, Dictionary<string, BakedFile>> { [SharedFolder] = new() };
            var scripts = new Dictionary<string, Script>();
            var errors = new HashSet<string>();

            foreach (string sample in samples.Where(sample => !Regex.IsMatch(ToDefine(sample), @"^[A-Za-z_]\w*$")))
                errors.Add($"'{sample}' can't be used as a scripting define. Rename the folder.");

            foreach (string file in Files(sharedRoot).Where(file => !IsSplit(file)))
                Add(SharedFolder, sharedRoot, file, null, true);

            foreach (string sample in samples)
            {
                var sources = (BaseFolders.TryGetValue(sample, out var bases) ? bases : Array.Empty<string>()).Append(sample).ToList();
                var enabled = sources.Select(ToDefine).ToHashSet();
                outputs[sample] = new();

                foreach (string source in sources)
                {
                    if (!folders.TryGetValue(source, out string root))
                    {
                        errors.Add($"{sample} is built on {source}, but there is no {source} folder in CombinedSamples.");
                        continue;
                    }
                    foreach (string file in Files(root))
                        Add(sample, root, file, enabled, true);
                }

                // Shared's folder .meta files are already in Shared, so let Unity generate new ones here.
                foreach (string file in Files(sharedRoot).Where(IsSplit))
                    Add(sample, sharedRoot, file, enabled, false);
            }

            if (errors.Count > 0)
            {
                foreach (string error in errors)
                    Debug.LogError($"Samples Baker: {error}");
                Debug.LogError("Samples Baker: nothing was written. Fix the errors above and bake again.");
                return;
            }

            foreach (var (file, script) in scripts)
            {
                var neverOn = script.Tests.Except(script.BakedWith).ToList();
                if (neverOn.Count > 0)
                    Debug.LogWarning($"Samples Baker: {Path.GetRelativePath(sourceRoot, file)} checks {string.Join(", ", neverOn)}, but no sample built from its folder defines that, so the code never ships.");
            }

            foreach (var (name, files) in outputs)
                Write(Path.Combine(outputRoot, name), files);

            foreach (string folder in Directory.GetDirectories(outputRoot).Select(Path.GetFileName).Where(folder => !outputs.ContainsKey(folder)))
                Debug.LogWarning($"Samples Baker: Samples~/{folder} doesn't match any folder in CombinedSamples. Delete it if it's stale.");

            var manifest = JsonUtility.FromJson<PackageManifest>(File.ReadAllText(Path.Combine(packageRoot, "package.json")));
            var listed = manifest.samples.Select(entry => entry.path).ToHashSet();
            foreach (string sample in samples.Where(sample => !listed.Contains($"Samples~/{sample}")))
                Debug.LogWarning($"Samples Baker: {sample} isn't listed in package.json, so the Package Manager won't offer it.");
            foreach (string path in listed.Where(path => !outputs.ContainsKey(path.Replace("Samples~/", ""))))
                Debug.LogWarning($"Samples Baker: package.json lists {path}, but nothing was baked there.");

            Debug.Log($"Samples Baker: baked {SharedFolder} and {samples.Count} samples ({string.Join(", ", samples)}) into {outputRoot}.");

            bool IsSplit(string file) => Load(file)?.Tests.Count > 0;

            Script Load(string file)
            {
                if (!file.EndsWith(".cs"))
                    return null;
                if (scripts.TryGetValue(file, out var script))
                    return script;

                byte[] bytes = File.ReadAllBytes(file);
                bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                string text = Encoding.UTF8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
                script = new Script { Text = text, Bom = bom, Tests = SamplesPreprocessor.DefinesIn(text).Where(sampleDefines.Contains).ToHashSet() };
                scripts[file] = script;
                return script;
            }

            void Add(string output, string root, string file, HashSet<string> enabled, bool copyFolderMetas)
            {
                var baked = new BakedFile { Source = file, FolderMetaRoot = copyFolderMetas ? root : null };
                if (IsSplit(file))
                {
                    var script = scripts[file];
                    script.BakedWith.UnionWith(enabled);
                    try
                    {
                        baked.Content = SamplesPreprocessor.Resolve(script.Text, sampleDefines, enabled);
                    }
                    catch (FormatException exception)
                    {
                        errors.Add($"{Path.GetRelativePath(sourceRoot, file)}: {exception.Message}");
                        return;
                    }
                    // Nothing left means the script only had code for other samples.
                    if (string.IsNullOrWhiteSpace(baked.Content))
                        return;
                    baked.Bom = script.Bom;
                }

                string relative = Path.GetRelativePath(root, file);
                if (!outputs[output].TryAdd(relative, baked))
                    errors.Add($"{output}: {relative} comes from more than one folder.");
            }
        }

        private static void Write(string destination, Dictionary<string, BakedFile> files)
        {
            if (Directory.Exists(destination))
                Directory.Delete(destination, true);
            Directory.CreateDirectory(destination);

            foreach (var (relative, file) in files)
            {
                string target = Path.Combine(destination, relative);
                CreateFolder(file.FolderMetaRoot, destination, Path.GetDirectoryName(relative));

                if (file.Content != null)
                    File.WriteAllText(target, file.Content, new UTF8Encoding(file.Bom));
                else
                    File.Copy(file.Source, target);

                // Keeping the .meta keeps the GUID, so scenes and prefabs still find the asset.
                if (File.Exists(file.Source + ".meta"))
                    File.Copy(file.Source + ".meta", target + ".meta");
            }
        }

        private static void CreateFolder(string metaRoot, string destination, string relative)
        {
            if (string.IsNullOrEmpty(relative) || Directory.Exists(Path.Combine(destination, relative)))
                return;

            CreateFolder(metaRoot, destination, Path.GetDirectoryName(relative));
            Directory.CreateDirectory(Path.Combine(destination, relative));

            string meta = metaRoot == null ? null : Path.Combine(metaRoot, relative) + ".meta";
            if (meta != null && File.Exists(meta))
                File.Copy(meta, Path.Combine(destination, relative) + ".meta");
        }

        private static IEnumerable<string> Files(string root) =>
            Directory.GetFiles(root, "*", SearchOption.AllDirectories).Where(file => !file.EndsWith(".meta")).OrderBy(file => file);

        private static string ToDefine(string sample) => sample.Replace(' ', '_');
    }
}
