using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace nostra.platform.tools
{
    public class CoreDLLsUtilityWindow : EditorWindow
    {
        private string npmVersion = "Checking...";
        private string latestPackageVersion = "Checking...";
        private string currentDllVersion = "Checking...";
        private string logOutput = "";
        private Vector2 scroll;

        [MenuItem("Tools/DLL Manager")]
        public static void ShowWindow()
        {
            GetWindow<CoreDLLsUtilityWindow>("Core DLL Manager");
        }

        private void OnEnable()
        {
            RefreshInfo();
        }

        void OnGUI()
        {
            GUILayout.Label("📦 NPM & DLL Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Display version information
            DrawVersionInfo();
            
            EditorGUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1)); // Divider

            // Display update buttons based on version comparison
            DrawUpdateButtons();

            EditorGUILayout.Space(5);
            if (GUILayout.Button("🔁 Refresh Info"))
            {
                RefreshInfo();
            }

            // Display log area
            DrawLogArea();
        }

        // Draws the version information section
        void DrawVersionInfo()
        {
            DrawInfo("🔢 npm version", npmVersion);
            DrawInfo("📌 core DLLs latest version", latestPackageVersion);
            DrawInfo("📋 current DLLs version", currentDllVersion);
        }

        // Draws the update buttons based on version comparison
        void DrawUpdateButtons()
        {
            bool IsValidVersion(string version) => !string.IsNullOrEmpty(version) && version != "Checking..." && version != "Not found";

            // Check if we have valid version information
            bool hasValidVersions = IsValidVersion(latestPackageVersion) && IsValidVersion(currentDllVersion);

            // Compare versions if we have valid information
            if (hasValidVersions)
            {
                // Try to parse versions for proper comparison
                System.Version currentVersion = null;
                System.Version latestVersion = null;
                bool validCurrentVersion = System.Version.TryParse(currentDllVersion, out currentVersion);
                bool validLatestVersion = System.Version.TryParse(latestPackageVersion, out latestVersion);
                
                // If we can parse both versions, do a proper version comparison
                if (validCurrentVersion && validLatestVersion)
                {
                    DrawVersionComparisonButtons(currentVersion, latestVersion);
                }
                else
                {
                    // Fallback to string comparison if version parsing fails
                    DrawStringComparisonButtons();
                }
            }
            else
            {
                // If we don't have valid version info, show a disabled button
                GUI.enabled = false;
                GUILayout.Button("🔄 Update DLLs (waiting for version info)");
                GUI.enabled = true;
            }
        }

        // Draws buttons based on proper version comparison
        void DrawVersionComparisonButtons(System.Version currentVersion, System.Version latestVersion)
        {
            int comparison = currentVersion.CompareTo(latestVersion);

            if (comparison < 0)
            {
                RenderUpdateActionButton("🔄 Update DLLs to "); // Update to newer version
            }
            else if (comparison == 0)
            {
                RenderUpdateActionButton("⚠️ Force Update to "); // Already latest, allow force update
            }
            else
            {
                EditorGUILayout.HelpBox("Current version is newer than the latest published version. This is unusual.", MessageType.Warning);
                RenderUpdateActionButton("⚠️ Downgrade to ");
            }
        }

        // Draws buttons based on string comparison (fallback)
        void DrawStringComparisonButtons()
        {
            if (currentDllVersion != latestPackageVersion)
            {
                RenderUpdateActionButton("🔄 Update DLLs to ");
            }
            else
            {
                RenderUpdateActionButton("⚠️ Force Update to ");
            }
        }

        // Unified button drawing method
        void RenderUpdateActionButton(string labelPrefix)
        {
            string buttonLabel = $"{labelPrefix}{latestPackageVersion}";

            if (GUILayout.Button(buttonLabel))
            {
                UpdateDlls();
            }
        }

        // Draws the log area with styling
        void DrawLogArea()
        {
            GUILayout.Space(10);
            
            // Log section with title and box
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("📄 Output Log:", EditorStyles.boldLabel);
            
            // Create a box style for the log content
            GUIStyle boxStyle = new GUIStyle(EditorStyles.textArea);
            boxStyle.margin = new RectOffset(4, 4, 0, 4);
            boxStyle.padding = new RectOffset(4, 4, 4, 4);
            boxStyle.normal.background = EditorGUIUtility.whiteTexture;
            
            // Apply a dark tint to the background
            Color oldColor = GUI.color;
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            
            EditorGUILayout.BeginVertical(boxStyle);
            GUI.color = oldColor;
            
            // Log content
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(150));
            GUIStyle logStyle = new GUIStyle(EditorStyles.label);
            logStyle.wordWrap = true;
            logStyle.richText = true;
            logStyle.margin = new RectOffset(0, 0, 0, 0);
            EditorGUILayout.LabelField(logOutput, logStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        void UpdateDlls()
        {
            string projectRoot = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
            string scriptPath = Path.Combine(projectRoot, "update_dlls.sh");

            Debug.Log("📂 Script path: " + scriptPath);
            logOutput += "\nRunning update script: " + scriptPath + "\n";
            Repaint();

            if (!File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("Dll update failed", "❌ update_dlls.sh not found at:\n" + scriptPath, "OK");
                logOutput += "❌ update_dlls.sh not found\n";
                Repaint();
                return;
            }

            Process process = new Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"\"{scriptPath}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WorkingDirectory = projectRoot;

            try
            {
                process.Start();
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                logOutput += stdout + "\n";
                if (!string.IsNullOrEmpty(stderr))
                    logOutput += "Errors: " + stderr + "\n";
                    
                Repaint();

                if (process.ExitCode == 0)
                {
                    EditorUtility.DisplayDialog("DLL Update", "✅ DLLs updated successfully!", "OK");
                    AssetDatabase.Refresh();
                    // Refresh version info after update
                    RefreshInfo();
                }
                else
                {
                    string message = $"❌ DLL update failed with error:\n\n{stderr}";
                    EditorUtility.DisplayDialog("DLL Update Failed", message, "OK");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error running update_dlls.sh: " + ex.Message);
                logOutput += "❌ Error: " + ex.Message + "\n";
                Repaint();
                EditorUtility.DisplayDialog("Error", "Failed to run update_dlls.sh\n" + ex.Message, "OK");
            }
        }

        void DrawInfo(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(220));
            GUILayout.Label(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        void RefreshInfo()
        {
            RunShellScript();
        }

        void RunShellScript()
        {
            string projectRoot = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
            string scriptPath = Path.Combine(projectRoot, "get_dlls_info.sh");
            logOutput = GetCommandOutput($"bash \"{scriptPath}\"");

            var lines = logOutput.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("npm version:"))
                    npmVersion = line.Replace("npm version:", "").Trim();

                if (line.StartsWith("quickplay-core version:"))
                    latestPackageVersion = line.Replace("quickplay-core version:", "").Trim();
            }
            // Get current DLL version from the version file
            string nostraPath = Path.Combine(Application.dataPath, "Plugins/Nostra");
            currentDllVersion = GetInstalledDllVersion(nostraPath);
            Repaint(); // Refresh window
        }
        
        string GetInstalledDllVersion(string installDir)
        {
            string versionFilePath = Path.Combine(installDir, "dll-version.txt");
            if (File.Exists(versionFilePath))
            {
                return File.ReadAllText(versionFilePath).Trim();
            }
            return "Not found";
        }

        string GetCommandOutput(string command)
        {
    #if UNITY_EDITOR_OSX
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
    #elif UNITY_EDITOR_WIN
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
    #endif
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            output += process.StandardError.ReadToEnd();
            process.WaitForExit();
            return output.Trim();
        }
    }
}
