using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class BuildScript
{
    public static void PerformBuild()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string buildTargetArg = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-buildTarget" && i + 1 < args.Length)
            {
                buildTargetArg = args[i + 1];
                break;
            }
        }

        if (string.IsNullOrEmpty(buildTargetArg))
        {
            Debug.LogError("No build target specified.");
            return;
        }

        if (!System.Enum.TryParse(buildTargetArg, true, out BuildTarget target))
        {
            Debug.LogError("Invalid build target: " + buildTargetArg);
            return;
        }

        string outputPath = Path.Combine("Builds", buildTargetArg);

        string extension = target switch
        {
            BuildTarget.StandaloneWindows => ".exe",
            BuildTarget.StandaloneWindows64 => ".exe",
            BuildTarget.StandaloneOSX => ".app",
            BuildTarget.Android => ".apk",
            _ => ""
        };

        string outputFile = Path.Combine(outputPath, "GameBuild" + extension);

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = outputFile,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"✅ Build succeeded: {summary.totalSize / 1_000_000f:F2} MB");
        }
        else
        {
            Debug.LogError($"❌ Build failed: {summary.result}");
        }
    }
}
