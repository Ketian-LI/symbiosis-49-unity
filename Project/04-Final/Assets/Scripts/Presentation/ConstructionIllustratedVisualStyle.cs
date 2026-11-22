using UnityEngine;
using System.Collections.Generic;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Presentation
{
    // Visual adapter for the illustrated construction tiles. The existing
    // construction model, room roots, routes and actor motion stay in charge.
    internal static class ConstructionIllustratedVisualStyle
    {
        internal enum ActorKind { Resident, Car, Pigeon, Squirrel, Hedgehog, Fox }

        private const float PixelsPerUnit = 100f;
        private const float PlotHeight = 5f;
        private const float ActorHeight = 5.15f;
        private static readonly int[] PlotX = { 94, 418, 736, 1051, 1370 };
        private static readonly Rect[] People =
        {
            new(144, 104, 172, 285), new(560, 94, 186, 296),
            new(1015, 94, 182, 296), new(1428, 102, 230, 307)
        };
        private static readonly Rect[] Cars =
        {
            new(105, 540, 257, 254), new(527, 540, 308, 254),
            new(965, 530, 300, 262), new(1394, 540, 306, 254)
        };
        private static readonly Rect[,] Wildlife =
        {
            { new(58, 95, 254, 270), new(380, 110, 289, 255), new(733, 149, 294, 216) },
            { new(47, 360, 263, 345), new(371, 418, 315, 291), new(741, 360, 271, 351) },
            { new(54, 766, 270, 260), new(391, 770, 285, 243), new(742, 773, 271, 250) },
            { new(27, 1072, 310, 286), new(360, 1097, 339, 243), new(722, 1108, 330, 244) }
        };

        private static bool loaded;
        private static bool available;
        private static Sprite[] plotSprites;
        private static Sprite[] upgradedPlotSprites;
        private static Sprite[] residentSprites;
        private static Sprite[] carSprites;
        private static Sprite[][] wildlifeSprites;

        private static bool EnsureSprites()
        {
            if (loaded) return available;
            loaded = true;
            var plots = Resources.Load<Texture2D>("ArtStyleExperiment/urban-plots-v1");
            var people = Resources.Load<Texture2D>("ArtStyleExperiment/resident-car-poses-v2");
            var animals = Resources.Load<Texture2D>("ArtStyleExperiment/four-animals-poses-v2");
            if (plots == null || people == null || animals == null)
            {
                Debug.LogWarning("Illustrated construction atlas missing; keeping the existing room visuals.");
                return false;
            }

            plotSprites = new Sprite[10];
            for (var index = 0; index < plotSprites.Length; index++)
                plotSprites[index] = Crop(plots,
                    new Rect(PlotX[index % 5], index < 5 ? 116 : 459, 310, 310));
            var upgrades = Resources.Load<Texture2D>(
                "ArtStyleExperiment/urban-plot-upgrades-v1");
            if (upgrades != null)
            {
                // Coordinates include the four connector tabs on each tile.
                var areas = new Rect[]
                {
                    new(21, 17, 396, 396), new(427, 17, 400, 396),
                    new(836, 17, 398, 396), new(21, 425, 397, 373),
                    new(427, 425, 400, 373), new(836, 425, 398, 373),
                    new(21, 811, 397, 408), new(427, 811, 401, 408),
                    new(836, 811, 398, 408)
                };
                var names = new[]
                {
                    "Oak 2", "Oak 3", "Meadow 2", "Meadow 3",
                    "Shrub 2", "Shrub 3", "Garden residence",
                    "Terrace restaurant", "Slow street"
                };
                upgradedPlotSprites = new Sprite[areas.Length];
                for (var index = 0; index < areas.Length; index++)
                {
                    upgradedPlotSprites[index] = Crop(upgrades, areas[index]);
                    upgradedPlotSprites[index].name = names[index];
                }
            }
            residentSprites = new Sprite[People.Length];
            carSprites = new Sprite[Cars.Length];
            for (var index = 0; index < People.Length; index++)
            {
                residentSprites[index] = Crop(people, People[index]);
                carSprites[index] = Crop(people, Cars[index]);
            }
            wildlifeSprites = new Sprite[4][];
            for (var species = 0; species < 4; species++)
            {
                wildlifeSprites[species] = new Sprite[3];
                for (var frame = 0; frame < 3; frame++)
                    wildlifeSprites[species][frame] = Crop(animals,
                        Wildlife[species, frame]);
            }
            available = true;
            return true;
        }

        private static Sprite Crop(Texture2D texture, Rect topLeftPixels)
        {
            var bottomLeft = new Rect(topLeftPixels.x,
                texture.height - topLeftPixels.y - topLeftPixels.height,
                topLeftPixels.width, topLeftPixels.height);
            return Sprite.Create(texture, bottomLeft, new Vector2(0.5f, 0.5f),
                PixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        private static int PlotIndex(ConstructionTileData tile) => tile.category switch
        {
            ConstructionCategory.Green => tile.planting switch
            {
                GreenPlanting.Oak => 0,
                GreenPlanting.Shrub => 2,
                _ => 1
            },
            ConstructionCategory.Residence => 3,
            ConstructionCategory.Workshop => 4,
            ConstructionCategory.Restaurant => 5,
            ConstructionCategory.Supermarket => 6,
            ConstructionCategory.Waste => 7,
            ConstructionCategory.Street => 8,
            ConstructionCategory.Square => 9,
            _ => 1
        };

        internal static Sprite UpgradePicture(ConstructionTileData tile, int level)
        {
            if (tile == null || level < 2 || !EnsureSprites() ||
                upgradedPlotSprites == null) return null;
            var index = tile.category switch
            {
                ConstructionCategory.Green => PlotIndex(tile) * 2 + level - 2,
                ConstructionCategory.Residence => 6,
                ConstructionCategory.Restaurant => 7,
                ConstructionCategory.Street => 8,
                _ => -1
            };
            return index < 0 ? null : upgradedPlotSprites[index];
        }

        internal static void ApplyRoom(GameObject room, ConstructionTileData tile,
            ConstructionRunModel run)
        {
            if (room == null || tile == null || !EnsureSprites()) return;
            var level = tile.category == ConstructionCategory.Green
                ? run.GreenUpgradeLevel(tile.id)
                : run.UsesNeighborUpgrades
                    ? ConstructionPlacementScore.UpgradeLevel(tile, run.BuiltTiles)
                    : 1;
            var upgrade = UpgradePicture(tile, level);
            var turnCrossing = upgrade != null &&
                tile.category == ConstructionCategory.Street &&
                ConstructionPlacementScore.SlowStreetAxis(tile, run.BuiltTiles) ==
                    ConstructionSlowStreetAxis.NorthSouth;
            ApplyPlot(room, upgrade ?? plotSprites[PlotIndex(tile)], turnCrossing);
        }

        internal static void ApplyStarterOak(GameObject room)
        {
            if (room == null || !EnsureSprites()) return;
            ApplyPlot(room, plotSprites[0]);
        }

        private static void ApplyPlot(GameObject room, Sprite sprite,
            bool turnCrossing = false)
        {
            var state = room.GetComponent<ConstructionIllustratedRoomSprite>();
            if (state == null)
                state = room.AddComponent<ConstructionIllustratedRoomSprite>();
            var plot = room.transform.Find("Illustrated construction plot");
            SpriteRenderer picture;
            if (plot == null)
            {
                var objectRoot = new GameObject("Illustrated construction plot");
                objectRoot.layer = 29;
                objectRoot.transform.SetParent(room.transform, false);
                picture = objectRoot.AddComponent<SpriteRenderer>();
                picture.flipY = true;
                picture.sortingOrder = 0;
                plot = objectRoot.transform;
            }
            else picture = plot.GetComponent<SpriteRenderer>();

            picture.sprite = sprite;
            plot.position = room.transform.position + Vector3.up * PlotHeight;
            plot.rotation = Quaternion.Euler(0f, turnCrossing ? 90f : 0f, 0f) *
                Quaternion.Euler(-90f, 0f, 0f);
            var parentScale = Mathf.Max(0.001f, Mathf.Abs(room.transform.lossyScale.x));
            var imageScale =
                (WorldScaleStandards.CellSizeMeters - 0.07f) * PixelsPerUnit /
                parentScale;
            plot.localScale = new Vector3(imageScale / sprite.rect.width,
                imageScale / sprite.rect.height, imageScale / sprite.rect.width);

            // Legacy meshes remain as model-owned objects, but this room uses
            // one complete illustrated tile as its visible presentation.
            foreach (var renderer in room.GetComponentsInChildren<Renderer>(true))
                if (renderer != picture) state.Suppress(renderer);
        }

        internal static void RestoreRoom(GameObject room)
        {
            if (room == null) return;
            var state = room.GetComponent<ConstructionIllustratedRoomSprite>();
            if (state != null) state.Restore();
        }

        internal static void AttachActor(GameObject actor, ActorKind kind)
        {
            if (actor == null || !EnsureSprites()) return;
            var visual = actor.GetComponent<ConstructionIllustratedActorSprite>();
            if (visual == null)
                visual = actor.AddComponent<ConstructionIllustratedActorSprite>();
            var frames = kind switch
            {
                ActorKind.Resident => residentSprites,
                ActorKind.Car => carSprites,
                ActorKind.Pigeon => wildlifeSprites[0],
                ActorKind.Squirrel => wildlifeSprites[1],
                ActorKind.Hedgehog => wildlifeSprites[2],
                _ => wildlifeSprites[3]
            };
            visual.Initialize(frames, kind, ActorHeight);
        }
    }

    // The opening oak belongs to the menu scene as well as the construction
    // view. Restore its renderers when leaving construction.
    public sealed class ConstructionIllustratedRoomSprite : MonoBehaviour
    {
        private readonly Dictionary<Renderer, bool> originalStates = new();

        internal void Suppress(Renderer renderer)
        {
            if (renderer == null) return;
            if (!originalStates.ContainsKey(renderer))
                originalStates.Add(renderer, renderer.enabled);
            renderer.enabled = false;
        }

        internal void Restore()
        {
            var plot = transform.Find("Illustrated construction plot");
            if (plot != null)
            {
                plot.gameObject.SetActive(false);
                Destroy(plot.gameObject);
            }
            foreach (var entry in originalStates)
                if (entry.Key != null) entry.Key.enabled = entry.Value;
            originalStates.Clear();
            Destroy(this);
        }
    }

    // A companion to a moving model actor. Only its picture changes; the
    // existing agent's position, visibility and collision timing are reused.
    public sealed class ConstructionIllustratedActorSprite : MonoBehaviour
    {
        private Sprite[] frames;
        private ConstructionIllustratedVisualStyle.ActorKind kind;
        private SpriteRenderer picture;
        private Renderer[] originalRenderers;
        private Vector3 previousPosition;
        private Vector3 lastHeading = Vector3.right;
        private bool hasPreviousPosition;
        private float worldHeight;

        internal void Initialize(Sprite[] actorFrames,
            ConstructionIllustratedVisualStyle.ActorKind actorKind,
            float height)
        {
            if (picture != null) return;
            frames = actorFrames;
            kind = actorKind;
            worldHeight = height;
            originalRenderers = GetComponentsInChildren<Renderer>(true);
            var objectRoot = new GameObject("Illustrated moving actor");
            objectRoot.layer = 29;
            objectRoot.transform.SetParent(transform, false);
            picture = objectRoot.AddComponent<SpriteRenderer>();
            picture.flipY = true;
            picture.sortingOrder = 1;
            picture.sprite = frames[0];
            var desiredHeight = kind switch
            {
                ConstructionIllustratedVisualStyle.ActorKind.Car => 1.06f,
                ConstructionIllustratedVisualStyle.ActorKind.Resident => 0.98f,
                ConstructionIllustratedVisualStyle.ActorKind.Fox => 0.82f,
                _ => 0.69f
            };
            var sourceHeight = frames[0].rect.height / 100f;
            objectRoot.transform.localScale = Vector3.one *
                desiredHeight / Mathf.Max(0.01f, sourceHeight);
            SuppressOriginalRenderers();
        }

        private void LateUpdate()
        {
            if (picture == null || frames == null) return;
            var visible = originalRenderers == null || originalRenderers.Length == 0;
            if (originalRenderers != null)
                foreach (var renderer in originalRenderers)
                {
                    if (renderer == null) continue;
                    visible |= renderer.gameObject.activeInHierarchy;
                    if (renderer.enabled) renderer.enabled = false;
                }
            picture.enabled = visible;
            if (!visible) return;

            var pictureTransform = picture.transform;
            pictureTransform.position = new Vector3(transform.position.x,
                worldHeight, transform.position.z);
            var displacement = transform.position - previousPosition;
            displacement.y = 0f;
            var moving = hasPreviousPosition &&
                displacement.sqrMagnitude > 0.000001f;
            if (kind == ConstructionIllustratedVisualStyle.ActorKind.Car)
            {
                // Street vehicles already choose a forward axis for each
                // pass. The picture must follow that axis even while stopped.
                var forward = transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.000001f)
                    lastHeading = forward.normalized;
            }
            else if (moving && displacement.sqrMagnitude < 1f)
                lastHeading = displacement.normalized;
            previousPosition = transform.position;
            hasPreviousPosition = true;

            // All wildlife and walking resident frames face image-right. The
            // car's bonnet points diagonally toward image-bottom-left; with
            // flipY and the flat board rotation this is world south-west.
            // Rotate around the board normal so the picture faces travel,
            // without tilting a flat sprite onto its edge.
            var sourceAngle = kind ==
                ConstructionIllustratedVisualStyle.ActorKind.Car ? -135f : 0f;
            var targetAngle = Mathf.Atan2(lastHeading.z, lastHeading.x) *
                Mathf.Rad2Deg;
            pictureTransform.rotation = kind ==
                ConstructionIllustratedVisualStyle.ActorKind.Resident && !moving
                ? Quaternion.Euler(-90f, 0f, 0f)
                : Quaternion.AngleAxis(sourceAngle - targetAngle, Vector3.up) *
                  Quaternion.Euler(-90f, 0f, 0f);
            var frame = kind switch
            {
                ConstructionIllustratedVisualStyle.ActorKind.Resident =>
                    moving ? 1 + Mathf.FloorToInt(Time.unscaledTime * 7f) % 2 : 0,
                ConstructionIllustratedVisualStyle.ActorKind.Car =>
                    Mathf.FloorToInt(Time.unscaledTime * 3f) % frames.Length,
                _ => Mathf.FloorToInt(Time.unscaledTime * 2.5f) % frames.Length
            };
            picture.sprite = frames[frame];
        }

        private void SuppressOriginalRenderers()
        {
            if (originalRenderers == null) return;
            foreach (var renderer in originalRenderers)
                if (renderer != null) renderer.enabled = false;
        }
    }
}
