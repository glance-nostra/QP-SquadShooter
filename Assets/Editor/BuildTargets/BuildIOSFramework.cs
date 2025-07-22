using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace nostra.platform.build
{
    public class BuildIOSFramework
    {
        public static void ExportIOSFramework()
        {
            try
            {
                Debug.Log("Starting iOS Framework export...");

                // Set iOS as the target platform
                EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.iOS;

                // Set the export path
                string exportPath = Path.Combine("Exports", "iOS");
                Directory.CreateDirectory(exportPath);  // Ensure directory exists

                Debug.Log($"Export path: {Path.GetFullPath(exportPath)}");

                // Get all enabled scenes from build settings
                var scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();

                Debug.Log($"Building with {scenes.Length} scenes");

                // Build the iOS Xcode project
                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = exportPath,
                    target = BuildTarget.iOS,
                    options = BuildOptions.None
                };

                Debug.Log("Starting build...");
                BuildPipeline.BuildPlayer(buildPlayerOptions);
                Debug.Log("Build completed successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during iOS Framework export: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
                EditorApplication.Exit(1);  // Exit with error code
            }
        }
    }
}
