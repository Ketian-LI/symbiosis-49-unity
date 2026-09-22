using System;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>
    /// Builds the five approved waste-room states from lightweight primitives.
    /// Everything stays in the four corners so the cross route remains clear.
    /// </summary>
    public sealed class WasteRoomLoadVisual : MonoBehaviour
    {
        private static readonly Vector2[] BinPositions =
        {
            new(-1.11f, 1.11f), new(1.11f, 1.11f),
            new(-1.11f, -1.11f), new(1.11f, -1.11f),
            new(-0.80f, 1.11f), new(0.80f, 1.11f),
            new(-0.80f, -1.11f), new(0.80f, -1.11f),
            new(-1.11f, 0.80f), new(1.11f, 0.80f),
            new(-1.11f, -0.80f), new(1.11f, -0.80f),
            new(-0.80f, 0.80f), new(0.80f, 0.80f),
            new(-0.80f, -0.80f), new(0.80f, -0.80f)
        };

        private static readonly Vector2[] BagPositions =
        {
            new(-0.73f, 1.23f), new(1.23f, 0.73f), new(-1.23f, -0.73f),
            new(0.73f, -1.23f), new(-1.25f, 0.72f), new(0.72f, 1.25f),
            new(-0.72f, -1.25f), new(1.25f, -0.72f), new(-0.73f, 0.77f),
            new(0.77f, 0.73f), new(-0.77f, -0.73f), new(0.73f, -0.77f)
        };

        private static readonly Color[] BinColours =
        {
            new(0.20f, 0.43f, 0.28f),
            new(0.31f, 0.55f, 0.36f),
            new(0.42f, 0.56f, 0.66f),
            new(0.34f, 0.34f, 0.34f)
        };

        private Material surfaceMaterial;
        private HideFlags generatedHideFlags;
        private Transform generatedRoot;

        public WasteRoomLoadModel Model { get; private set; }
        public WasteRoomLoadStage DisplayedStage { get; private set; }

        public void Initialize(Material material, HideFlags hideFlags, int initialUnits = 0)
        {
            surfaceMaterial = material != null
                ? material
                : throw new ArgumentNullException(nameof(material));
            generatedHideFlags = hideFlags;
            Model = new WasteRoomLoadModel();
            Model.Changed += HandleLoadChanged;
            Model.Restore(initialUnits);
            Rebuild();
        }

        private void OnDestroy()
        {
            if (Model != null)
            {
                Model.Changed -= HandleLoadChanged;
            }
        }

        private void HandleLoadChanged()
        {
            Rebuild();
        }

        private void Rebuild()
        {
            ClearGenerated();
            DisplayedStage = Model?.Stage ?? WasteRoomLoadStage.Empty;

            var rootObject = new GameObject($"Waste State - {DisplayedStage}")
            {
                hideFlags = generatedHideFlags
            };
            generatedRoot = rootObject.transform;
            generatedRoot.SetParent(transform, false);

            var binCount = DisplayedStage switch
            {
                WasteRoomLoadStage.Empty => 4,
                WasteRoomLoadStage.Low => 9,
                WasteRoomLoadStage.Medium => 12,
                _ => 16
            };
            var bagCount = DisplayedStage switch
            {
                WasteRoomLoadStage.Medium => 3,
                WasteRoomLoadStage.Full => 8,
                WasteRoomLoadStage.Overflow => 12,
                _ => 0
            };

            for (var index = 0; index < binCount; index++)
            {
                CreateBin(index, BinPositions[index]);
            }

            for (var index = 0; index < bagCount; index++)
            {
                CreateBag(index, BagPositions[index]);
            }

            if (DisplayedStage == WasteRoomLoadStage.Overflow)
            {
                CreateOverflowFeedback();
            }
        }

        private void CreateBin(int index, Vector2 position)
        {
            var colour = BinColours[index % BinColours.Length];
            var height = 0.42f + (index % 3) * 0.035f;
            var width = 0.27f + (index % 2) * 0.025f;
            var angle = (index % 5 - 2) * 4f;

            var bin = CreatePrimitive(
                PrimitiveType.Cube,
                $"Closed Bin {index + 1}",
                new Vector3(position.x, 0.27f + height * 0.5f, position.y),
                new Vector3(width, height, width * 0.92f),
                colour);
            bin.transform.localRotation = Quaternion.Euler(0f, angle, 0f);

            var lid = CreatePrimitive(
                PrimitiveType.Cube,
                $"Bin Lid {index + 1}",
                new Vector3(position.x, 0.29f + height, position.y),
                new Vector3(width * 1.08f, 0.055f, width),
                Color.Lerp(colour, Color.white, 0.08f));
            lid.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        private void CreateBag(int index, Vector2 position)
        {
            var scale = 0.19f + (index % 3) * 0.018f;
            var bag = CreatePrimitive(
                PrimitiveType.Sphere,
                $"Tied Bag {index + 1}",
                new Vector3(position.x, 0.30f + scale * 0.55f, position.y),
                new Vector3(scale, scale * 0.92f, scale),
                new Color(0.105f, 0.105f, 0.115f));
            bag.transform.localRotation = Quaternion.Euler(0f, index * 17f, 0f);

            CreatePrimitive(
                PrimitiveType.Sphere,
                $"Bag Knot {index + 1}",
                new Vector3(position.x, 0.31f + scale, position.y),
                Vector3.one * 0.055f,
                new Color(0.13f, 0.13f, 0.14f));
        }

        private void CreateOverflowFeedback()
        {
            var scrapPositions = new[]
            {
                new Vector3(-0.73f, 0.29f, 1.02f),
                new Vector3(1.02f, 0.29f, 0.73f),
                new Vector3(0.73f, 0.29f, -1.02f)
            };

            for (var index = 0; index < scrapPositions.Length; index++)
            {
                var scrap = CreatePrimitive(
                    PrimitiveType.Cube,
                    $"Edible Scrap {index + 1}",
                    scrapPositions[index],
                    new Vector3(0.10f + index * 0.012f, 0.035f, 0.07f),
                    index == 1
                        ? new Color(0.77f, 0.30f, 0.18f)
                        : new Color(0.88f, 0.66f, 0.28f));
                scrap.transform.localRotation = Quaternion.Euler(0f, 18f + index * 41f, 0f);
            }

            var flyPositions = new[]
            {
                new Vector3(-1.08f, 0.96f, 0.94f),
                new Vector3(-0.86f, 0.89f, 1.16f),
                new Vector3(1.10f, 1.00f, 0.89f),
                new Vector3(0.93f, 0.92f, 1.14f)
            };
            for (var index = 0; index < flyPositions.Length; index++)
            {
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    $"Fly {index + 1}",
                    flyPositions[index],
                    new Vector3(0.035f, 0.025f, 0.035f),
                    new Color(0.08f, 0.07f, 0.06f));
            }

            for (var index = 0; index < 3; index++)
            {
                var ripple = CreatePrimitive(
                    PrimitiveType.Cube,
                    $"Odour Ripple {index + 1}",
                    new Vector3(-1.00f + index * 0.11f, 0.84f + index * 0.06f, -0.98f),
                    new Vector3(0.035f, 0.035f, 0.18f),
                    new Color(0.45f, 0.43f, 0.19f));
                ripple.transform.localRotation = Quaternion.Euler(0f, index % 2 == 0 ? 12f : -12f, 0f);
            }
        }

        private GameObject CreatePrimitive(
            PrimitiveType type,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Color colour)
        {
            return UrbanVisualFactory.CreatePrimitive(
                type,
                objectName,
                generatedRoot,
                localPosition,
                localScale,
                colour,
                surfaceMaterial,
                true,
                generatedHideFlags);
        }

        private void ClearGenerated()
        {
            if (generatedRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedRoot.gameObject);
            }
            else
            {
                DestroyImmediate(generatedRoot.gameObject);
            }

            generatedRoot = null;
        }
    }
}
