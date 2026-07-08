// DefineBakerWindow.cs
// Place this (and DefinePreprocessor.cs, ExpressionEvaluator.cs) in an "Editor" folder.
//
// Workflow:
// 1. Write your master source file with normal C# #if / #elif / #else / #endif
//    blocks around the sample-specific code, plus shared "base" code outside
//    any #if block.
// 2. Open Tools > Samples > Define Baker, point it at the master file, and add
//    one row per (Define, Output Folder) pair you want baked.
// 3. Click "Bake All". Each row produces a clean file containing the base code
//    plus only the code for that define, with the #if/#elif/#else/#endif lines
//    themselves stripped out.
//
// Note: keep the master file itself out of Unity's normal compile path (e.g.
// name it "*.cs.txt" or put it in a folder ending in "~") so the live file and
// the baked copies don't both try to define the same types at once.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace StatusEffectsFramework.Editor
{
    internal class SamplesBakerWindow : EditorWindow
    {
        [Serializable]
        private class Entry
        {
            public string defineName = "";
            public string outputFolder = "Assets/Samples/";
            public string outputFileName = ""; // optional override; defaults to master file name
        }

        [Serializable]
        private class Config
        {
            public string masterFilePath = "";
            public List<Entry> entries = new List<Entry>();
        }

        private const string PrefsKey = "DefineBakerWindow.Config";

        private Config _config = new Config();
        private Vector2 _scroll;

        [MenuItem("Tools/Samples/Define Baker")]
        public static void Open()
        {
            var window = GetWindow<SamplesBakerWindow>("Samples Baker");
            window.Load();
        }

        private void OnEnable() => Load();
        private void OnDisable() => Save();

        private void Load()
        {
            string json = EditorPrefs.GetString(PrefsKey, "");
            _config = string.IsNullOrEmpty(json) ? new Config() : JsonUtility.FromJson<Config>(json);
            if (_config.entries == null)
                _config.entries = new List<Entry>();
        }

        private void Save()
        {
            EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(_config));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Master File", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _config.masterFilePath = EditorGUILayout.TextField(_config.masterFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string picked = EditorUtility.OpenFilePanel("Select master source file", Application.dataPath, "");
                if (!string.IsNullOrEmpty(picked))
                    _config.masterFilePath = MakeProjectRelative(picked);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Define -> Folder mappings", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            int removeIndex = -1;
            for (int i = 0; i < _config.entries.Count; i++)
            {
                var entry = _config.entries[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                entry.defineName = EditorGUILayout.TextField("Define", entry.defineName);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                entry.outputFolder = EditorGUILayout.TextField("Output Folder", entry.outputFolder);
                if (GUILayout.Button("Browse", GUILayout.Width(70)))
                {
                    string picked = EditorUtility.OpenFolderPanel("Select output folder", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(picked))
                        entry.outputFolder = MakeProjectRelative(picked);
                }
                EditorGUILayout.EndHorizontal();

                entry.outputFileName = EditorGUILayout.TextField("File Name (optional)", entry.outputFileName);

                if (GUILayout.Button("Bake This One"))
                    BakeEntry(entry);

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            if (removeIndex >= 0)
                _config.entries.RemoveAt(removeIndex);

            if (GUILayout.Button("+ Add Mapping"))
                _config.entries.Add(new Entry());

            EditorGUILayout.Space();
            if (GUILayout.Button("Bake All", GUILayout.Height(30)))
                BakeAll();
        }

        private void BakeAll()
        {
            foreach (var entry in _config.entries)
                BakeEntry(entry);
            AssetDatabase.Refresh();
        }

        private void BakeEntry(Entry entry)
        {
            if (string.IsNullOrEmpty(_config.masterFilePath) || !File.Exists(_config.masterFilePath))
            {
                Debug.LogError($"DefineBaker: master file not found at '{_config.masterFilePath}'.");
                return;
            }
            if (string.IsNullOrEmpty(entry.defineName))
            {
                Debug.LogError("DefineBaker: entry has no define name set.");
                return;
            }

            string source = File.ReadAllText(_config.masterFilePath);
            string resolved = SamplesPreprocessor.Resolve(source, new HashSet<string> { entry.defineName });

            string fileName = string.IsNullOrEmpty(entry.outputFileName)
                ? StripExtraExtensions(Path.GetFileName(_config.masterFilePath))
                : entry.outputFileName;

            Directory.CreateDirectory(entry.outputFolder);
            string outputPath = Path.Combine(entry.outputFolder, fileName).Replace('\\', '/');

            var sb = new StringBuilder();
            sb.AppendLine($"// Auto-generated by DefineBaker for define '{entry.defineName}'. Edit the master file, not this one.");
            sb.Append(resolved);

            File.WriteAllText(outputPath, sb.ToString());
            Debug.Log($"DefineBaker: baked '{entry.defineName}' -> {outputPath}");
        }

        // Turns "AllSamples.cs.txt" into "AllSamples.cs"
        private static string StripExtraExtensions(string fileName)
        {
            if (fileName.EndsWith(".cs.txt"))
                return fileName.Substring(0, fileName.Length - 4);
            if (!fileName.EndsWith(".cs"))
                return fileName + ".cs";
            return fileName;
        }

        private static string MakeProjectRelative(string absolutePath)
        {
            absolutePath = absolutePath.Replace('\\', '/');
            string projectPath = Application.dataPath.Replace('\\', '/');
            projectPath = projectPath.Substring(0, projectPath.Length - "Assets".Length);
            if (absolutePath.StartsWith(projectPath))
                return absolutePath.Substring(projectPath.Length);
            return absolutePath;
        }
    }
}