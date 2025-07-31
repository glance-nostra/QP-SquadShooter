using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using System.Collections.Generic;

namespace nostra.platform.build
{
    public static class BuildTestingAPK
    {
        // Usage: -executeMethod BuildTestingAPK.BuildStandaloneTestingAPK -buildTarget Android -gameName MyGame -logFile ... -outputAPK ...
        public static void BuildStandaloneTestingAPK()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            string gameName = GetArg(args, "-gameName") ?? "Game";
            string gameAddress = GetArg(args, "-gameAddress") ?? "default_address";
            string catalogUrl = GetArg(args, "-catalogUrl") ?? "https://example.com/catalog.json";
            string isLandscapeGame = (GetArg(args, "-isLandscapeGame")?.ToLower() == "true") ? "1" : "0";
            string outputAPK = GetArg(args, "-outputAPK") ?? "Builds/Android/app.apk";

            Debug.Log($"[BuildTestingAPK] Building Android APK for {gameName}, isLandscapeGame: {isLandscapeGame}, gameAddress: {gameAddress}, catalogUrl: {catalogUrl}, output: {outputAPK}");

            UpdateQuickPlayControllerInScene(gameName, gameAddress, catalogUrl, isLandscapeGame);

            // // Clean Addressables before building
            // Debug.Log("Cleaning Addressables...");
            // AddressableAssetSettings.CleanPlayerContent();

            // // Build Addressables for Android
            // Debug.Log("Building Addressables for Android...");
            // AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            // if (!string.IsNullOrEmpty(result.Error))
            // {
            //     Debug.LogError($"Addressables build error: {result.Error}");
            //     EditorApplication.Exit(1);
            //     return;
            // }

            // Set build options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = GetEnabledScenes();
            buildPlayerOptions.locationPathName = outputAPK;
            buildPlayerOptions.target = BuildTarget.Android;
            buildPlayerOptions.options = BuildOptions.None;

            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputAPK);
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            // Build
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildTestingAPK] Android APK build failed: {report.summary.result}");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log($"[BuildTestingAPK] Android APK build succeeded: {outputAPK}");
                EditorApplication.Exit(0);
            }
        }

        private static void UpdateQuickPlayControllerInScene(string gameName, string gameAddress, string catalogUrl, string isLandscapeGame)
        {
            // --- Patch QuickPlay.unity ---
            string scenePath = "Assets/Scenes/QuickPlay.unity";
            if (File.Exists(scenePath))
            {
                var lines = File.ReadAllLines(scenePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("m_quickPlayType:"))
                        lines[i] = "  m_quickPlayType: 2";
                    if (lines[i].Contains("m_testGamePosts:"))
                    {
                        // Expecting the next lines to be the first element
                        if (i + 1 < lines.Length && lines[i + 1].TrimStart().StartsWith("- name:"))
                            lines[i + 1] = $"  - name: {gameName}";
                        if (i + 2 < lines.Length && lines[i + 2].TrimStart().StartsWith("addressablePath:"))
                            lines[i + 2] = $"    addressablePath: {gameAddress}";
                        if (i + 3 < lines.Length && lines[i + 3].TrimStart().StartsWith("catalogUrl:"))
                            lines[i + 3] = $"    catalogUrl: {catalogUrl}";
                        // Check if line 4 (i+4) is "isLandscapeGame:"
                        if (i + 4 < lines.Length && lines[i + 4].TrimStart().StartsWith("isLandscapeGame:"))
                        {
                            lines[i + 4] = $"    isLandscapeGame: {isLandscapeGame}";
                        }
                        else
                        {
                            // Insert the line if it's missing
                            var updatedLines = new List<string>(lines);
                            updatedLines.Insert(i + 4, $"    isLandscapeGame: {isLandscapeGame}");
                            lines = updatedLines.ToArray();
                        }
                    }
                }
                File.WriteAllLines(scenePath, lines);
                Debug.Log("[BuildTestingAPK] QuickPlay.unity updated for test build.");
            }
            else
            {
                Debug.LogWarning("[BuildTestingAPK] QuickPlay.unity not found, skipping scene patch.");
            }
            // --- End patch ---
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
        }

        private static string GetArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && i + 1 < args.Length)
                    return args[i + 1];
            }
            return null;
        }
    }
}