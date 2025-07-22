using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace nostra.platform.tools
{
    public class GameDLLsUtilityWindow : EditorWindow
    {
        private Vector2 gameListScroll;
        
        // List of game DLLs - add or remove games as needed
        private static List<string> availableGames = new List<string>
        {
            "ColorClash.dll",
            "StairRace.dll"
            // Add more game DLLs as they become available
        };
        
        private string selectedGame = "";
        private string searchFilter = "";
        private string newGameName = "";

        [MenuItem("Tools/Game DLL Manager")]
        public static void ShowWindow()
        {
            GetWindow<GameDLLsUtilityWindow>("Game DLL Manager");
        }

        private void OnEnable()
        {
            // Select the first game by default
            if (availableGames.Count > 0 && string.IsNullOrEmpty(selectedGame))
            {
                selectedGame = availableGames[0];
            }
        }

        void OnGUI()
        {
            // Add some padding
            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });
            
            // Title
            EditorGUILayout.LabelField("Game DLL Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Search filter
            DrawSearchFilter();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Game selection area
            DrawGameSelectionArea();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Add new game section
            DrawAddNewGameSection();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Action buttons
            DrawActionButtons();
            
            EditorGUILayout.EndVertical();
        }

        void DrawSearchFilter()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Search field
            GUILayout.Label("Search:", GUILayout.Width(50));
            string newSearch = EditorGUILayout.TextField(searchFilter);
            if (newSearch != searchFilter)
            {
                searchFilter = newSearch;
            }
            
            if (!string.IsNullOrEmpty(searchFilter))
            {
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    searchFilter = "";
                    GUI.FocusControl(null);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        void DrawGameSelectionArea()
        {
            EditorGUILayout.LabelField("Available Games:", EditorStyles.boldLabel);
            
            if (availableGames.Count == 0)
            {
                EditorGUILayout.HelpBox("No games defined. Add a new game below.", MessageType.Info);
                return;
            }

            // Filter games based on search
            var filteredGames = availableGames
                .Where(g => string.IsNullOrEmpty(searchFilter) || g.ToLower().Contains(searchFilter.ToLower()))
                .ToList();

            if (filteredGames.Count == 0)
            {
                EditorGUILayout.HelpBox("No games match your search criteria.", MessageType.Info);
                return;
            }

            // Game selection list
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            gameListScroll = EditorGUILayout.BeginScrollView(gameListScroll, GUILayout.Height(150));
            
            foreach (var game in filteredGames)
            {
                EditorGUILayout.BeginHorizontal();
                
                bool isInstalled = IsGameInstalled(game);
                
                // Game selection button
                if (GUILayout.Toggle(selectedGame == game, game, EditorStyles.radioButton))
                {
                    if (selectedGame != game)
                    {
                        selectedGame = game;
                    }
                }
                
                GUILayout.FlexibleSpace();
                
                // Installation status with better colors
                if (isInstalled)
                {
                    GUILayout.Label("Installed", EditorStyles.boldLabel, GUILayout.Width(65));
                    GUILayout.Label("✓", EditorStyles.boldLabel, GUILayout.Width(15));
                }
                else
                {
                    GUILayout.Label("Not Installed", EditorStyles.miniLabel, GUILayout.Width(65));
                    GUILayout.Label("✗", EditorStyles.miniLabel, GUILayout.Width(15));
                }
                
                // Remove button
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Remove Game", 
                        $"Are you sure you want to remove {game} from the list?\n\nThis will not delete any installed files.", 
                        "Remove", "Cancel"))
                    {
                        availableGames.Remove(game);
                        if (selectedGame == game)
                        {
                            selectedGame = availableGames.Count > 0 ? availableGames[0] : "";
                        }
                        GUIUtility.ExitGUI(); // Prevent GUI errors when modifying the list during iteration
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Add a small space between items
                GUILayout.Space(2);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawAddNewGameSection()
        {
            EditorGUILayout.LabelField("Add New Game:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Show naming convention hint
            EditorGUILayout.HelpBox("Please use PascalCase naming convention (e.g., ColorClash, StairRace).", MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            
            // New game name field
            GUILayout.Label("Game Name:", GUILayout.Width(80));
            newGameName = EditorGUILayout.TextField(newGameName);
            
            // Ensure .dll extension
            bool hasExtension = newGameName.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase);
            
            // Add button
            GUI.enabled = !string.IsNullOrWhiteSpace(newGameName);
            if (GUILayout.Button("Add Game", GUILayout.Width(100)))
            {
                string gameName = hasExtension ? newGameName : newGameName + ".dll";
                
                if (!availableGames.Contains(gameName, System.StringComparer.OrdinalIgnoreCase))
                {
                    availableGames.Add(gameName);
                    // Sort the list alphabetically
                    availableGames.Sort();
                    newGameName = ""; // Clear the field after adding
                }
                else
                {
                    EditorUtility.DisplayDialog("Duplicate Game", 
                        "This game is already in the list.", "OK");
                }
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            if (!string.IsNullOrWhiteSpace(newGameName) && !hasExtension)
            {
                EditorGUILayout.HelpBox("The .dll extension will be added automatically.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawActionButtons()
        {
            EditorGUILayout.Space(5);
            
            // Download button - only enabled if a game is selected
            GUI.enabled = !string.IsNullOrEmpty(selectedGame);
            
            if (GUILayout.Button("Download Selected Game", GUILayout.Height(30)))
            {
                DownloadSelectedGame();
            }
            
            GUI.enabled = true;
        }

        void DownloadSelectedGame()
        {
            if (string.IsNullOrEmpty(selectedGame))
            {
                EditorUtility.DisplayDialog("Error", "Please select a game to download", "OK");
                return;
            }
            
            string projectRoot = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
            string scriptPath = Path.Combine(projectRoot, "download_game_dll_from_cloudsmith.sh");
            
            if (!File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("Error", 
                    $"download_game_dll_from_cloudsmith.sh not found at:\n{scriptPath}", "OK");
                return;
            }
            
            // Show a progress dialog
            EditorUtility.DisplayProgressBar("Downloading Game DLL", 
                $"Downloading {selectedGame}...", 0.5f);
            
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"\"{scriptPath}\" \"{selectedGame}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.WorkingDirectory = projectRoot;
                
                process.Start();
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                EditorUtility.ClearProgressBar();
                
                if (process.ExitCode == 0)
                {
                    EditorUtility.DisplayDialog("Game DLL Update", 
                        $"✅ {selectedGame} updated successfully!", "OK");
                    AssetDatabase.Refresh();
                }
                else
                {
                    string message = $"❌ Game DLL update failed with error:\n\n{stderr}";
                    EditorUtility.DisplayDialog("Game DLL Update Failed", message, "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"Error running download_game_dll_from_cloudsmith.sh: {ex.Message}");
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to run download_game_dll_from_cloudsmith.sh\n{ex.Message}", "OK");
            }
        }

        bool IsGameInstalled(string gameName)
        {
            // Extract game name without .dll extension
            string gameNameWithoutExtension = Path.GetFileNameWithoutExtension(gameName);
            
            // Check if the game directory exists
            string gameDir = Path.Combine(Application.dataPath, "Games", gameNameWithoutExtension);
            bool dirExists = Directory.Exists(gameDir);
            
            // Check if the DLL file exists
            string dllPath = Path.Combine(gameDir, gameName);
            bool dllExists = File.Exists(dllPath);
            
            return dirExists && dllExists;
        }
    }
}