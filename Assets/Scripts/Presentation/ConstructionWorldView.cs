using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.People;

namespace UrbanWildlifeRooms.Presentation
{
    // Presentation only: the construction model remains the sole source of
    // placement, food and route outcomes. This reuses the original low-poly
    // furniture, sketch material and animated characters in the new mode.
    public sealed class ConstructionWorldView : MonoBehaviour
    {
        // Unity's existing room kit uses 3.1 world units per cell. The 4.3 cm
        // physical tile dimension is not a Unity scene-space measurement.
        private const float Pitch = WorldScaleStandards.CellSizeMeters;
        private const float PrototypeToLegacyScale = Pitch / 4.3f;
        private static readonly Vector3[] SquirrelOffsets =
        {
            new(0.60f, 0.23f, -0.70f), new(-0.56f, 0.23f, 0.48f),
            new(0.48f, 0.23f, 0.52f), new(-0.49f, 0.23f, -0.42f)
        };
        private static readonly Vector3[] PigeonOffsets =
        {
            new(-0.54f, 0.23f, 0.45f), new(0.52f, 0.23f, -0.32f),
            new(-0.38f, 0.23f, -0.52f), new(0.39f, 0.23f, 0.54f)
        };
        private readonly Dictionary<string, GameObject> rooms = new();
        private readonly Dictionary<string, OakTreeStageVisual> growthVisuals = new();
        private readonly Dictionary<string, int> growthStages = new();
        private Transform world;
        private GameObject boardBase;
        private Camera boardCamera;
        private Material surface;
        private Material tabletopMaterial;
        private Transform tabletop;
        private Vector3 cameraOffset;
        private SquirrelDemoVisual squirrel;
        private PigeonDemoVisual pigeon;
        private HedgehogDemoVisual hedgehog;
        private FoxDemoVisual fox;
        private SquirrelDemoVisual openingSquirrelVisual;
        private const int MaxVisibleAnimalsPerSpecies = 8;
        private readonly List<SquirrelDemoVisual> squirrels = new();
        private readonly List<PigeonDemoVisual> pigeons = new();
        private readonly Dictionary<Transform, Vector3> animalHomes = new();
        private readonly HashSet<Transform> travelingAnimals = new();
        private readonly List<Coroutine> foragingTrips = new();
        private Transform travelingFox;
        private float foxContactUntil;
        private GameObject transientFoxPrey;
        private bool transientFoxPreyIsGhost;
        private readonly Dictionary<string, CitizenDemoVisual> residents = new();
        private readonly Dictionary<CitizenDemoVisual, Vector3> commutingHomes = new();
        private readonly List<Coroutine> commuteTrips = new();
        private CitizenDemoVisual arrivingResident;
        private Vector3 arrivingHomePosition;
        private Coroutine pigeonArrival;
        private Coroutine foxHunt;
        private Coroutine residentArrival;
        private bool usingOpeningBoard;
        private GameObject caseHardware;
        private Coroutine entranceCameraMotion;
        private Coroutine entranceRoomReveal;

        public bool IsReady => world != null && boardCamera != null;
        public Transform BoardFoundation => boardBase == null ? null : boardBase.transform;
        public bool UsesOpeningBoard => usingOpeningBoard;
        public bool IsEntranceCameraMoving => entranceCameraMotion != null;

        public void Initialize(Camera camera, RectTransform boardArea,
            float cellSpacing)
        {
            if (world != null || camera == null || boardArea == null ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            surface = UrbanVisualFactory.CreateSurfaceMaterial();
            world = new GameObject("Construction low-poly world").transform;
            world.SetParent(transform, false);
            world.localPosition = Vector3.zero;
            boardCamera = camera;
            var cameraStartPosition = camera.transform.position;
            var cameraStartRotation = camera.transform.rotation;
            var cameraStartSize = camera.orthographicSize;
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            boardArea.GetWorldCorners(corners);
            var lower = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var upper = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            var screenCentre = (lower + upper) * 0.5f;
            var canvas = boardArea.GetComponentInParent<Canvas>();
            var spacingPixels = cellSpacing * canvas.rootCanvas.scaleFactor;
            var gameplaySize = Pitch * Screen.height /
                (2f * Mathf.Max(1f, spacingPixels));
            cameraOffset = new Vector3(
                (0.5f - screenCentre.x / Screen.width) *
                    2f * gameplaySize * camera.aspect,
                42f,
                (0.5f - screenCentre.y / Screen.height) *
                    2f * gameplaySize);
            var opening = FindFirstObjectByType<ConstructionOpeningBoardPreview>();
            usingOpeningBoard = opening != null && opening.IsPreviewing &&
                opening.BoardCamera == camera && opening.Foundation != null &&
                opening.OpeningOak?.VisualRoot != null;
            if (!usingOpeningBoard)
            {
                camera.transform.position = cameraOffset;
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                camera.orthographicSize = gameplaySize;
                camera.orthographic = true;
            }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.93f, 0.89f, 0.79f);
            camera.targetTexture = null;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 180f;
            if (usingOpeningBoard)
            {
                // Both construction modes continue on the exact board object
                // seen in the opening, rather than a second scene's facsimile.
                boardBase = opening.Foundation.gameObject;
                tabletop = opening.Tabletop;
                entranceCameraMotion = StartCoroutine(MoveOpeningCamera(
                    cameraStartPosition, cameraStartRotation, cameraStartSize,
                    gameplaySize));
                var starter = opening.OpeningOak.VisualRoot.gameObject;
                rooms[ConstructionBoardModel.StarterOakId] = starter;
                var growth = starter.GetComponentInChildren<OakTreeStageVisual>(true);
                if (growth != null)
                    growthVisuals[ConstructionBoardModel.StarterOakId] = growth;
                openingSquirrelVisual = opening.OpeningSquirrel == null ? null :
                    opening.OpeningSquirrel.GetComponent<SquirrelDemoVisual>();
                // The old simulation may have hidden this animal after a
                // saved death. Construction has its own population model, so
                // use a fresh visual from the same prefab instead of sharing
                // the legacy animal's vitality and renderer state.
                openingSquirrelVisual?.SetVisible(false);
            }
            else
            {
                BuildTabletop();
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.82f, 0.80f, 0.78f);
                var lightObject = new GameObject("Construction board light");
                lightObject.transform.SetParent(transform, false);
                lightObject.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.05f;
                light.cullingMask = 1 << 29;
                BuildBoardFoundation();
            }
            BuildBoardCaseHardware();
        }

