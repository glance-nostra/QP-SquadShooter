using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using nostra.platform.Core;

namespace nostra.platform.Editor
{
    [CustomEditor(typeof(QuickPlayController))]
    public class QuickPlayControllerEditor : UnityEditor.Editor
    {
        SerializedProperty m_quickPlayType;
        SerializedProperty m_testGamePosts;
        ReorderableList testGamePostList;

        void OnEnable()
        {
            m_quickPlayType = serializedObject.FindProperty("m_quickPlayType");
            m_testGamePosts = serializedObject.FindProperty("m_testGamePosts");

            testGamePostList = new ReorderableList(serializedObject, m_testGamePosts, true, true, true, true);

            testGamePostList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Test Game Posts");
            };

            testGamePostList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = m_testGamePosts.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(rect, element, GUIContent.none, true);
            };

            testGamePostList.elementHeightCallback = index =>
            {
                var element = m_testGamePosts.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 4;
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_quickPlayType", "m_testGamePosts");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_quickPlayType);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Repaint();
            }

            if ((QuickPlayType)m_quickPlayType.enumValueIndex == QuickPlayType.TEST)
            {
                EditorGUILayout.Space();
                testGamePostList.DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
