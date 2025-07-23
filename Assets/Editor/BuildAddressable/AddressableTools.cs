using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class AddressableTools
{
    private static readonly string[] ValidBuildTargets = { "Android", "iOS", "WebGL", "StandaloneOSX", "StandaloneWindows64" };
    
    private struct BuildConfig
    {
        public string Profile;
        public string BuildTarget;
        public string GameName;
        public string ConfigFilePath;
    }

    private static void FixAddressableSettingsAsset(string configFilePath)
    {
        string assetPath = AssetDatabase.GetAssetPath(AddressableAssetSettingsDefaultObject.Settings);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("Could not find path to AddressableAssetSettings.asset");
            return;
        }

        if (!File.Exists(configFilePath))
        {
            Debug.LogError($"Config file not found at: {configFilePath}");
            return;
        }
        string jsonContent = File.ReadAllText(configFilePath);
        GameConfig gameConfig = JsonUtility.FromJson<GameConfig>(jsonContent);
        
        if (gameConfig == null)
        {
            Debug.LogError("Failed to parse game configuration file");
            return;
        }

        Debug.Log($"Fixing AddressableAssetSettings.asset at {assetPath}");

        string buildPathProduction = gameConfig.addressable_settings.build_paths.production;
        string buildPathStaging = gameConfig.addressable_settings.build_paths.staging;
        string loadPathProduction = gameConfig.addressable_settings.load_paths.production;
        string loadPathStaging = gameConfig.addressable_settings.load_paths.staging;

        buildPathProduction = buildPathProduction.Replace("[BuildTarget]", "");
        buildPathStaging = buildPathStaging.Replace("[BuildTarget]", "");
        // loadPathProduction = loadPathProduction.Replace("[BuildTarget]", "");
        // loadPathStaging = loadPathStaging.Replace("[BuildTarget]", "");

        
        string content = File.ReadAllText(assetPath);
        bool modified = false;

        // Update build paths
        content = RegexReplaceIfMatch(content, @"(m_Value:\s*')(ServerData/Production/QuickPlay/)(\[BuildTarget\]')", $"$1{buildPathProduction}$3", ref modified, "Production build path");
        content = RegexReplaceIfMatch(content, @"(m_Value:\s*')(ServerData/Staging/QuickPlay/)(\[BuildTarget\]')", $"$1{buildPathStaging}$3", ref modified, "Staging build path");

        // Update load paths
        content = RegexReplaceIfMatch(content, @"(m_Value:\s*')[^']*g-mob\.glance-cdn\.com[^']*(')", $"$1{loadPathProduction}$2", ref modified, "Production load path");
        content = RegexReplaceIfMatch(content, @"(m_Value:\s*')[^']*x-stg\.glance-cdn\.com[^']*(')", $"$1{loadPathStaging}$2", ref modified, "Staging load path");

        if (modified)
        {
            File.WriteAllText(assetPath, content);
            AssetDatabase.Refresh();
            Debug.Log("Successfully updated AddressableAssetSettings.asset");
        }
        else
        {
            Debug.Log("No changes needed to AddressableAssetSettings.asset");
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


    public static void BuildAddressables()
    {
        try
        {
            var config = ParseCommandLineArgs();
            var settings = GetAddressableSettings();
            
            Debug.Log($"Starting Addressable Build for game: {config.GameName}, profile: {config.Profile}, target: {config.BuildTarget}");

            // Fix the AddressableAssetSettings.asset file directly
            FixAddressableSettingsAsset(config.ConfigFilePath);
            
            // Reload settings after fixing the asset file
            settings = GetAddressableSettings();
            
            // Setup addressable groups based on config file if specified
            if (!string.IsNullOrEmpty(config.ConfigFilePath))
            {
                Debug.Log($"Using config file: {config.ConfigFilePath}");
                SetupAddressableGroups(settings, config.ConfigFilePath, config);
            }
            
            SetupBuildConfiguration(settings, config);
            
            ExecuteBuild();
            
            Debug.Log("Addressable Build Completed Successfully!");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Build failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }


    private static BuildConfig ParseCommandLineArgs()
    {
        var args = System.Environment.GetCommandLineArgs();
        var settings = GetAddressableSettings();
        
        // Get the active profile name from settings if available
        string defaultProfile = GetActiveProfileName(settings);
        if (string.IsNullOrEmpty(defaultProfile))
        {
            defaultProfile = "Default";
        }
        
        var config = new BuildConfig 
        { 
            Profile = GetArgValue(args, "-profile", defaultProfile),
            BuildTarget = GetArgValue(args, "-buildTarget", "Android"),
            GameName = GetArgValue(args, "-gameName", ""),
            ConfigFilePath = GetArgValue(args, "-configFile", "")
        };


        if (string.IsNullOrEmpty(config.GameName))
        {
            throw new System.ArgumentException("Game name must be specified using -gameName parameter");
        }

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

    private static void SetupBuildConfiguration(AddressableAssetSettings settings, BuildConfig config)
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
        //Get Remote Load Path for logging
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
    
    [System.Serializable]
    private class GameConfig
    {
        public string game_name;
        public string prefab_path;
        public AddressableSettings addressable_settings;
        
        [System.Serializable]
        public class AddressableSettings
        {
            public string group_name;
            public string game_address;
            public BuildPaths build_paths;
            public BuildPaths load_paths;
            
            [System.Serializable]
            public class BuildPaths
            {
                public string staging;
                public string production;
            }
        }
    }
    
    private static void SetupAddressableGroups(AddressableAssetSettings settings, string configFilePath, BuildConfig buildConfig)
    {
        if (!File.Exists(configFilePath))
        {
            Debug.LogError($"Config file not found at: {configFilePath}");
            return;
        }
        
        try
        {
            string jsonContent = File.ReadAllText(configFilePath);
            GameConfig gameConfig = JsonUtility.FromJson<GameConfig>(jsonContent);
            
            if (gameConfig == null)
            {
                Debug.LogError("Failed to parse game configuration file");
                return;
            }
            
            // Check if the prefab exists
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(gameConfig.prefab_path);
            if (prefab == null)
            {
                Debug.LogError($"Game prefab not found at path: {gameConfig.prefab_path}");
                return;
            }
            
            // Create or get the addressable group
            string groupName = gameConfig.addressable_settings.group_name;
            var group = settings.FindGroup(groupName);
            
            if (group == null)
            {
                Debug.Log($"Creating new addressable group: {groupName}");
                group = settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
            }
            else
            {
                Debug.Log($"Using existing addressable group: {groupName}");
                // Clear existing entries to avoid duplicates
                var existingEntries = new List<AddressableAssetEntry>(group.entries);
                foreach (var entryItem in existingEntries)
                {
                    group.RemoveAssetEntry(entryItem);
                }
            }
            
            // Add the prefab to the addressable group
            var prefabGUID = AssetDatabase.AssetPathToGUID(gameConfig.prefab_path);
            var entry = settings.CreateOrMoveEntry(prefabGUID, group);
            entry.address = gameConfig.addressable_settings.game_address;
            
            Debug.Log($"Added prefab to addressable group: {entry.address} (GUID: {prefabGUID})");
            
            // Save the settings
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
            
            Debug.Log("Addressable group setup completed successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting up addressable groups: {e.Message}\n{e.StackTrace}");
        }
    }
}
