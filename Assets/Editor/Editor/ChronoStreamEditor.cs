using UnityEditor;
using UnityEngine;
using ChronoStream;

[CustomEditor(typeof(ChronoStreamManager))]
public class ChronoStreamEditor : Editor
{
    private ChronoStreamManager manager;
    private float playbackSpeed = 1f;

    public override void OnInspectorGUI()
    {
        manager = (ChronoStreamManager)target;
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🕹 ChronoStream Controls", EditorStyles.boldLabel);

        var currentMode = GetCurrentMode();

        // MODE SWITCH
        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        var newMode = (ChronoStreamManager.EditorMode)EditorGUILayout.EnumPopup("Mode", currentMode);
        if (newMode != currentMode && EditorApplication.isPlaying)
        {
            SetMode(newMode);
        }
        EditorGUI.EndDisabledGroup();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use ChronoStream controls.", MessageType.Info);
            return;
        }

        // === Record Mode UI ===
        if (newMode == ChronoStreamManager.EditorMode.Record)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🔴 Record Controls", EditorStyles.boldLabel);

            if (manager.Mode != ChronoStreamManager.StreamMode.Record)
            {
                if (GUILayout.Button("▶ Start Recording"))
                    manager.StartRecording();
            }
            else if (manager.Mode == ChronoStreamManager.StreamMode.Record)
            {
                if (GUILayout.Button("⏹ Stop Recording"))
                    manager.SaveRecording();
            }

            if (!manager.RecordingPaused)
            {
                if (GUILayout.Button("⏸ Pause Recording"))
                    manager.PauseRecording();
            }
            else
            {
                if (GUILayout.Button("▶ Resume Recording"))
                    manager.UnPauseRecording();
            }
        }

        // === Replay Mode UI ===
        else if (newMode == ChronoStreamManager.EditorMode.Replay)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🔁 Replay Controls", EditorStyles.boldLabel);

            if (manager.Mode != ChronoStreamManager.StreamMode.Replay)
            {
                if (GUILayout.Button("▶ Start Replay"))
                    manager.StartReplay();
            }
            else if (manager.Mode == ChronoStreamManager.StreamMode.Replay)
            {
                if (GUILayout.Button("⏹ Stop Replay"))
                    manager.StopReplay();
                if (GUILayout.Button("⏸ Pause Replay"))
                    manager.PauseReplay();
                if (GUILayout.Button("▶ Resume Replay"))
                    manager.UnPauseReplay();
            }
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
        }
    }

    private ChronoStreamManager.EditorMode GetCurrentMode()
    {
        var modeField = typeof(ChronoStreamManager).GetField("editorMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (ChronoStreamManager.EditorMode)modeField.GetValue(target);
    }

    private void SetMode(ChronoStreamManager.EditorMode mode)
    {
        var modeField = typeof(ChronoStreamManager).GetField("editorMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        modeField.SetValue(manager, mode);
        EditorUtility.SetDirty(manager);
    }
}