        private IEnumerator MoveOpeningCamera(Vector3 fromPosition,
            Quaternion fromRotation, float fromSize, float toSize)
        {
            var initialTabletopPosition = tabletop == null ? Vector3.zero : tabletop.position;
            var initialTabletopScale = tabletop == null ? Vector3.one : tabletop.localScale;
            var finalTabletopPosition = new Vector3(cameraOffset.x, -0.58f,
                cameraOffset.z);
            var finalTabletopScale = new Vector3(
                toSize * boardCamera.aspect * 2f * 1.22f / 10f,
                1f, toSize * 2f * 1.22f / 10f);
            const float duration = 0.85f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var eased = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / duration));
                boardCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, cameraOffset, eased),
                    Quaternion.Slerp(fromRotation, Quaternion.Euler(90f, 0f, 0f), eased));
                boardCamera.orthographicSize = Mathf.Lerp(fromSize, toSize, eased);
                if (tabletop != null)
                {
                    tabletop.position = Vector3.Lerp(initialTabletopPosition,
                        finalTabletopPosition, eased);
                    tabletop.localScale = Vector3.Lerp(initialTabletopScale,
                        finalTabletopScale, eased);
                }
                yield return null;
            }
            entranceCameraMotion = null;
        }

        public void RevealRestoredRooms()
        {
            if (!usingOpeningBoard || rooms.Count <= 1) return;
            if (entranceRoomReveal != null) StopCoroutine(entranceRoomReveal);
            entranceRoomReveal = StartCoroutine(RevealRooms());
        }

        private IEnumerator RevealRooms()
        {
            var arrivals = new List<Transform>();
            var scales = new List<Vector3>();
            foreach (var entry in rooms)
            {
                if (entry.Key == ConstructionBoardModel.StarterOakId || entry.Value == null)
                    continue;
                arrivals.Add(entry.Value.transform);
                scales.Add(entry.Value.transform.localScale);
                entry.Value.transform.localScale = Vector3.zero;
            }
            const float stagger = 0.065f;
            const float growth = 0.42f;
            var duration = growth + Mathf.Max(0, arrivals.Count - 1) * stagger;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (var i = 0; i < arrivals.Count; i++)
                    if (arrivals[i] != null)
                    {
                        var progress = Mathf.SmoothStep(0f, 1f,
                            Mathf.Clamp01((elapsed - i * stagger) / growth));
                        arrivals[i].localScale = scales[i] * progress;
                    }
                yield return null;
            }
            for (var i = 0; i < arrivals.Count; i++)
                if (arrivals[i] != null) arrivals[i].localScale = scales[i];
            entranceRoomReveal = null;
        }

        private void BuildBoardCaseHardware()
        {
            if (boardBase == null || caseHardware != null) return;
            const float mapSize = 7f * Pitch;
            var half = mapSize * 0.5f;
            caseHardware = new GameObject("Folding board-case hardware");
            caseHardware.transform.SetParent(boardBase.transform, false);
            // These sit below and just outside the original foundation; no
            // room mesh, empty plot or click collider is replaced.
            Box("Deep navy case tray", caseHardware.transform,
                new Vector3(0f, -0.46f, 0f),
                new Vector3(mapSize + 1.4f, 0.44f, mapSize + 1.4f),
                UrbanPalette.Boundary);
            foreach (var side in new[] { -1f, 1f })
            {
                Box("Case edge inlay", caseHardware.transform,
                    new Vector3(side * (half + 0.42f), -0.13f, 0f),
                    new Vector3(0.09f, 0.09f, mapSize + 1.3f),
                    UrbanPalette.Doorframe);
                foreach (var hingeZ in new[] { -6.2f, 0f, 6.2f })
                    Box("Brass case hinge", caseHardware.transform,
                        new Vector3(side * (half + 0.5f), 0.05f, hingeZ),
                        new Vector3(0.42f, 0.22f, 1.02f),
                        UrbanPalette.Doorframe);
            }
            Box("Front case clasp", caseHardware.transform,
                new Vector3(0f, 0.02f, -half - 0.5f),
                new Vector3(1.2f, 0.19f, 0.46f), UrbanPalette.Doorframe);
            SetLayer(caseHardware, 29);
        }

        public void SetViewport(int minColumn, int minRow)
        {
            if (!IsReady || boardCamera == null) return;
            var centre = Cell(minColumn + 3, minRow + 3);
            boardCamera.transform.position = cameraOffset +
                new Vector3(centre.x, 0f, centre.z);
            if (boardBase != null)
                boardBase.transform.localPosition = centre;
            if (tabletop != null)
                tabletop.position = new Vector3(boardCamera.transform.position.x,
                    -0.58f, boardCamera.transform.position.z);
        }

        private void BuildTabletop()
        {
            var texture = Resources.Load<Texture2D>(
                "UI/GameplayHud/gameplay-tabletop-backdrop-v01");
            if (texture == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            tabletopMaterial = new Material(shader) { mainTexture = texture };
            if (tabletopMaterial.HasProperty("_BaseColor"))
                tabletopMaterial.SetColor("_BaseColor", Color.white);
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Gameplay Tabletop Backdrop";
            plane.transform.SetParent(transform, false);
            tabletop = plane.transform;
            tabletop.position = new Vector3(cameraOffset.x, -0.58f, cameraOffset.z);
            tabletop.rotation = Quaternion.Euler(0f, 180f, 0f);
            var halfHeight = boardCamera.orthographicSize;
            tabletop.localScale = new Vector3(
                halfHeight * boardCamera.aspect * 2f * 1.22f / 10f,
                1f, halfHeight * 2f * 1.22f / 10f);
            var renderer = plane.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = tabletopMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var collider = plane.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }

        private void BuildBoardFoundation()
        {
            // Same board dimensions, palette, slot spacing and rim detail as
            // the opening scene's BuildFixedBoardFoundation.
            const float mapSize = 7f * Pitch;
            boardBase = new GameObject("Board base");
            boardBase.transform.SetParent(world, false);
            Box("Paper Board", boardBase.transform,
                new Vector3(0f, -0.18f, 0f),
                new Vector3(mapSize + 1.25f, 0.28f, mapSize + 1.25f),
                UrbanPalette.Boundary);
            var slotSize = Pitch - 0.28f;
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                var slot = Box($"Empty plot {column + 1},{row + 1}", boardBase.transform,
                    Cell(column, row) + new Vector3(0f, -0.025f, 0f),
                    new Vector3(slotSize, 0.08f, slotSize),
                    (column + row) % 2 == 0
                        ? UrbanPalette.BoardSlot : UrbanPalette.BoardSlotAlternate);
                var face = Box("Pastel plot face", slot.transform,
                    new Vector3(0f, 0.54f, 0f),
                    new Vector3(0.94f, 0.20f, 0.94f),
                    UrbanPalette.BoardSlotTop);
                UrbanVisualFactory.SetInkStrength(face.GetComponent<Renderer>(), 0f);
            }
            var rimOffset = mapSize * 0.5f + 0.34f;
            var inlayOffset = mapSize * 0.5f + 0.20f;
            var woodOffset = mapSize * 0.5f + 0.59f;
            var cornerOffset = mapSize * 0.5f + 0.48f;
            foreach (var side in new[] { -1f, 1f })
            {
                Box(side < 0 ? "West Board Rim" : "East Board Rim",
                    boardBase.transform, new Vector3(side * rimOffset, 0.04f, 0f),
                    new Vector3(0.34f, 0.22f, mapSize + 1.02f),
                    UrbanPalette.Boundary);
                Box(side < 0 ? "South Board Rim" : "North Board Rim",
                    boardBase.transform, new Vector3(0f, 0.04f, side * rimOffset),
                    new Vector3(mapSize + 1.02f, 0.22f, 0.34f),
                    UrbanPalette.Boundary);
                Box("Wooden case edge", boardBase.transform,
                    new Vector3(0f, 0.04f, side * woodOffset),
                    new Vector3(mapSize + 1.35f, 0.22f, 0.16f), UrbanPalette.BoardFrameWood);
                Box("Wooden case edge", boardBase.transform,
                    new Vector3(side * woodOffset, 0.04f, 0f),
                    new Vector3(0.16f, 0.22f, mapSize + 1.35f), UrbanPalette.BoardFrameWood);
                Box("Brass inner rule", boardBase.transform,
                    new Vector3(0f, 0.18f, side * inlayOffset),
                    new Vector3(mapSize + 0.48f, 0.035f, 0.055f), UrbanPalette.BoardFrameBrass);
                Box("Brass inner rule", boardBase.transform,
                    new Vector3(side * inlayOffset, 0.18f, 0f),
                    new Vector3(0.055f, 0.035f, mapSize + 0.48f), UrbanPalette.BoardFrameBrass);
                foreach (var end in new[] { -1f, 1f })
                    Box("Brass corner plate", boardBase.transform,
                        new Vector3(side * cornerOffset, 0.18f, end * cornerOffset),
                        new Vector3(0.26f, 0.055f, 0.26f), UrbanPalette.BoardFrameBrass);
            }
            SetLayer(boardBase, 29);
        }

        public void Sync(ConstructionRunModel run, bool animateGrowth)
        {
            if (!IsReady || run == null) return;
            foreach (var tile in run.BuiltTiles)
            {
                if (!rooms.ContainsKey(tile.id)) AddRoom(tile);
                if (tile.category != ConstructionCategory.Green) continue;
                var stage = run.GreenGrowthStage(tile.id);
                if (growthVisuals.TryGetValue(tile.id, out var growth))
                {
                    if (!growthStages.TryGetValue(tile.id, out var previous))
                        growth.SetStage(ToOakStage(stage));
                    else if (stage != previous)
                    {
                        if (animateGrowth && stage > previous)
                            growth.AnimateGrowthTo(ToOakStage(stage));
                        else growth.SetStage(ToOakStage(stage));
                    }
                }
                growthStages[tile.id] = stage;
            }
            SyncActors(run);
        }

        public void ClearBuiltWorld()
        {
            // A new construction run keeps this view object alive. Remove the
            // previous run's meshes before Sync creates its starter oak again.
            StopForaging();
            if (foxHunt != null) StopCoroutine(foxHunt);
            HideTransientFoxPrey();
            StopCommuting();
            if (pigeonArrival != null) StopCoroutine(pigeonArrival);
            if (residentArrival != null) StopCoroutine(residentArrival);
            foxHunt = null;
            travelingFox = null;
            foxContactUntil = 0f;
            pigeonArrival = null;
            residentArrival = null;
            arrivingResident = null;
            for (var index = 1; index < squirrels.Count; index++)
                if (squirrels[index] != null)
                {
                    squirrels[index].gameObject.SetActive(false);
                    Destroy(squirrels[index].gameObject);
                }
            squirrels.Clear();
            if (squirrel != null)
                squirrels.Add(squirrel);
            animalHomes.Clear();
            foreach (var entry in rooms)
            {
                if (usingOpeningBoard && entry.Key == ConstructionBoardModel.StarterOakId)
                    continue;
                var room = entry.Value;
                if (room == null) continue;
                room.SetActive(false);
                Destroy(room);
            }
            rooms.Clear();
            growthVisuals.Clear();
            growthStages.Clear();
            if (usingOpeningBoard)
            {
                var opening = FindFirstObjectByType<ConstructionOpeningBoardPreview>();
                if (opening?.OpeningOak?.VisualRoot != null)
                {
                    var starter = opening.OpeningOak.VisualRoot.gameObject;
                    rooms[ConstructionBoardModel.StarterOakId] = starter;
                    var growth = starter.GetComponentInChildren<OakTreeStageVisual>(true);
                    if (growth != null)
                        growthVisuals[ConstructionBoardModel.StarterOakId] = growth;
                }
            }
            foreach (var bird in pigeons)
            {
                if (bird == null) continue;
                bird.gameObject.SetActive(false);
                Destroy(bird.gameObject);
            }
            pigeons.Clear();
            pigeon = null;
            if (hedgehog != null)
            {
                hedgehog.gameObject.SetActive(false);
                Destroy(hedgehog.gameObject);
                hedgehog = null;
            }
            if (fox != null)
            {
                fox.gameObject.SetActive(false);
                Destroy(fox.gameObject);
                fox = null;
            }
            foreach (var resident in residents.Values)
            {
                if (resident == null) continue;
                resident.gameObject.SetActive(false);
                Destroy(resident.gameObject);
            }
            residents.Clear();
            if (squirrel != null)
            {
                squirrel.transform.position = Cell(3, 3) +
                    new Vector3(0.6f, 0.23f, -0.7f);
                animalHomes[squirrel.transform] = squirrel.transform.position;
            }
        }

        public void PlayForaging(IReadOnlyList<ConstructionTileData> squirrelRoute,
            IReadOnlyList<ConstructionTileData> pigeonRoute)
            => PlayForaging(
                squirrelRoute == null ? null :
                    new IReadOnlyList<ConstructionTileData>[] { squirrelRoute },
                pigeonRoute == null ? null :
                    new IReadOnlyList<ConstructionTileData>[] { pigeonRoute });

        public void PlayForaging(
            IReadOnlyList<IReadOnlyList<ConstructionTileData>> squirrelRoutes,
            IReadOnlyList<IReadOnlyList<ConstructionTileData>> pigeonRoutes)
        {
            if (!IsReady) return;
            StopForaging();
            if (foxHunt != null) StopCoroutine(foxHunt);
            HideTransientFoxPrey();
            foxHunt = null;
            travelingFox = null;
            foxContactUntil = 0f;
            StartForagingTrips(squirrels, squirrelRoutes, false);
            StartForagingTrips(pigeons, pigeonRoutes, true);
        }

        public void PlayPigeonArrival()
        {
            if (!IsReady || pigeon == null || !pigeon.gameObject.activeSelf) return;
            if (pigeonArrival != null) StopCoroutine(pigeonArrival);
            pigeonArrival = StartCoroutine(AnimatePigeonArrival(pigeon.transform));
        }

        public void PlayFoxHunt(IReadOnlyList<string> routeTileIds,
            IReadOnlyList<ConstructionTileData> builtTiles,
            ConstructionPreySpecies preySpecies)
        {
            if (!IsReady || fox == null || !fox.gameObject.activeInHierarchy ||
                routeTileIds == null || routeTileIds.Count < 2) return;
            var byId = new Dictionary<string, ConstructionTileData>();
            foreach (var tile in builtTiles) byId[tile.id] = tile;
            var route = new List<ConstructionTileData>();
            foreach (var id in routeTileIds)
                if (byId.TryGetValue(id, out var tile)) route.Add(tile);
            if (route.Count < 2) return;
            if (foxHunt != null) StopCoroutine(foxHunt);
            HideTransientFoxPrey();
            RevealFoxPrey(preySpecies, route[route.Count - 1]);
            foxHunt = StartCoroutine(AnimateFoxHunt(route));
        }

        private void RevealFoxPrey(ConstructionPreySpecies species,
            ConstructionTileData target)
        {
            var preyHome = Cell(target.column, target.row) +
                (species switch
                {
                    ConstructionPreySpecies.Squirrel => SquirrelOffsets[0],
                    ConstructionPreySpecies.Pigeon => PigeonOffsets[0],
                    _ => new Vector3(-0.48f, 0.23f, 0.42f)
                });
            switch (species)
            {
                case ConstructionPreySpecies.Squirrel:
                    for (var index = squirrels.Count - 1; index >= 0; index--)
                        if (squirrels[index] != null &&
                            !squirrels[index].gameObject.activeSelf)
                        {
                            transientFoxPrey = squirrels[index].gameObject;
                            break;
                        }
                    break;
                case ConstructionPreySpecies.Pigeon:
                    for (var index = pigeons.Count - 1; index >= 0; index--)
                        if (pigeons[index] != null &&
                            !pigeons[index].gameObject.activeSelf)
                        {
                            transientFoxPrey = pigeons[index].gameObject;
                            break;
                        }
                    break;
                case ConstructionPreySpecies.Hedgehog when hedgehog != null &&
                    !hedgehog.gameObject.activeSelf:
                    transientFoxPrey = hedgehog.gameObject;
                    break;
            }
            if (transientFoxPrey == null)
            {
                var actor = NewActor("Fox hunt prey", world, preyHome);
                switch (species)
                {
                    case ConstructionPreySpecies.Squirrel:
                        actor.AddComponent<SquirrelDemoVisual>()
                            .Initialize(surface, HideFlags.None);
                        break;
                    case ConstructionPreySpecies.Pigeon:
                        actor.AddComponent<PigeonDemoVisual>()
                            .Initialize(surface, HideFlags.None);
                        break;
                    case ConstructionPreySpecies.Hedgehog:
                        actor.AddComponent<HedgehogDemoVisual>()
                            .Initialize(surface, HideFlags.None);
                        break;
                    default:
                        Destroy(actor);
                        return;
                }
                SetLayer(actor, 29);
                transientFoxPrey = actor;
                transientFoxPreyIsGhost = true;
            }
            else
            {
                transientFoxPrey.transform.position = preyHome;
                animalHomes[transientFoxPrey.transform] = preyHome;
            }
            transientFoxPrey.SetActive(true);
        }

        private void HideTransientFoxPrey()
        {
            if (transientFoxPrey != null)
            {
                transientFoxPrey.SetActive(false);
                if (transientFoxPreyIsGhost) Destroy(transientFoxPrey);
            }
            transientFoxPrey = null;
            transientFoxPreyIsGhost = false;
        }

        private IEnumerator AnimateFoxHunt(IReadOnlyList<ConstructionTileData> route)
        {
            travelingFox = fox.transform;
            var home = animalHomes[travelingFox];
            var preyPosition = transientFoxPrey == null
                ? Cell(route[route.Count - 1].column,
                    route[route.Count - 1].row) +
                  new Vector3(0.55f, 0.23f, -0.5f)
                : transientFoxPrey.transform.position;
            for (var index = 1; index < route.Count; index++)
                yield return MoveBetween(travelingFox, travelingFox.position,
                    index == route.Count - 1 ? preyPosition :
                        Cell(route[index].column, route[index].row) +
                        new Vector3(0.25f, 0.23f, -0.18f), 0.32f);
            HideTransientFoxPrey();
            foxContactUntil = Time.unscaledTime + 0.2f;
            yield return new WaitForSecondsRealtime(0.2f);
            for (var index = route.Count - 2; index >= 0; index--)
                yield return MoveBetween(travelingFox, travelingFox.position,
                    index == 0 ? home : Cell(route[index].column,
                        route[index].row) + new Vector3(0.25f, 0.23f, -0.18f),
                    0.32f);
            travelingFox = null;
            foxHunt = null;
        }

        private IEnumerator AnimatePigeonArrival(Transform actor)
        {
            var landing = actor.localPosition;
            var fullScale = actor.localScale;
            var approach = landing + new Vector3(-0.9f, 2.1f, 0.8f);
            actor.localPosition = approach;
            actor.localScale = fullScale * 0.72f;
            yield return null;
            const float duration = 0.8f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                var progress = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / duration));
                actor.localPosition = Vector3.Lerp(approach, landing, progress);
                actor.localScale = Vector3.Lerp(fullScale * 0.72f,
                    fullScale, progress);
                yield return null;
            }
            actor.localPosition = landing;
            actor.localScale = fullScale;
            pigeonArrival = null;
        }

        public void PlayResidentCommute(IReadOnlyList<string> routeTileIds,
            IReadOnlyList<ConstructionTileData> builtTiles)
            => PlayResidentCommutes(new[] { new ConstructionResidentDayResult
            {
                routeTileIds = routeTileIds == null ? new List<string>() :
                    new List<string>(routeTileIds),
                worked = true
            } }, builtTiles);

        public void PlayResidentCommutes(
            IReadOnlyList<ConstructionResidentDayResult> journeys,
            IReadOnlyList<ConstructionTileData> builtTiles)
        {
            if (!IsReady || journeys == null || builtTiles == null) return;
            var byId = new Dictionary<string, ConstructionTileData>();
            foreach (var tile in builtTiles) byId[tile.id] = tile;
            if (residentArrival != null)
            {
                StopCoroutine(residentArrival);
                residentArrival = null;
                if (arrivingResident != null)
                    arrivingResident.transform.localPosition = arrivingHomePosition;
                arrivingResident = null;
            }
            StopCommuting();
            foreach (var journey in journeys)
            {
                if (journey == null || (!journey.worked && !journey.ate) ||
                    journey.routeTileIds == null ||
                    journey.routeTileIds.Count < 2 ||
                    !residents.TryGetValue(journey.homeTileId ??
                        journey.routeTileIds[0], out var resident) ||
                    resident == null || !resident.gameObject.activeSelf)
                    continue;
                var route = new List<ConstructionTileData>();
                foreach (var id in journey.routeTileIds)
                    if (byId.TryGetValue(id, out var tile)) route.Add(tile);
                if (route.Count != journey.routeTileIds.Count || route.Count < 2)
                    continue;
                commutingHomes[resident] = resident.transform.localPosition;
                commuteTrips.Add(StartCoroutine(Commute(resident, route)));
            }
        }

        public void PlayResidentArrival(string homeTileId)
        {
            if (!IsReady || !residents.TryGetValue(homeTileId, out var resident) ||
                resident == null || !resident.gameObject.activeSelf) return;
            if (residentArrival != null)
            {
                StopCoroutine(residentArrival);
                if (arrivingResident != null)
                    arrivingResident.transform.localPosition = arrivingHomePosition;
                arrivingResident = null;
            }
            arrivingResident = resident;
            arrivingHomePosition = resident.transform.localPosition;
            residentArrival = StartCoroutine(AnimateResidentArrival(resident.transform));
        }

        private IEnumerator AnimateResidentArrival(Transform actor)
        {
            var home = actor.localPosition;
            var entrance = home + new Vector3(-1.1f, 0f, 0.55f);
            actor.localPosition = entrance;
            yield return null;
            const float duration = 0.85f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                actor.localPosition = Vector3.Lerp(entrance, home,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            actor.localPosition = home;
            residentArrival = null;
            arrivingResident = null;
        }

        private void AddRoom(ConstructionTileData tile)
        {
            var type = RoomTypeFor(tile);
            var root = new GameObject($"{tile.id} {type}");
            root.transform.SetParent(world, false);
            root.transform.localPosition = Cell(tile.column, tile.row);
            root.transform.localScale = Vector3.one * PrototypeToLegacyScale;
            SetLayer(root, 29);
            rooms.Add(tile.id, root);
            Box("Room floor", root.transform, new Vector3(0f, 0.12f, 0f),
                new Vector3(4.08f, 0.24f, 4.08f), UrbanPalette.ForRoom(type));
            ConstructionPlotFrameVisual.BuildFrame(root.transform, surface);
            if (tile.category == ConstructionCategory.Green)
                AddGreen(root.transform, tile);
            else if (tile.category == ConstructionCategory.Street)
                AddStreet(root.transform);
            else if (tile.category == ConstructionCategory.Square)
                AddSquare(root.transform);
            else if (tile.category == ConstructionCategory.Waste)
            {
                var waste = root.AddComponent<WasteRoomLoadVisual>();
                waste.Initialize(surface, HideFlags.None);
            }
            else
            {
                var spec = new RoomSpec(tile.id, type.ToString(), type,
                    tile.column, tile.row, 1, 1, false, string.Empty);
                RoomInteriorVisualBuilder.Build(root.transform, spec,
                    4.08f, 4.08f, surface, HideFlags.None);
            }
            SetLayer(root, 29);
        }

        private void AddStreet(Transform parent)
        {
            var stone = Color.Lerp(UrbanPalette.Board, Color.white, 0.12f);
            Box("Street north-south paving", parent,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(1.24f, 0.022f, 3.83f), stone);
            Box("Street east-west paving", parent,
                new Vector3(0f, 0.26f, 0f),
                new Vector3(3.83f, 0.022f, 1.24f), stone);
        }

        private void AddSquare(Transform parent)
        {
            Box("Square stone paving", parent,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(3.78f, 0.025f, 3.78f), UrbanPalette.Board);
            Box("Square planted bed", parent,
                new Vector3(-1.10f, 0.29f, 1.10f),
                new Vector3(0.82f, 0.10f, 0.82f), UrbanPalette.Doorframe);
            Bush(parent, new Vector3(-1.10f, 0.47f, 1.10f), 0.62f);
            Box("Square bench seat", parent,
                new Vector3(0.95f, 0.47f, -1.05f),
                new Vector3(1.02f, 0.10f, 0.34f), new Color(0.54f, 0.34f, 0.20f));
            Box("Square bench back", parent,
                new Vector3(0.95f, 0.64f, -1.20f),
                new Vector3(1.02f, 0.29f, 0.08f), new Color(0.43f, 0.27f, 0.18f));
        }

        private void AddGreen(Transform parent, ConstructionTileData tile)
        {
            var stages = new GameObject[3];
            for (var stage = 0; stage < stages.Length; stage++)
            {
                var root = new GameObject($"Green growth stage {stage}");
                root.transform.SetParent(parent, false);
                stages[stage] = root;
                if (tile.planting == GreenPlanting.Oak)
                {
                    LowPolyTreeVisualBuilder.Build(root.transform,
                        new Vector3(-0.20f, 0f, 0.18f),
                        stage == 0 ? 0.46f : stage == 1 ? 0.80f : 1.25f,
                        stage < 2, surface, HideFlags.None);
                    Bush(root.transform, new Vector3(1.12f, 0.40f, -1.02f),
                        stage == 0 ? 0.25f : 0.39f);
                }
                else
                {
                    var shrub = tile.planting == GreenPlanting.Shrub;
                    var count = shrub ? 1 + stage * 2 : 2 + stage * 3;
                    for (var index = 0; index < count; index++)
                    {
                        var x = -1.18f + index % 3 * 1.12f;
                        var z = -0.95f + index / 3 * 1.02f;
                        Bush(root.transform, new Vector3(x, 0.34f, z),
                            shrub ? 0.48f + stage * 0.12f : 0.20f + stage * 0.05f);
                    }
                }
            }
            var visual = parent.gameObject.AddComponent<OakTreeStageVisual>();
            visual.Initialize(null, stages[0], stages[1], stages[2],
                OakTreeStage.Sapling);
            growthVisuals[tile.id] = visual;
        }

        private void SyncActors(ConstructionRunModel run)
        {
            if (squirrel == null)
            {
                var actor = NewActor("Squirrel", world, new Vector3(0.6f, 0.23f, -0.7f));
                squirrel = actor.AddComponent<SquirrelDemoVisual>();
                squirrel.Initialize(surface, HideFlags.None);
                SetLayer(actor, 29);
            }
            if (squirrels.Count == 0) squirrels.Add(squirrel);
            var squirrelCount = Mathf.Min(run.SquirrelPopulation,
                MaxVisibleAnimalsPerSpecies);
            while (squirrels.Count < squirrelCount)
            {
                var actor = NewActor($"Squirrel {squirrels.Count + 1}", world,
                    Vector3.zero);
                var visual = actor.AddComponent<SquirrelDemoVisual>();
                visual.Initialize(surface, HideFlags.None);
                SetLayer(actor, 29);
                squirrels.Add(visual);
            }
            var oakTiles = System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.OrderBy(
                    System.Linq.Enumerable.Where(run.BuiltTiles, tile =>
                        tile.category == ConstructionCategory.Green &&
                        tile.planting == GreenPlanting.Oak),
                    tile => tile.id == ConstructionBoardModel.StarterOakId ? 0 : 1));
            if (oakTiles.Length == 0) return;
            for (var index = 0; index < squirrels.Count; index++)
            {
                var visual = squirrels[index];
                if (visual == null) continue;
                var active = index < squirrelCount ||
                    visual.gameObject == transientFoxPrey;
                visual.gameObject.SetActive(active);
                if (!active) continue;
                visual.SetVisible(true);
                if (visual.gameObject == transientFoxPrey) continue;
                var tile = oakTiles[index % oakTiles.Length];
                var home = Cell(tile.column, tile.row) +
                    AnimalOffset(index, false);
                animalHomes[visual.transform] = home;
                if (!travelingAnimals.Contains(visual.transform))
                    visual.transform.position = home;
            }
            var pigeonTile = run.BuiltTiles == null ? null :
                System.Linq.Enumerable.FirstOrDefault(run.BuiltTiles,
                    tile => tile.id == run.PigeonTileId);
            var pigeonCount = pigeonTile == null ? 0 : Mathf.Min(
                run.PigeonPopulation, MaxVisibleAnimalsPerSpecies);
            while (pigeons.Count < pigeonCount)
            {
                var actor = NewActor(pigeons.Count == 0 ? "Pigeon flock" :
                    $"Pigeon {pigeons.Count + 1}", world, Vector3.zero);
                var visual = actor.AddComponent<PigeonDemoVisual>();
                visual.Initialize(surface, HideFlags.None);
                SetLayer(actor, 29);
                pigeons.Add(visual);
                if (pigeon == null) pigeon = visual;
            }
            for (var index = 0; index < pigeons.Count; index++)
            {
                var visual = pigeons[index];
                if (visual == null) continue;
                var active = index < pigeonCount ||
                    visual.gameObject == transientFoxPrey;
                visual.gameObject.SetActive(active);
                if (!active) continue;
                visual.SetVisible(true);
                if (visual.gameObject == transientFoxPrey) continue;
                var home = Cell(pigeonTile.column, pigeonTile.row) +
                    AnimalOffset(index, true);
                animalHomes[visual.transform] = home;
                if (!travelingAnimals.Contains(visual.transform) &&
                    (visual != pigeon || pigeonArrival == null))
                    visual.transform.position = home;
            }
            var hedgehogTile = run.HedgehogHabitat;
            if (hedgehogTile != null && hedgehog == null)
            {
                var actor = NewActor("Hedgehog visitor", world, Vector3.zero);
                hedgehog = actor.AddComponent<HedgehogDemoVisual>();
                hedgehog.Initialize(surface, HideFlags.None);
                SetLayer(actor, 29);
            }
            if (hedgehog != null)
            {
                hedgehog.gameObject.SetActive(hedgehogTile != null ||
                    hedgehog.gameObject == transientFoxPrey);
                if (hedgehogTile != null)
                {
                    var home = Cell(hedgehogTile.column, hedgehogTile.row) +
                        new Vector3(-0.48f, 0.23f, 0.42f);
                    animalHomes[hedgehog.transform] = home;
                    hedgehog.transform.position = home;
                }
            }
            var foxTile = run.FoxHabitat;
            if (foxTile != null && fox == null)
            {
                var actor = NewActor("Fox visitor", world, Vector3.zero);
                fox = actor.AddComponent<FoxDemoVisual>();
                fox.Initialize(surface, HideFlags.None);
                SetLayer(actor, 29);
            }
            if (fox != null)
            {
                fox.gameObject.SetActive(foxTile != null);
                if (foxTile != null)
                {
                    var home = Cell(foxTile.column, foxTile.row) +
                        new Vector3(0.46f, 0.23f, -0.36f);
                    animalHomes[fox.transform] = home;
                    if (fox.transform != travelingFox)
                        fox.transform.position = home;
                }
            }
            foreach (var home in run.BuiltTiles)
            {
                if (home.category != ConstructionCategory.Residence) continue;
                if (!residents.TryGetValue(home.id, out var resident) || resident == null)
                {
                    var actor = NewActor(home.id == run.FirstResidentHomeTileId
                            ? "First resident" : $"Resident {home.id}",
                        world, ResidentPosition(home.column, home.row));
                    resident = actor.AddComponent<CitizenDemoVisual>();
                    resident.Initialize(surface, HideFlags.None);
                    SetLayer(actor, 29);
                    residents[home.id] = resident;
                }
                resident.gameObject.SetActive(run.HasResident(home.id));
            }
        }

        private void StartForagingTrips<T>(IReadOnlyList<T> actors,
            IReadOnlyList<IReadOnlyList<ConstructionTileData>> routes, bool bird)
            where T : MonoBehaviour
        {
            if (routes == null) return;
            for (var index = 0; index < actors.Count && index < routes.Count; index++)
            {
                var actor = actors[index];
                var route = routes[index];
                if (actor == null || !actor.gameObject.activeInHierarchy ||
                    route == null || route.Count == 0) continue;
                travelingAnimals.Add(actor.transform);
                foragingTrips.Add(StartCoroutine(ForagingTrip(actor.transform,
                    route, index, bird)));
            }
        }

        private IEnumerator ForagingTrip(Transform actor,
            IReadOnlyList<ConstructionTileData> route, int index, bool bird)
        {
            // One visible journey per meal recorded by the day model. The
            // presentation never decides whether an animal ate.
            yield return TravelAnimal(actor, route, bird ? 0.85f : 0.65f,
                index, bird);
            travelingAnimals.Remove(actor);
        }

        private IEnumerator TravelAnimal(Transform actor,
            IReadOnlyList<ConstructionTileData> route, float pace,
            int visualIndex, bool bird)
        {
            if (!animalHomes.TryGetValue(actor, out var home)) yield break;
            // A food source in the home tile is still a visible short forage,
            // rather than an invisible one-node route.
            if (route.Count == 1)
            {
                yield return MoveBetween(actor, actor.position,
                    home + new Vector3(-0.52f, 0f, 0.43f), pace);
                yield return MoveBetween(actor, actor.position, home, pace);
                yield break;
            }
            for (var index = 1; index < route.Count; index++)
            {
                var target = Cell(route[index].column, route[index].row) +
                    AnimalOffset(visualIndex, bird);
                yield return MoveBetween(actor, actor.position, target, pace);
            }
            yield return MoveBetween(actor, actor.position, home, pace);
        }

        private static IEnumerator MoveBetween(Transform actor, Vector3 from,
            Vector3 to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration && actor != null && actor.gameObject.activeInHierarchy)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                actor.position = Vector3.Lerp(from, to,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            if (actor != null && actor.gameObject.activeInHierarchy)
                actor.position = to;
        }

        private void RestoreAnimalHomes()
        {
            foreach (var entry in animalHomes)
                if (entry.Key != null && entry.Key.gameObject.activeInHierarchy &&
                    (pigeonArrival == null || pigeon == null ||
                     entry.Key != pigeon.transform))
                    entry.Key.position = entry.Value;
        }

        private void StopForaging()
        {
            foreach (var trip in foragingTrips)
                if (trip != null) StopCoroutine(trip);
            foragingTrips.Clear();
            travelingAnimals.Clear();
            RestoreAnimalHomes();
        }

        private static Vector3 AnimalOffset(int index, bool bird)
        {
            var positions = bird ? PigeonOffsets : SquirrelOffsets;
            return positions[index % positions.Length];
        }

        private IEnumerator Commute(CitizenDemoVisual resident,
            IReadOnlyList<ConstructionTileData> route)
        {
            yield return Travel(resident.transform, route, 0.25f);
            commutingHomes.Remove(resident);
        }

        private void StopCommuting()
        {
            foreach (var trip in commuteTrips)
                if (trip != null) StopCoroutine(trip);
            commuteTrips.Clear();
            foreach (var entry in commutingHomes)
                if (entry.Key != null)
                    entry.Key.transform.localPosition = entry.Value;
            commutingHomes.Clear();
        }

        private static IEnumerator Travel(Transform actor,
            IReadOnlyList<ConstructionTileData> route, float pace)
        {
            var start = actor.localPosition;
            for (var index = 1; index < route.Count; index++)
            {
                var from = actor.localPosition;
                var to = Cell(route[index].column, route[index].row) +
                    new Vector3(0.55f, 0.23f, -0.5f);
                var elapsed = 0f;
                while (elapsed < pace)
                {
                    elapsed += Time.unscaledDeltaTime;
                    actor.localPosition = Vector3.Lerp(from, to,
                        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / pace)));
                    yield return null;
                }
            }
            actor.localPosition = start;
        }

        private void Update()
        {
            var time = Time.unscaledTime;
            for (var index = 0; index < squirrels.Count; index++)
            {
                var visual = squirrels[index];
                if (visual == null || !visual.gameObject.activeInHierarchy) continue;
                Wander(visual.transform, index, time,
                    travelingAnimals.Contains(visual.transform) ||
                    visual.gameObject == transientFoxPrey);
                visual.ApplyPose(visual.gameObject == transientFoxPrey
                    ? SquirrelDemoState.Alert : SquirrelDemoState.Hop,
                    time + index * 0.23f);
            }
            for (var index = 0; index < pigeons.Count; index++)
            {
                var visual = pigeons[index];
                if (visual == null || !visual.gameObject.activeInHierarchy) continue;
                var arriving = visual == pigeon && pigeonArrival != null;
                Wander(visual.transform, index + 5, time,
                    arriving || travelingAnimals.Contains(visual.transform) ||
                    visual.gameObject == transientFoxPrey);
                visual.ApplyPose(arriving || visual.gameObject == transientFoxPrey
                    ? PigeonDemoState.Flutter :
                    PigeonDemoState.Walk, time + index * 0.19f);
            }
            if (hedgehog != null && hedgehog.gameObject.activeInHierarchy)
            {
                Wander(hedgehog.transform, 11, time,
                    hedgehog.gameObject == transientFoxPrey);
                hedgehog.ApplyPose(hedgehog.gameObject == transientFoxPrey
                    ? HedgehogDemoState.Curl : HedgehogDemoState.Waddle, time);
            }
            if (fox != null && fox.gameObject.activeInHierarchy)
            {
                Wander(fox.transform, 13, time, fox.transform == travelingFox);
                fox.ApplyPose(time < foxContactUntil ? FoxDemoState.Sniff :
                    FoxDemoState.Trot, time);
            }
            foreach (var resident in residents.Values)
                resident?.ApplyPose((resident != null &&
                    commutingHomes.ContainsKey(resident)) ||
                    resident == arrivingResident ? CitizenDemoState.Walk :
                    CitizenDemoState.Idle, time);
        }

        private void Wander(Transform actor, int index, float time, bool busy)
        {
            if (busy || !animalHomes.TryGetValue(actor, out var home)) return;
            var phase = time * (index < 5 ? 1.35f : 0.92f) + index * 1.7f;
            actor.position = home + new Vector3(
                Mathf.Sin(phase) * 0.38f, 0f,
                Mathf.Sin(phase * 0.63f + index) * 0.29f);
        }

        private void OnDestroy()
        {
            if (openingSquirrelVisual != null)
                openingSquirrelVisual.SetVisible(true);
            if (caseHardware != null) Destroy(caseHardware);
            if (surface != null) Destroy(surface);
            if (tabletopMaterial != null) Destroy(tabletopMaterial);
        }

        private static RoomType RoomTypeFor(ConstructionTileData tile) => tile.category switch
        {
            ConstructionCategory.Residence => RoomType.Residence,
            ConstructionCategory.Restaurant => RoomType.Canteen,
            ConstructionCategory.Workshop => RoomType.Office,
            ConstructionCategory.Supermarket => RoomType.Supermarket,
            ConstructionCategory.Waste => RoomType.Trash,
            ConstructionCategory.Street => RoomType.Garage,
            ConstructionCategory.Square => RoomType.CommunitySquare,
            _ => tile.planting == GreenPlanting.Oak ? RoomType.OakHabitat :
                tile.planting == GreenPlanting.Shrub ? RoomType.ShrubHabitat :
                RoomType.CentralPark
        };

        private static Vector3 Cell(int column, int row) =>
            new((column - 3) * Pitch, 0f, (3 - row) * Pitch);

        private static Vector3 ResidentPosition(int column, int row) =>
            Cell(column, row) + new Vector3(0.8f, 0.23f, -0.5f);

        private static OakTreeStage ToOakStage(int growth) => growth switch
        {
            <= 0 => OakTreeStage.Sapling,
            1 => OakTreeStage.Young,
            _ => OakTreeStage.Mature
        };

        private GameObject Box(string name, Transform parent, Vector3 position,
            Vector3 size, Color color) =>
            UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cube, name, parent,
                position, size, color, surface, true, HideFlags.None);

        private void Bush(Transform parent, Vector3 position, float size) =>
            LowPolyTreeVisualBuilder.BuildFacetedFoliage(
                "Faceted green foliage", parent, position, Vector3.one * size,
                17f, UrbanPalette.ShrubLeaf, surface, HideFlags.None);

        private static GameObject NewActor(string name, Transform parent, Vector3 position)
        {
            var actor = new GameObject(name);
            actor.transform.SetParent(parent, false);
            actor.transform.localPosition = position;
            return actor;
        }

        private static void SetLayer(GameObject root, int layer)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }
    }

    // Built construction rooms use the older board's navy wall and pale
    // threshold language. Connectivity is still decided by the model.
    public static class ConstructionPlotFrameVisual
    {
        public static void BuildFrame(Transform parent, Material surface)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                foreach (var end in new[] { -1f, 1f })
                {
                    Box("North-south wall", parent,
                        new Vector3(end * 1.37f, 0.43f, side * 1.97f),
                        new Vector3(1.16f, 0.42f, 0.14f),
                        UrbanPalette.Wall, surface);
                    Box("East-west wall", parent,
                        new Vector3(side * 1.97f, 0.43f, end * 1.37f),
                        new Vector3(0.14f, 0.42f, 1.16f),
                        UrbanPalette.Wall, surface);
                    Box("Room corner", parent,
                        new Vector3(side * 1.97f, 0.42f, end * 1.97f),
                        new Vector3(0.19f, 0.58f, 0.19f),
                        UrbanPalette.Boundary, surface);
                }
                Box("North-south threshold", parent,
                    new Vector3(0f, 0.29f, side * 1.97f),
                    new Vector3(1.22f, 0.08f, 0.17f),
                    UrbanPalette.Doorframe, surface);
                Box("East-west threshold", parent,
                    new Vector3(side * 1.97f, 0.29f, 0f),
                    new Vector3(0.17f, 0.08f, 1.22f),
                    UrbanPalette.Doorframe, surface);
            }
        }

        private static void Box(string name, Transform parent, Vector3 position,
            Vector3 size, Color color, Material surface)
        {
            var item = UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cube,
                name, parent, position, size, color, surface, true, HideFlags.None);
            item.layer = parent.gameObject.layer;
        }
    }
}
