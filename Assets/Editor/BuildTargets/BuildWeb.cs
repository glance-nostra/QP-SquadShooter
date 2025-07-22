using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;

namespace nostra.platform.build
{
    public class BuildWeb
    {
        public static void ExportWebGL()
        {
            // Get the export path
            string exportPath = Path.Combine("Exports", "Web");
            string fullExportPath = Path.Combine(Application.dataPath, "..", exportPath);
            
            // Create directory if it doesn't exist
            Directory.CreateDirectory(fullExportPath);

            // Configure WebGL settings for production
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;  // Enable fallback for browsers without Gzip
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.memorySize = 512;

            // Production optimization settings
            PlayerSettings.stripUnusedMeshComponents = true;
            PlayerSettings.bakeCollisionMeshes = true;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.High);

            // Switch to WebGL platform before building Addressables
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            // Clean Addressables before building
            Debug.Log("Cleaning Addressables...");
            AddressableAssetSettings.CleanPlayerContent();

            // Build Addressables for WebGL
            Debug.Log("Building Addressables for WebGL...");
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError($"Addressables build error: {result.Error}");
                return;
            }

            // Get all enabled scenes from build settings
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("No scenes found in build settings! Please add at least one scene.");
                EditorUtility.DisplayDialog("Build Error", 
                    "No scenes found in build settings! Please add at least one scene.", "OK");
                return;
            }

            // Set build options for production
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = scenes;
            buildPlayerOptions.locationPathName = fullExportPath;
            buildPlayerOptions.target = BuildTarget.WebGL;
            buildPlayerOptions.options = BuildOptions.None; // Production build

            Debug.Log($"Building WebGL with {scenes.Length} scenes: {string.Join(", ", scenes)}");
            
            // Build the project
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }
    }
}
