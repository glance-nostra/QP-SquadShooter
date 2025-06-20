using UnityEditor;
using UnityEngine;
using System;

namespace NostraTools.Editor
{
    public class QPPackageExporter : EditorWindow
    {
        private string _targetAssemblyName = ""; // The DLL assembly name
        private string _targetPrefabDir = "Assets/";  // Directory containing prefabs to remap

        [MenuItem("Nostra/Tools/Remap Prefabs")]
        public static void ShowWindow()
        {
            var window = GetWindow<QPPackageExporter>("Prefab Remapper");
            window.Show();
        }

        internal static void OpenExportWindow(string targetAssemblyName)
        {
            var window = GetWindow<QPPackageExporter>("Remap Prefabs");
            window._targetAssemblyName = targetAssemblyName;
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Prefab Component Remapper", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Remap prefab components between Unity scripts and your DLL.", MessageType.Info);
            EditorGUILayout.Space(10);

            // Target assembly name field
            _targetAssemblyName = EditorGUILayout.TextField("DLL Assembly Name", _targetAssemblyName);

            // Target Prefab Directory Picker
            _targetPrefabDir = BrowseField("Target Prefab Dir", _targetPrefabDir);

            EditorGUILayout.Space(10);

            // Remap options
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (GUILayout.Button($"Remap Assets → {_targetAssemblyName}", GUILayout.Height(30)))
            {
                if (string.IsNullOrEmpty(_targetAssemblyName))
                {
                    EditorUtility.DisplayDialog("Error", "Please specify a target assembly name.", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(_targetPrefabDir))
                {
                    EditorUtility.DisplayDialog("Error", "Please specify a valid prefab directory.", "OK");
                    return;
                }

                // Remap prefabs from source to target assembly
                RemapPrefabs(_targetAssemblyName, _targetPrefabDir);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Remaps prefabs from one assembly to another
        /// </summary>
        private void RemapPrefabs(string targetAssembly, string prefabDir)
        {
            try
            {
                bool targetExists = Array.Find(
                    AppDomain.CurrentDomain.GetAssemblies(), 
                    a => a.GetName().Name == targetAssembly) != null;

                if (!targetExists)
                {
                    Debug.LogWarning($"Target assembly '{targetAssembly}' not found in current domain. " +
                                    "Remapping may not work correctly.");
                }

                // Call the remapping utility
                RemapDllUtils.RemapToDllAssembly(targetAssembly,prefabDir);

                EditorUtility.DisplayDialog("Remapping Complete", 
                    $"Prefabs have been remapped  to '{targetAssembly}'.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during remapping: {ex.Message}");
                EditorUtility.DisplayDialog("Remapping Failed", 
                    $"An error occurred: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Reusable component for a text field with a "Browse" button.
        /// </summary>
        private string BrowseField(string label, string path)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel($"Select {label}", path, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    path = ConvertToRelativePath(selectedPath);
                }
            }
            EditorGUILayout.EndHorizontal();
            return path;
        }

        private string ConvertToRelativePath(string absolutePath)
        {
            if (absolutePath.StartsWith(Application.dataPath))
            {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }
            return absolutePath;
        }
    }
}
