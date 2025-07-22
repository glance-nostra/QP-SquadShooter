using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Handles building addressables specifically for the QuickPlay platform (not games).
/// This is separate from game-specific addressable builds to avoid mixing platform and game logic.
/// </summary>
public class BuildPlatformAddressables
{
    private static readonly string[] ValidBuildTargets = { "Android", "iOS", "WebGL", "StandaloneOSX", "StandaloneWindows64" };
    
    private struct PlatformBuildConfig
    {
        public string Profile;
        public string BuildTarget;
        public string BuildPath;
        public string LoadPath;
    }


    /// <summary>
    /// Entry point for building platform addressables. Called from the build_platform_addressables.sh script.
    /// </summary>
    public static void Main()
    {
        try
        {
            var config = ParseCommandLineArgs();
            var settings = GetAddressableSettings();
            
            Debug.Log($"Starting Platform Addressable Build for profile: {config.Profile}, target: {config.BuildTarget}");

            // Fix the AddressableAssetSettings.asset file for platform builds with direct paths
            FixPlatformAddressableSettingsAsset(config);
            
            // Reload settings after fixing the asset file
            settings = GetAddressableSettings();
            
            SetupBuildConfiguration(settings, config);
            
            ExecuteBuild();
            
            Debug.Log("Platform Addressable Build Completed Successfully!");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Platform Addressable Build failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    private static PlatformBuildConfig ParseCommandLineArgs()
    {
        var args = System.Environment.GetCommandLineArgs();
        var settings = GetAddressableSettings();
        
        // Get the active profile name from settings if available
        string defaultProfile = GetActiveProfileName(settings);
        if (string.IsNullOrEmpty(defaultProfile))
        {
            defaultProfile = "Staging"; // Fallback to a default profile if none is set
        }
        
        var config = new PlatformBuildConfig 
        { 
            Profile = GetArgValue(args, "-profile", defaultProfile),
            BuildTarget = GetArgValue(args, "-buildTarget", "Android"),
            BuildPath = GetArgValue(args, "-buildPath", ""),
            LoadPath = GetArgValue(args, "-loadPath", "")
        };

        if (!ValidBuildTargets.Contains(config.BuildTarget))
        {
            throw new System.ArgumentException(
                $"Invalid build target: {config.BuildTarget}. Valid targets are: {string.Join(", ", ValidBuildTargets)}");
        }

        return config;
    }

    private static string GetActiveProfileName(AddressableAssetSettings settings)
    {
        if (settings == null)
        {
            return string.Empty;
        }
        
        string activeProfileId = settings.activeProfileId;
        if (string.IsNullOrEmpty(activeProfileId))
        {
            return string.Empty;
        }
        
        return settings.profileSettings.GetProfileName(activeProfileId);
    }

    private static string GetArgValue(string[] args, string key, string defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(key, System.StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return defaultValue;
    }

    private static AddressableAssetSettings GetAddressableSettings()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            throw new System.InvalidOperationException("Addressable Asset Settings not found!");
        }
        return settings;
    }

    private static void SetupBuildConfiguration(AddressableAssetSettings settings, PlatformBuildConfig config)
    {
        if (!SetActiveProfile(settings, config.Profile))
        {
            throw new System.InvalidOperationException($"Failed to set active profile: {config.Profile}");
        }

        VerifyRemoteBuildAndLoadPaths(settings);
    }

    private static bool SetActiveProfile(AddressableAssetSettings settings, string profileName)
    {
        var profiles = settings.profileSettings.GetAllProfileNames();
        if (profiles.Contains(profileName))
        {
            var profileId = settings.profileSettings.GetProfileId(profileName);
            settings.activeProfileId = profileId;
            return true;
        }
        return false;
    }

    private static void VerifyRemoteBuildAndLoadPaths(AddressableAssetSettings settings)
    {
        // Get Remote Load Path for logging
        var remoteLoadPathVarForLogging = settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kRemoteLoadPath);
        if (remoteLoadPathVarForLogging != null)
        {
            string currentLoadPath = settings.profileSettings.GetValueById(settings.activeProfileId, remoteLoadPathVarForLogging.Id);
            Debug.Log($"Current remote load path: {currentLoadPath}");
        }
        
        // Just verify the current build path for logging purposes
        var remoteBuildPathVar = settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kRemoteBuildPath);
        if (remoteBuildPathVar != null)
        {
            string currentBuildPath = settings.profileSettings.GetValueById(settings.activeProfileId, remoteBuildPathVar.Id);
            Debug.Log($"Current remote build path: {currentBuildPath}");
        }
    }

    private static void ExecuteBuild()
    {
        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent(out var result);
        
        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError($"Addressable build error: {result.Error}");
            throw new System.Exception($"Addressable build failed: {result.Error}");
        }
        
        Debug.Log("Addressable build completed successfully!");
    }

    private static void FixPlatformAddressableSettingsAsset(PlatformBuildConfig config)
    {
        string assetPath = AssetDatabase.GetAssetPath(AddressableAssetSettingsDefaultObject.Settings);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("Could not find path to AddressableAssetSettings.asset");
            return;
        }

        Debug.Log($"Fixing AddressableAssetSettings.asset at {assetPath} for platform build");

        string buildPath = config.BuildPath;
        string loadPath = config.LoadPath;

        // Remove [BuildTarget] from build path if needed
        buildPath = buildPath.Replace("[BuildTarget]", "");
        
        string content = File.ReadAllText(assetPath);
        bool modified = false;

        if(config.Profile == "Staging")
        {
            Debug.Log("Using staging profile for build paths");
            content = RegexReplaceIfMatch(content, @"(m_Value:\s*')(ServerData/Staging/[^']*/)(\[BuildTarget\]')", $"$1{buildPath}$3", ref modified, "Build path");
            content = RegexReplaceIfMatch(content, @"(m_Value:\s*')[^']*x-stg\.glance-cdn\.com[^']*(')", $"$1{loadPath}$2", ref modified, "Load path");
            
        }
        else if(config.Profile == "Production")
        {
            Debug.Log("Using production profile for build paths");
            content = RegexReplaceIfMatch(content, @"(m_Value:\s*')(ServerData/Production/[^']*/)(\[BuildTarget\]')", $"$1{buildPath}$3", ref modified, "Build path");
            content = RegexReplaceIfMatch(content, @"(m_Value:\s*')[^']*g-mob\.glance-cdn\.com[^']*(')", $"$1{loadPath}$2", ref modified, "Load path");

        }
        

        if (modified)
        {
            File.WriteAllText(assetPath, content);
            AssetDatabase.Refresh();
            Debug.Log("Successfully updated AddressableAssetSettings.asset for platform build");
        }
        else
        {
            Debug.Log("No changes needed to AddressableAssetSettings.asset for platform build");
        }
    }

    private static string RegexReplaceIfMatch(string input, string pattern, string replacement, ref bool modified, string desc)
    {
        Debug.Log($"Checking for pattern: {pattern} in {desc}");
        if (System.Text.RegularExpressions.Regex.IsMatch(input, pattern))
        {
            Debug.Log($"Modified {desc}");
            modified = true;
            return System.Text.RegularExpressions.Regex.Replace(input, pattern, replacement);
        } else {
            Debug.Log($"No match found for pattern: {pattern} in {desc}");
        }
        return input;
    }

}
