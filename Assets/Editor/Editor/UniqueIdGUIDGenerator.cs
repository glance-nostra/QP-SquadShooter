#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Presets;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.Callbacks;
using UnityEditor.VersionControl;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using ChronoStream.Unique;
using System.IO;

public static class UniqueIdGUIDGenerator {
    [MenuItem("Tools/UniqueId/Assign New GUIDs To All Prefabs and Scenes")]
    public static void AssignGuidsProjectWide() {
        var seenGuids = new HashSet<string>();
        var guidCounts = new Dictionary<string, int>();

        // Step 1: Collect all UniqueIds across all prefabs
        string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (var prefabGuid in allPrefabGuids) {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            var uniqueIds = prefabRoot.GetComponentsInChildren<UniqueId>(true);

            foreach (var uid in uniqueIds) {
                if (!string.IsNullOrEmpty(uid.GUID)) {
                    if (!guidCounts.ContainsKey(uid.GUID))
                        guidCounts[uid.GUID] = 0;
                    guidCounts[uid.GUID]++;
                }
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        // Step 2: Assign new GUIDs as needed
        foreach (var prefabGuid in allPrefabGuids) {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            bool modified = false;

            var uniqueIds = prefabRoot.GetComponentsInChildren<UniqueId>(true);
            foreach (var uid in uniqueIds) {
                bool isMissing = string.IsNullOrEmpty(uid.GUID);
                bool isDuplicate = !isMissing && guidCounts[uid.GUID] > 1;

                if (isMissing || isDuplicate) {
                    string newGuid;
                    do {
                        newGuid = System.Guid.NewGuid().ToString();
                    } while (guidCounts.ContainsKey(newGuid));

                    var so = new SerializedObject(uid);
                    so.FindProperty("guid").stringValue = newGuid;
                    so.ApplyModifiedProperties();

                    guidCounts[newGuid] = 1;
                    modified = true;

                    Debug.Log($"[UniqueIdGUIDGenerator] Assigned new GUID in prefab '{path}' on '{uid.gameObject.name}': {newGuid}");
                }
            }

            if (modified) {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[UniqueIdGUIDGenerator] GUID assignment completed for all prefabs.");
    }
}
#endif
