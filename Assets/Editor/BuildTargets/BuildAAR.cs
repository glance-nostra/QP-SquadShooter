using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;

namespace nostra.platform.build
{
    public class BuildAAR
    {
        public static void ExportAndroidAAR()
        {
            try
            {
                // Check for development build flag in command line arguments
                bool isDevelopmentBuild = false;
                string[] args = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower() == "-dev" || args[i].ToLower() == "--dev")
                    {
                        isDevelopmentBuild = true;
                        Debug.Log("Development build flag detected. Building with development options enabled.");
                        break;
                    }
                }
                
                Debug.Log($"Starting Android AAR export{(isDevelopmentBuild ? " (Development Build)" : "")}...");
                
                // Set Android as the target platform
                EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

                // Switch to Android platform before building Addressables
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

                // Configure Android build settings
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                
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
                
                // Set the export path
                string exportPath = Path.Combine("Exports", "Android");
                Directory.CreateDirectory(exportPath);  // Ensure directory exists
                
                Debug.Log($"Export path: {Path.GetFullPath(exportPath)}");
                
                // Get all enabled scenes from build settings
                var scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();
                    
                Debug.Log($"Building with {scenes.Length} scenes");
                
                // Build the Android Library with appropriate options
                BuildOptions options = BuildOptions.None;
                
                // Apply development settings if enabled (both Editor settings and build options)
                if (isDevelopmentBuild)
                {
                    Debug.Log("Enabling development build settings:");
                    
                    // Enable development mode settings in Unity Editor (persists between builds)
                    EditorUserBuildSettings.development = true;
                    EditorUserBuildSettings.connectProfiler = true;
                    
                    // Enable development options for this specific build
                    options |= BuildOptions.Development;
                    options |= BuildOptions.ConnectWithProfiler;
                    options |= BuildOptions.EnableDeepProfilingSupport;
                }
                
                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = exportPath,
                    target = BuildTarget.Android,
                    options = options
                };

                Debug.Log("Starting build...");
                BuildPipeline.BuildPlayer(buildPlayerOptions);
                Debug.Log("Build completed successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during AAR export: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
                EditorApplication.Exit(1);  // Exit with error code
            }
        }
    }
}