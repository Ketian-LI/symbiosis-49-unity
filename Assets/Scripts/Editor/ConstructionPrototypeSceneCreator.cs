using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Editor
{
    public static class ConstructionPrototypeSceneCreator
    {
        private const string ScenePath = "Assets/Scenes/ConstructionPrototype.unity";

        [MenuItem("SYMBIOSIS/Create Construction Prototype Scene")]
        public static void CreateScene()
        {
            if (File.Exists(ScenePath))
            {
                Debug.Log($"Construction prototype scene already exists: {ScenePath}");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var root = new GameObject("Construction Prototype");
            root.AddComponent<ConstructionPrototypeController>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException($"Could not save {ScenePath}");
            AssetDatabase.ImportAsset(ScenePath);

            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(item => item.path != ScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            Debug.Log($"Created construction prototype scene: {ScenePath}");
        }
    }
}
