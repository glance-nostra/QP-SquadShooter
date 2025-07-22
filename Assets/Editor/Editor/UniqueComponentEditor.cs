using UnityEngine;
using ChronoStream.Unique;
using UnityEditor;

namespace ChronoStream.Editor{
    [CustomEditor(typeof(UniqueId), true)]
    public class UniqueComponentEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            UniqueId unique = (UniqueId)target;
            GUI.enabled = false;
            EditorGUILayout.LabelField("Unique Component", EditorStyles.boldLabel);
            EditorGUILayout.TextField("UDID", unique.GUID);
            GUI.enabled = true;
        }
    }
}
