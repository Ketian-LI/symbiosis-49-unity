using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UrbanWildlifeRooms.Editor
{
    public static class PigeonFbxValidation
    {
        private const string AssetPath = "Assets/Resources/Animals/Pigeon/Pigeon_Animated_v02.fbx";

        [MenuItem("Urban Wildlife/Validate Blender Pigeon Asset")]
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
                configuredClips[0].name = "Pigeon_Demo_12fps";
                configuredClips[0].loopTime = false;
                importer.clipAnimations = configuredClips;
                importer.SaveAndReimport();
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath);
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"Could not import {AssetPath}.");
            }

            var meshes = prefab.GetComponentsInChildren<MeshRenderer>(true).Length;
            var clips = AssetDatabase.LoadAllAssetsAtPath(AssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__"))
                .ToArray();

            if (meshes == 0 || clips.Length == 0)
            {
                throw new System.InvalidOperationException(
                    $"Pigeon FBX import is incomplete. Mesh renderers: {meshes}; animation clips: {clips.Length}.");
            }

            Debug.Log($"[Pigeon FBX] Import passed: {meshes} mesh renderers, {clips.Length} animation clip(s): {string.Join(", ", clips.Select(clip => clip.name))}.");
        }
    }
}
