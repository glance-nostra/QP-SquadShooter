using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace NostraTools.Editor
{
    /// <summary>
    /// Command line utility to extract DLLs from Assembly Definition files and remap prefabs
    /// </summary>
    public class ExtractAndRemapDll
    {
        private static bool isVerbose = false;
        private static string gameScriptsPath = "";
        private static bool shouldRemap = false;

        /// <summary>
        /// Entry point for the script, can be called from command line
        /// Unity requires this method to have no parameters
        /// </summary>
        public static void Main()
        {
            try
            {
                // Parse command-line arguments
                ParseCommandLineArguments();
                
                if (string.IsNullOrEmpty(gameScriptsPath))
                {
                    LogError("Scripts path is required. Use -scriptsPath argument.");
                    return;
                }

                if (!Directory.Exists(gameScriptsPath))
                {
                    LogError($"Directory does not exist: {gameScriptsPath}");
                    return;
                }

                // Step 1: Extract DLLs
                List<string> extractedAssemblies = new List<string>();
                int extractedCount = ExtractDlls(gameScriptsPath, extractedAssemblies);
                Log($"Extraction complete: {extractedCount} DLLs processed");
                
                // Step 2: Remap prefabs if requested
                if (shouldRemap && extractedCount > 0)
                {
                    foreach (string assemblyName in extractedAssemblies)
                    {
                        Log($"Remapping prefabs for assembly: {assemblyName}");
                        string dllAssemblyName = $"{assemblyName}.dll";
                        RemapDllUtils.RemapToDllAssembly(dllAssemblyName, gameScriptsPath);
                    }
                    Log("Remapping complete");
                }
                else {
                    //print shouldRemap and extractedCount
                    Log($"Remapping skipped. shouldRemap: {shouldRemap}, extractedCount: {extractedCount}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse command-line arguments passed to Unity
        /// </summary>
        private static void ParseCommandLineArguments()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-scriptsPath" && i + 1 < args.Length)
                {
                    gameScriptsPath = args[i + 1];
                    Log($"Scripts path: {gameScriptsPath}");
                }
                else if (args[i] == "-verbose")
                {
                    isVerbose = true;
                    Log("Verbose logging enabled");
                }
                else if (args[i] == "-remap")
                {
                    shouldRemap = true;
                    Log("Remapping enabled");
                }
            }
        }

        /// <summary>
        /// Extract DLLs from assembly definition files
        /// </summary>
        /// <param name="scriptsPath">Path to the scripts directory</param>
        /// <param name="extractedAssemblies">List to be populated with extracted assembly names</param>
        /// <returns>Number of DLLs extracted</returns>
        private static int ExtractDlls(string scriptsPath, List<string> extractedAssemblies)
        {
            Log($"Extracting DLLs from: {scriptsPath}");
            
            // Find all .asmdef files
            List<string> asmdefPaths = new List<string>();
            FindAsmdefFiles(scriptsPath, asmdefPaths);

            if (asmdefPaths.Count == 0)
            {
                LogWarning($"No assembly definition files found in {scriptsPath}");
                return 0;
            }

            Log($"Found {asmdefPaths.Count} assembly definition files");
            
            int extractedCount = 0;
            
            foreach (string asmdefPath in asmdefPaths)
            {
                // Extract assembly name from the .asmdef file
                string asmdefJson = File.ReadAllText(asmdefPath);
                string assemblyName = ExtractAssemblyNameFromJson(asmdefJson);
                
                if (string.IsNullOrEmpty(assemblyName))
                {
                    LogWarning($"Could not extract assembly name from {asmdefPath}");
                    continue;
                }

                // Path to the compiled DLL in Library/ScriptAssemblies
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string generatedDllPath = Path.Combine(projectRoot, "Library/ScriptAssemblies", $"{assemblyName}.dll");
                
                if (!File.Exists(generatedDllPath))
                {
                    LogWarning($"DLL not found for {assemblyName} at {generatedDllPath}");
                    continue;
                }

                // Destination path is the same folder as the .asmdef file
                string destinationFolder = Path.GetDirectoryName(asmdefPath);
                string finalDllPath = Path.Combine(destinationFolder, $"{assemblyName}.dll");

                // Copy the DLL
                File.Copy(generatedDllPath, finalDllPath, true);
                extractedCount++;
                extractedAssemblies.Add(assemblyName);
                Log($"Extracted {assemblyName}.dll to {destinationFolder}");
                
                // Delete the .asmdef file and its .meta file
                DeleteAsmdefFiles(asmdefPath);
            }

            AssetDatabase.Refresh();
            return extractedCount;
        }
        
        /// <summary>
        /// Delete asmdef file and its meta file
        /// </summary>
        private static void DeleteAsmdefFiles(string asmdefPath)
        {
            try
            {
                File.Delete(asmdefPath);
                
                // Also delete the .asmdef.meta file if it exists
                string metaPath = asmdefPath + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
                
                LogVerbose($"Deleted {Path.GetFileName(asmdefPath)}");
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to delete {asmdefPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively finds all .asmdef files in the specified directory and its subdirectories.
        /// </summary>
        private static void FindAsmdefFiles(string directoryPath, List<string> asmdefPaths)
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
        private static string ExtractAssemblyNameFromJson(string json)
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

        #region Logging

        private static void Log(string message)
        {
            Debug.Log($"[ExtractAndRemapDll] {message}");
        }

        private static void LogWarning(string message)
        {
            Debug.LogWarning($"[ExtractAndRemapDll] {message}");
        }

        private static void LogError(string message)
        {
            Debug.LogError($"[ExtractAndRemapDll] {message}");
        }

        private static void LogVerbose(string message)
        {
            if (isVerbose)
            {
                Debug.Log($"[ExtractAndRemapDll] {message}");
            }
        }

        #endregion
    }
}
