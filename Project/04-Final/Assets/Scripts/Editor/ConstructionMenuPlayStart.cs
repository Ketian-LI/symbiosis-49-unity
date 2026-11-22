using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UrbanWildlifeRooms.Editor
{
    // The Editor normally plays whichever scene is open. Keep the player's
    // entry at Main even when a developer last inspected ConstructionPrototype.
    [InitializeOnLoad]
    internal static class ConstructionMenuPlayStart
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        static ConstructionMenuPlayStart()
        {
            // Batchmode tests explicitly manage their own scene setup.
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += SetMainAsPlayStart;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (path.StartsWith("Assets/InitTestScene"))
                EditorSceneManager.playModeStartScene = null;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += SetMainAsPlayStart;
        }

        private static void SetMainAsPlayStart()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // Preserve an explicit Test Runner start scene instead of replacing
            // it with the developer convenience entry point.
            var requestedStart = EditorSceneManager.playModeStartScene;
            if (requestedStart != null && AssetDatabase.GetAssetPath(requestedStart)
                    .StartsWith("Assets/InitTestScene"))
                return;

            if (SceneManager.GetActiveScene().path.StartsWith("Assets/InitTestScene"))
                return;
            var main = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            if (main != null && EditorSceneManager.playModeStartScene != main)
                EditorSceneManager.playModeStartScene = main;
        }
    }
}
