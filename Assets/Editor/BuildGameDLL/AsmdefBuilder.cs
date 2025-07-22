using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NostraTools.Editor
{
    /// <summary>
    /// Unity Editor tool to automatically generate Assembly Definition files for games
    /// This helps with creating DLLs for game integration into the platform
    /// </summary>
    public class AsmdefBuilder : EditorWindow
    {
        // Configuration
        private string gameName = "";
        private string gameScriptsPath = "Assets/Games/";  // Will be updated to include gameName
        private List<string> assemblyReferences = new List<string>();
        private List<string> precompiledReferences = new List<string>();
        private List<string> autoReferenced = new List<string> { "true" };
        private List<string> defineConstraints = new List<string>();
        private List<string> versionDefines = new List<string>();
        private List<string> noEngineReferences = new List<string> { "false" };
        private bool overwriteExisting = false;

        // UI state
        private Vector2 scrollPosition;
        private bool showAssemblyReferences = true;
        private bool showPrecompiledReferences = true;
        private bool showAdvancedOptions = false;
        private string statusMessage = "";
        private bool isSuccess = false;
        private string newReference = "";
        private string newPrecompiledReference = "";
        private bool isGameNameValid = false;

        // Common Unity references
        private readonly string[] commonUnityReferences = new string[]
        {
            "UnityEngine",
            "UnityEngine.CoreModule",
            "UnityEngine.UIModule",
            "UnityEngine.TextRenderingModule",
            "UnityEngine.UI",
            "UnityEngine.InputSystem",
            "Unity.TextMeshPro",
            "Unity.Mathematics",
            "Unity.InputSystem",
            "Unity.AI.Navigation",  
            "JoystickPack"        // Added JoystickPack as an assembly reference
        };

        // Common Nostra references (moved to precompiled references)
        private readonly string[] commonNostraPrecompiledReferences = new string[]
        {
            "NostraCore.dll",
            "NostraRemote.dll",
            "QuickPlay.dll",
            "ChronoStream.dll"
        };
        
        // Common third-party precompiled references
        private readonly string[] commonThirdPartyReferences = new string[]
        {
            "DOTween.dll",
            "Newtonsoft.Json.dll"
        };

        [MenuItem("Nostra/Tools/Assembly Definition Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<AsmdefBuilder>("Asmdef Builder");
            window.minSize = new Vector2(450, 600);
            window.maxSize = new Vector2(800, 1000);
        }

        private void OnEnable()
        {
            // Initialize with default references
            assemblyReferences = new List<string>(commonUnityReferences);
            
            // Add third-party references to precompiled references by default
            foreach (var reference in commonThirdPartyReferences)
            {
                if (!precompiledReferences.Contains(reference))
                {
                    precompiledReferences.Add(reference);
                }
            }
            
            // Add Nostra DLLs to precompiled references
            foreach (var reference in commonNostraPrecompiledReferences)
            {
                if (!precompiledReferences.Contains(reference))
                {
                    precompiledReferences.Add(reference);
                }
            }

            // Initial validation
            ValidateGameName();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Assembly Definition Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("This tool creates Assembly Definition files for games to facilitate DLL generation.", MessageType.Info);
            EditorGUILayout.Space();
            
            // Basic settings
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
            
            // Game Name field with validation
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Game Name");
            GUI.backgroundColor = string.IsNullOrEmpty(gameName) ? new Color(1f, 0.6f, 0.6f) : (isGameNameValid ? Color.white : new Color(1, 0.7f, 0.7f));
            string newGameName = EditorGUILayout.TextField(gameName);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            // Only update if the name has changed
            if (newGameName != gameName)
            {
                gameName = newGameName;
                ValidateGameName();
            }
            
            // Display validation message for game name
            if (!string.IsNullOrEmpty(gameName) && !isGameNameValid)
            {
                EditorGUILayout.HelpBox("Game name must follow PascalCase naming convention (e.g., 'GameName', not 'gameName').", MessageType.Error);
            }
            
            gameScriptsPath = EditorGUILayout.TextField("Scripts Path", gameScriptsPath);
            
            // Validate path exists
            if (!string.IsNullOrEmpty(gameScriptsPath) && !AssetDatabase.IsValidFolder(gameScriptsPath))
            {
                EditorGUILayout.HelpBox($"Warning: Path '{gameScriptsPath}' does not exist!", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            // Assembly References
            showAssemblyReferences = EditorGUILayout.Foldout(showAssemblyReferences, "Assembly References", true);
            if (showAssemblyReferences)
            {
                EditorGUI.indentLevel++;
                
                // Display existing references
                for (int i = 0; i < assemblyReferences.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    assemblyReferences[i] = EditorGUILayout.TextField(assemblyReferences[i]);
                    if (GUILayout.Button("Remove", GUILayout.Width(80)))
                    {
                        assemblyReferences.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                // Add new reference
                EditorGUILayout.BeginHorizontal();
                newReference = EditorGUILayout.TextField("New Reference", newReference);
                if (GUILayout.Button("Add", GUILayout.Width(80)) && !string.IsNullOrEmpty(newReference))
                {
                    assemblyReferences.Add(newReference);
                    newReference = "";
                }
                EditorGUILayout.EndHorizontal();
                
                // Quick add common references
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Unity References"))
                {
                    foreach (var reference in commonUnityReferences)
                    {
                        if (!assemblyReferences.Contains(reference))
                        {
                            assemblyReferences.Add(reference);
                        }
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Precompiled References
            showPrecompiledReferences = EditorGUILayout.Foldout(showPrecompiledReferences, "Precompiled References", true);
            if (showPrecompiledReferences)
            {
                EditorGUI.indentLevel++;
                
                // Display existing precompiled references
                for (int i = 0; i < precompiledReferences.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    precompiledReferences[i] = EditorGUILayout.TextField(precompiledReferences[i]);
                    if (GUILayout.Button("Remove", GUILayout.Width(80)))
                    {
                        precompiledReferences.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                // Add new precompiled reference
                EditorGUILayout.BeginHorizontal();
                newPrecompiledReference = EditorGUILayout.TextField("New Precompiled", newPrecompiledReference);
                if (GUILayout.Button("Add", GUILayout.Width(80)) && !string.IsNullOrEmpty(newPrecompiledReference))
                {
                    // Ensure .dll extension
                    if (!newPrecompiledReference.EndsWith(".dll"))
                    {
                        newPrecompiledReference += ".dll";
                    }
                    precompiledReferences.Add(newPrecompiledReference);
                    newPrecompiledReference = "";
                }
                EditorGUILayout.EndHorizontal();
                
                // Quick add buttons
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add DOTween"))
                {
                    if (!precompiledReferences.Contains("DOTween.dll"))
                    {
                        precompiledReferences.Add("DOTween.dll");
                    }
                }
                
                if (GUILayout.Button("Add Nostra DLLs"))
                {
                    foreach (var reference in commonNostraPrecompiledReferences)
                    {
                        if (!precompiledReferences.Contains(reference))
                        {
                            precompiledReferences.Add(reference);
                        }
                    }
                }
                
                if (GUILayout.Button("Add Newtonsoft.Json"))
                {
                    if (!precompiledReferences.Contains("Newtonsoft.Json.dll"))
                    {
                        precompiledReferences.Add("Newtonsoft.Json.dll");
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Advanced options
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options", true);
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                
                autoReferenced[0] = EditorGUILayout.Toggle("Auto Referenced", autoReferenced[0] == "true").ToString().ToLower();
                noEngineReferences[0] = EditorGUILayout.Toggle("No Engine References", noEngineReferences[0] == "true").ToString().ToLower();
                
                // Show the default namespace that will be used
                EditorGUILayout.HelpBox($"Namespace will be: nostra.sarvottam.{gameName.ToLower()}", MessageType.Info);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Build options
            EditorGUILayout.LabelField("Build Options", EditorStyles.boldLabel);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
            
            EditorGUILayout.Space();
            
            // Status message
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, isSuccess ? MessageType.Info : MessageType.Error);
            }
            
            // Buttons are disabled if game name is empty or invalid
            GUI.enabled = !string.IsNullOrEmpty(gameName) && isGameNameValid;
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Step 1: Create the Assembly Definition (.asmdef) file first.", MessageType.Info);
            if (GUILayout.Button("Create Assembly Definition", GUILayout.Height(30)))
            {
                CreateAssemblyDefinition();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Step 2: After compiling your project, extract the generated DLL.", MessageType.Info);
            if(GUILayout.Button("Extract DLL from Assembly Definition", GUILayout.Height(30)))
            {
                ExtractDllFromAsemblyDef();
            }
            
            // Reset GUI enabled state
            GUI.enabled = true;
            
            EditorGUILayout.EndScrollView();
        }

        
        internal void CreateAssemblyDefinition()
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(gameName))
                {
                    statusMessage = "Game name cannot be empty!";
                    isSuccess = false;
                    return;
                }

                if (string.IsNullOrEmpty(gameScriptsPath) || !AssetDatabase.IsValidFolder(gameScriptsPath))
                {
                    statusMessage = $"Invalid scripts path: {gameScriptsPath}";
                    isSuccess = false;
                    return;
                }

                // First check for existing sub-assembly definitions and remove them
                List<string> existingAsmdefs = new List<string>();
                FindAsmdefFiles(gameScriptsPath, existingAsmdefs);
                int removedCount = 0;

                // Create a string builder to collect removed asmdef names
                StringBuilder removedAsmdefs = new StringBuilder();

                // Main asmdef path to create
                string mainAsmdefPath = AssemDefPath();

                // Remove existing asmdef files (except the main one we're about to create)
                foreach (var asmdefPath in existingAsmdefs)
                {
                    if (asmdefPath != mainAsmdefPath)
                    {
                        try
                        {
                            // Delete the .asmdef file and its .meta
                            File.Delete(asmdefPath);
                            string metaPath = asmdefPath + ".meta";
                            if (File.Exists(metaPath))
                            {
                                File.Delete(metaPath);
                            }
                            
                            removedCount++;
                            removedAsmdefs.AppendLine($"- Removed {Path.GetFileNameWithoutExtension(asmdefPath)}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Failed to remove assembly definition at {asmdefPath}: {ex.Message}");
                        }
                    }
                }

                // Check if main asmdef file exists and overwrite is not enabled
                if (File.Exists(mainAsmdefPath) && !overwriteExisting)
                {
                    statusMessage = $"Assembly definition already exists at {mainAsmdefPath}. Enable overwrite to replace it.";
                    isSuccess = false;
                    return;
                }

                // Create asmdef JSON content
                string asmdefContent = GenerateAsmdefJson();

                // Write to file
                File.WriteAllText(mainAsmdefPath, asmdefContent);

                // Refresh AssetDatabase
                AssetDatabase.Refresh();

                // Create success message
                string cleanupMessage = removedCount > 0 ? 
                    $"\n\nRemoved {removedCount} existing sub-assembly definitions:\n{removedAsmdefs}" : 
                    "";
                    
                statusMessage = $"Successfully created assembly definition at {mainAsmdefPath}{cleanupMessage}";
                isSuccess = true;
            }
            catch (Exception ex)
            {
                statusMessage = $"Error creating assembly definition: {ex.Message}";
                isSuccess = false;
                Debug.LogException(ex);
            }
        }

        internal void ExtractDllFromAsemblyDef()
        {
            try
            {
                if (string.IsNullOrEmpty(gameName) || string.IsNullOrEmpty(gameScriptsPath))
                {
                    statusMessage = "Game name and scripts path are required.";
                    isSuccess = false;
                    return;
                }

                // Find all .asmdef files in the scripts directory and subdirectories
                List<string> asmdefPaths = new List<string>();
                FindAsmdefFiles(gameScriptsPath, asmdefPaths);

                if (asmdefPaths.Count == 0)
                {
                    statusMessage = $"No assembly definition files found in {gameScriptsPath} or its subdirectories.";
                    isSuccess = false;
                    return;
                }

                int extractedCount = 0;
                int deletedCount = 0;
                StringBuilder resultMessages = new StringBuilder();

                foreach (string asmdefPath in asmdefPaths)
                {
                    // Extract assembly name from the .asmdef file
                    string asmdefJson = File.ReadAllText(asmdefPath);
                    string assemblyName = ExtractAssemblyNameFromJson(asmdefJson);
                    
                    if (string.IsNullOrEmpty(assemblyName))
                    {
                        resultMessages.AppendLine($"- Could not extract assembly name from {asmdefPath}");
                        continue;
                    }

                    // Path to the compiled DLL in Library/ScriptAssemblies
                    string generatedDllPath = Path.Combine(Application.dataPath, "../Library/ScriptAssemblies", $"{assemblyName}.dll");
                    
                    if (!File.Exists(generatedDllPath))
                    {
                        resultMessages.AppendLine($"- DLL not found for {assemblyName} at {generatedDllPath}");
                        continue;
                    }

                    // Destination path is the same folder as the .asmdef file
                    string destinationFolder = Path.GetDirectoryName(asmdefPath);
                    string finalDllPath = Path.Combine(destinationFolder, $"{assemblyName}.dll");

                    // Copy the DLL
                    File.Copy(generatedDllPath, finalDllPath, true);
                    extractedCount++;
                    resultMessages.AppendLine($"- Extracted {assemblyName}.dll to {destinationFolder}");
                    
                    // Delete the .asmdef file
                    try
                    {
                        File.Delete(asmdefPath);
                        
                        // Also delete the .asmdef.meta file if it exists
                        string metaPath = asmdefPath + ".meta";
                        if (File.Exists(metaPath))
                        {
                            File.Delete(metaPath);
                        }
                        
                        deletedCount++;
                        resultMessages.AppendLine($"  - Deleted {Path.GetFileName(asmdefPath)}");
                    }
                    catch (Exception ex)
                    {
                        resultMessages.AppendLine($"  - Failed to delete {asmdefPath}: {ex.Message}");
                    }
                }

                AssetDatabase.Refresh();

                if (extractedCount > 0)
                {
                    statusMessage = $"Successfully extracted {extractedCount} DLLs and removed {deletedCount} assembly definition files.\n{resultMessages}";
                    isSuccess = true;
                }
                else
                {
                    statusMessage = $"No DLLs were extracted.\n{resultMessages}";
                    isSuccess = false;
                }
            }
            catch (Exception ex)
            {
                statusMessage = $"Error extracting DLLs: {ex.Message}";
                isSuccess = false;
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Recursively finds all .asmdef files in the specified directory and its subdirectories.
        /// </summary>
        private void FindAsmdefFiles(string directoryPath, List<string> asmdefPaths)
        {
            if (!Directory.Exists(directoryPath))
                return;

            // Add all .asmdef files in current directory
            string[] asmdefFiles = Directory.GetFiles(directoryPath, "*.asmdef");
            asmdefPaths.AddRange(asmdefFiles);

            // Recursively search subdirectories
            string[] subdirectories = Directory.GetDirectories(directoryPath);
            foreach (string subdirectory in subdirectories)
            {
                FindAsmdefFiles(subdirectory, asmdefPaths);
            }
        }

        /// <summary>
        /// Extracts the assembly name from an .asmdef JSON file.
        /// </summary>
        private string ExtractAssemblyNameFromJson(string json)
        {
            try
            {
                // Parse the JSON to find the "name" field
                int nameIndex = json.IndexOf("\"name\":");
                if (nameIndex < 0)
                    return null;

                int startQuote = json.IndexOf('"', nameIndex + 7); // After "name":
                if (startQuote < 0)
                    return null;

                int endQuote = json.IndexOf('"', startQuote + 1);
                if (endQuote < 0)
                    return null;

                return json.Substring(startQuote + 1, endQuote - startQuote - 1);
            }
            catch
            {
                return null;
            }
        }

        private void ValidateGameName()
        {
            if (string.IsNullOrEmpty(gameName))
            {
                isGameNameValid = false;
                return;
            }
            
            // Check if the name follows PascalCase convention
            // First character must be uppercase, and no spaces or special characters allowed
            Regex pascalCaseRegex = new Regex(@"^[A-Z][a-zA-Z0-9]*$");
            isGameNameValid = pascalCaseRegex.IsMatch(gameName);
            
            // Update the scripts path with correct case
            // Note: Unity's AssetDatabase.IsValidFolder is case-insensitive on macOS, 
            // but we want to maintain consistent case in our path
            if (!string.IsNullOrEmpty(gameName))
            {
                gameScriptsPath = $"Assets/Games/{gameName}";
            }
        }

        private string IntermidiatePath()
        {
            return Path.Combine(Application.dataPath, "../Library/ScriptAssemblies", $"{gameName}.dll");
        }
        private string AssemDefPath()
        {
            return $"{gameScriptsPath}/{gameName}.asmdef";
        }
        private string FinalDllPath()
        {
            return $"{gameScriptsPath}/{gameName}.dll";
        }
        private string GenerateAsmdefJson()
        {
            // Ensure all precompiled references have .dll extension
            for (int i = 0; i < precompiledReferences.Count; i++)
            {
                if (!precompiledReferences[i].EndsWith(".dll"))
                {
                    precompiledReferences[i] += ".dll";
                }
            }
            
            // Always use the default namespace format
            string rootNamespace = $"nostra.sarvottam.{gameName.ToLower()}";
            
            // Create asmdef object
            var asmdef = new Dictionary<string, object>
            {
                { "name", gameName },
                { "rootNamespace", rootNamespace },
                { "references", assemblyReferences.ToArray() },
                { "includePlatforms", new string[] { } },
                { "excludePlatforms", new string[] { } },
                { "allowUnsafeCode", false },
                { "overrideReferences", true },  // Always true when using precompiled references
                { "precompiledReferences", precompiledReferences.ToArray() },
                { "autoReferenced", autoReferenced[0] == "true" },
                { "defineConstraints", defineConstraints.ToArray() },
                { "versionDefines", versionDefines.ToArray() },
                { "noEngineReferences", noEngineReferences[0] == "true" }
            };
            
            // Convert to JSON with pretty printing
            return JsonUtility.ToJson(new AsmdefWrapper(asmdef), true);
        }

        // Helper class for JSON serialization
        [Serializable]
        private class AsmdefWrapper
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public string[] includePlatforms;
            public string[] excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public string[] precompiledReferences;
            public bool autoReferenced;
            public string[] defineConstraints;
            public string[] versionDefines;
            public bool noEngineReferences;
            
            public AsmdefWrapper(Dictionary<string, object> data)
            {
                name = (string)data["name"];
                rootNamespace = (string)data["rootNamespace"];
                references = (string[])data["references"];
                includePlatforms = (string[])data["includePlatforms"];
                excludePlatforms = (string[])data["excludePlatforms"];
                allowUnsafeCode = (bool)data["allowUnsafeCode"];
                overrideReferences = (bool)data["overrideReferences"];
                precompiledReferences = (string[])data["precompiledReferences"];
                autoReferenced = (bool)data["autoReferenced"];
                defineConstraints = (string[])data["defineConstraints"];
                versionDefines = (string[])data["versionDefines"];
                noEngineReferences = (bool)data["noEngineReferences"];
            }
        }
    }
}
