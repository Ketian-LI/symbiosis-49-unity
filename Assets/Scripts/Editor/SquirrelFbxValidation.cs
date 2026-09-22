using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UrbanWildlifeRooms.Editor
{
    public static class SquirrelFbxValidation
    {
        private const string AssetPath = "Assets/Resources/Animals/Squirrel/Squirrel_Animated_v02.fbx";

        [MenuItem("Urban Wildlife/Validate Blender Squirrel Asset")]
        public static void Validate()
        {
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(AssetPath) as ModelImporter;
            if (importer == null)
            {
                throw new System.InvalidOperationException($"Could not access the model importer for {AssetPath}.");
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            var configuredClips = importer.defaultClipAnimations;
            if (configuredClips.Length > 0)
            {
                configuredClips[0].name = "Squirrel_Demo_12fps";
                configuredClips[0].loopTime = false;
                importer.clipAnimations = configuredClips;
                importer.SaveAndReimport();
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath);
            var meshes = prefab == null ? 0 : prefab.GetComponentsInChildren<MeshRenderer>(true).Length;
            var clips = AssetDatabase.LoadAllAssetsAtPath(AssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__"))
                .ToArray();
            if (prefab == null || meshes == 0 || clips.Length == 0)
            {
                throw new System.InvalidOperationException(
                    $"Squirrel FBX import is incomplete. Mesh renderers: {meshes}; animation clips: {clips.Length}.");
            }

            Debug.Log($"[Squirrel FBX] Import passed: {meshes} mesh renderers, {clips.Length} animation clip(s): {string.Join(", ", clips.Select(clip => clip.name))}.");
        }
    }
}
