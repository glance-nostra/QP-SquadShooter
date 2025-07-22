using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NostraTools.Editor
{
    public static class RemapDllUtils
    {

        private static string gameScriptsPath = "";
        private static bool isVerbose = false;
        private static bool shouldRemap = false;
        private static string gameName = "";
        public static void Main(){
            ParseCommandLineArguments();
            if (string.IsNullOrEmpty(gameScriptsPath))
            {
                Debug.LogError("No scripts path provided. Use -scriptsPath <path> to specify.");
                return;
            }
            if (string.IsNullOrEmpty(gameName))
            {
                Debug.LogError("No game name provided. Use -gameName <name> to specify.");
                return;
            }
            string dllAssemblyName = $"{gameName}.dll";
            RemapToDllAssembly(dllAssemblyName, gameScriptsPath);

        }

        private static void ParseCommandLineArguments()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-scriptsPath" && i + 1 < args.Length)
                {
                    gameScriptsPath = args[i + 1];
                    Debug.Log($"Scripts path: {gameScriptsPath}");
                }
                else if (args[i] == "-verbose")
                {
                    isVerbose = true;
                    Debug.Log("Verbose logging enabled");
                }
                else if (args[i] == "-remap")
                {
                    shouldRemap = true;
                    Debug.Log("Remapping enabled");
                }
                else if (args[i] == "-gameName" && i + 1 < args.Length)
                {
                    gameName = args[i + 1];
                    Debug.Log($"Game name: {gameName}");
                }
            }
        }

        public static void RemapToDllAssembly(string targetAssemblyName, string searchDirectory)
        {
            Debug.Log($"Starting MonoScript remapping for assembly: {targetAssemblyName} in {searchDirectory}");
            // Step 1: Build full map of MonoScripts (from all loaded)
            var allMonoScripts = Resources.FindObjectsOfTypeAll<MonoScript>()
                .Select(script =>
                {
                    string path = AssetDatabase.GetAssetPath(script);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    return (guid, script);
                })
                .Where(pair => !string.IsNullOrEmpty(pair.guid) && pair.script != null)
                .ToList();

            var sourceMap = new Dictionary<string, (string guid, long fileID)>();
            var dllMap = new Dictionary<string, (string guid, long fileID)>();

            foreach (var (guid, script) in allMonoScripts)
            {
                var serializedObject = new SerializedObject(script);
                var classNameProp = serializedObject.FindProperty("m_ClassName");
                var namespaceProp = serializedObject.FindProperty("m_Namespace");
                var assemblyProp = serializedObject.FindProperty("m_AssemblyName");

                if (classNameProp == null || assemblyProp == null)
                    continue;

                string className = classNameProp.stringValue;
                string namespaceName = namespaceProp?.stringValue;
                string assemblyName = assemblyProp.stringValue;
                long fileID = (long)Unsupported.GetLocalIdentifierInFileForPersistentObject(script);

                string fullName = string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}";

                if (assemblyName == targetAssemblyName)
                {
                    dllMap[fullName] = (guid, fileID);
                    Debug.Log($"[DLL] {fullName} => {guid}, {fileID}");
                }
                else
                {
                    sourceMap[fullName] = (guid, fileID);
                }
            }

            // Step 2: Build replacement map
            var replacementMap = new Dictionary<string, (string newGuid, long newFileID)>();
            foreach (var kvp in sourceMap)
            {
                if (dllMap.TryGetValue(kvp.Key, out var dllData))
                {
                    replacementMap[kvp.Value.guid] = dllData;
                    Debug.Log($"[Remap] {kvp.Value.guid} -> {dllData.guid} | {kvp.Value.fileID} -> {dllData.fileID}");
                }
            }

            // Step 3: Patch assets (prefabs, scenes, .asset)
            var regex = new Regex(@"m_Script: \{fileID: (?<fileID>-?\d+), guid: (?<guid>[a-f0-9]+), type: 3\}");
            var assetFiles = Directory.GetFiles(searchDirectory, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".prefab") || f.EndsWith(".unity") || f.EndsWith(".asset"))
                .ToList();

            foreach (var file in assetFiles)
            {
                var lines = File.ReadAllLines(file).ToList();
                bool modified = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    var match = regex.Match(lines[i]);
                    if (match.Success)
                    {
                        string oldGuid = match.Groups["guid"].Value;
                        if (replacementMap.TryGetValue(oldGuid, out var newRef))
                        {
                            lines[i] = $"  m_Script: {{fileID: {newRef.newFileID}, guid: {newRef.newGuid}, type: 3}}";
                            modified = true;
                            Debug.Log($"[Patched] {file} => line {i + 1}");
                        }
                    }
                }

                if (modified)
                {
                    File.WriteAllLines(file, lines);
                    Debug.Log($"[Saved] {file}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log("✅ MonoScript remapping complete.");
        }

    }
}
