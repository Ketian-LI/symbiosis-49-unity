using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.People;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;
using InvalidOperationException = System.InvalidOperationException;

namespace UrbanWildlifeRooms.Core
{
    [ExecuteAlways]
    public sealed class UrbanWildlifeBootstrap : MonoBehaviour
    {
        public const string GeneratedRootName = "__Generated Layout Preview";

        [SerializeField, Min(2f)] private float cellSize = WorldScaleStandards.CellSizeMeters;
        [SerializeField, Range(0.05f, 0.5f)] private float roomGap = 0.16f;
        [SerializeField] private bool showRoomLabels = true;

        private readonly List<RoomView> roomViews = new();
        private readonly Dictionary<string, WasteRoomLoadVisual> wasteRoomVisuals = new();
        private readonly Dictionary<string, OakTreeStageVisual> oakTreeVisuals = new();
        private readonly List<PigeonDemoAgent> pigeonAgents = new();
        private readonly List<SquirrelDemoAgent> squirrelAgents = new();
        private readonly List<HedgehogDemoAgent> hedgehogAgents = new();
        private readonly List<FoxDemoAgent> foxAgents = new();
        private Transform generatedRoot;
        private Material surfaceMaterial;
        private UrbanWildlifeHud hud;
        private EndRunResultsOverlay resultsOverlay;
        private BoardCameraController boardCameraController;
        private GameRuntimeController runtimeController;
        private RoomLayoutEditorController layoutEditorController;
        private SessionPersistenceController persistenceController;
        private PhysicalBoardCameraController physicalBoardCameraController;
        private WasteManagementController wasteManagementController;
        private ResourceEconomyController resourceEconomyController;
        private ResidentPopulationController residentPopulationController;
        private OakTreeLifecycleController oakTreeLifecycleController;
        private AnimalNavigationCoordinator animalNavigationCoordinator;
        private PlayerFeedingController playerFeedingController;
        private AnimalMortalityController animalMortalityController;
        private NaturalFoodController naturalFoodController;
        private AnimalPopulationController animalPopulationController;
        private AnimalNeedsController animalNeedsController;
        private GarageTrafficController garageTrafficController;
        private AnimalActivityController animalActivityController;
        private SquirrelTreeResponseController squirrelTreeResponseController;
        private FoxPredationController foxPredationController;
        private HedgehogForagingController hedgehogForagingController;
        private SquirrelNaturalForagingController squirrelNaturalForagingController;
        private EcologicalMetricsController ecologicalMetricsController;
        private ResearchSessionController researchSessionController;
        private FirstRunOnboardingController onboardingController;
        private Transform mapRoot;
        private CitizenDemoAgent citizenDemoAgent;
        private PigeonDemoAgent pigeonDemoAgent;
        private SquirrelDemoAgent squirrelDemoAgent;
        private HedgehogDemoAgent hedgehogDemoAgent;
        private FoxDemoAgent foxDemoAgent;
        private RoomView selectedRoom;
        private bool isBuilding;
        private HideFlags generatedHideFlags;

        public Camera LayoutCamera { get; private set; }
        public RoomLayoutEditorController LayoutEditor => layoutEditorController;

        private void OnEnable()
        {
            EnsureBuilt();
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                ClearGenerated();
            }
        }

        public void RebuildPreview()
        {
            ClearGenerated();
            EnsureBuilt();
        }

        private void EnsureBuilt()
        {
            if (isBuilding || generatedRoot != null)
            {
                return;
            }

            var existing = transform.Find(GeneratedRootName);
            if (existing != null)
            {
                generatedRoot = existing;
                return;
            }

            BuildLayout();
        }

        private void BuildLayout()
        {
            isBuilding = true;
            generatedHideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSaveInEditor;
            roomViews.Clear();
            wasteRoomVisuals.Clear();
            oakTreeVisuals.Clear();

            var validationErrors = RoomLayoutData.Validate();
            foreach (var error in validationErrors)
            {
                Debug.LogError($"[Urban Wildlife Layout] {error}", this);
            }

            var generated = NewObject(GeneratedRootName, transform);
            generatedRoot = generated.transform;
            surfaceMaterial = UrbanVisualFactory.CreateSurfaceMaterial();

            LayoutCamera = BuildCamera();
            boardCameraController = LayoutCamera.gameObject.AddComponent<BoardCameraController>();
            boardCameraController.Initialize(LayoutCamera);
            var runtimeObject = NewObject("Game Runtime", generatedRoot);
            runtimeController = runtimeObject.AddComponent<GameRuntimeController>();
            runtimeController.Initialize(boardCameraController);
            BuildLighting();
            BuildMap();
            BuildHud();
            BuildLayoutEditor();
            BuildPhysicalBoardCamera();
            BuildWasteManagement();
            BuildResourceEconomy();
            BuildResidentPopulation();
            BuildOakTreeLifecycle();
            BuildNaturalFood();
            BuildPigeonAnimationDemo();
            BuildCitizenAnimationDemo();
            BuildSquirrelAnimationDemo();
            BuildHedgehogAnimationDemo();
            BuildFoxAnimationDemo();
            BuildAnimalPopulation();
            BuildAnimalActivity();
            BuildAnimalNavigation();
            BuildGarageTraffic();
            BuildPlayerFeeding();
            BuildSquirrelTreeResponse();
            BuildAnimalMortality();
            BuildAnimalNeeds();
            BuildFoxPredation();
            BuildHedgehogForaging();
            BuildSquirrelNaturalForaging();
            BuildEcologicalMetrics();
            BuildResearchSession();
            BuildFirstRunOnboarding();
            BuildSessionPersistence();

            if (validationErrors.Count == 0)
            {
                Debug.Log("[Urban Wildlife Layout] 7×7 layout validated: 35 logical rooms occupy all 49 cells.", this);
            }

            isBuilding = false;
        }

        private Camera BuildCamera()
        {
            var cameraObject = NewObject("Main Camera", generatedRoot);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 32f, 0f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 14.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UrbanPalette.Background;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.depth = 0f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private void BuildLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.82f, 0.80f, 0.78f);

            var lightObject = NewObject("Soft Day Light", generatedRoot);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
        }

        private void BuildMap()
        {
            mapRoot = NewObject("7x7 Modular Floor", generatedRoot).transform;
            var mapSize = RoomLayoutData.GridSize * cellSize;

            BuildFixedBoardFoundation(mapRoot, mapSize);

            var roomsRoot = NewObject("Movable Room Modules", mapRoot).transform;

            foreach (var room in RoomLayoutData.All)
            {
                BuildRoom(roomsRoot, room);
            }

            if (!Application.isPlaying && showRoomLabels)
            {
                BuildMapCaption(mapRoot, mapSize);
            }
        }

        private void BuildFixedBoardFoundation(Transform parent, float mapSize)
        {
            var foundationRoot = NewObject("Fixed Board Foundation", parent).transform;

            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cube,
                "Paper Board",
                foundationRoot,
                new Vector3(0f, -0.18f, 0f),
                new Vector3(mapSize + 1.25f, 0.28f, mapSize + 1.25f),
                UrbanPalette.Board,
                surfaceMaterial,
                true,
                generatedHideFlags);

            var slotsRoot = NewObject("49 Fixed Cell Slots", foundationRoot).transform;
            var slotSize = cellSize - roomGap * 0.72f;
            for (var row = 0; row < RoomLayoutData.GridSize; row++)
            {
                for (var column = 0; column < RoomLayoutData.GridSize; column++)
                {
                    var x = (column + 0.5f - RoomLayoutData.GridSize * 0.5f) * cellSize;
                    var z = (RoomLayoutData.GridSize * 0.5f - row - 0.5f) * cellSize;
                    var color = (column + row) % 2 == 0
                        ? UrbanPalette.BoardSlot
                        : UrbanPalette.BoardSlotAlternate;
                    CreateBox(
                        $"Foundation Slot {column + 1}-{row + 1}",
                        slotsRoot,
                        new Vector3(x, -0.025f, z),
                        new Vector3(slotSize, 0.08f, slotSize),
                        color);
                }
            }

            BuildBoundaryCoverLayer(foundationRoot, mapSize);
        }

        private void BuildBoundaryCoverLayer(Transform foundationRoot, float mapSize)
        {
            const float rimThickness = 0.34f;
            const float rimHeight = 0.22f;
            const float capHeight = 0.34f;
            const float capThickness = 0.18f;
            const float capWidth = 0.66f;
            var rimOffset = mapSize * 0.5f + 0.34f;
            var capOffset = mapSize * 0.5f + 0.04f;
            var boundaryRoot = NewObject("Automatic Boundary Cover Layer", foundationRoot).transform;

            CreateBox("North Board Rim", boundaryRoot, new Vector3(0f, 0.04f, rimOffset), new Vector3(mapSize + 1.02f, rimHeight, rimThickness), UrbanPalette.Boundary);
            CreateBox("South Board Rim", boundaryRoot, new Vector3(0f, 0.04f, -rimOffset), new Vector3(mapSize + 1.02f, rimHeight, rimThickness), UrbanPalette.Boundary);
            CreateBox("West Board Rim", boundaryRoot, new Vector3(-rimOffset, 0.04f, 0f), new Vector3(rimThickness, rimHeight, mapSize + 1.02f), UrbanPalette.Boundary);
            CreateBox("East Board Rim", boundaryRoot, new Vector3(rimOffset, 0.04f, 0f), new Vector3(rimThickness, rimHeight, mapSize + 1.02f), UrbanPalette.Boundary);

            for (var index = 0; index < RoomLayoutData.GridSize; index++)
            {
                var axis = (index + 0.5f - RoomLayoutData.GridSize * 0.5f) * cellSize;
                CreateBox($"North Boundary Cap {index + 1}", boundaryRoot, new Vector3(axis, 0.38f, capOffset), new Vector3(capWidth, capHeight, capThickness), UrbanPalette.Boundary);
                CreateBox($"South Boundary Cap {index + 1}", boundaryRoot, new Vector3(axis, 0.38f, -capOffset), new Vector3(capWidth, capHeight, capThickness), UrbanPalette.Boundary);
                CreateBox($"West Boundary Cap {index + 1}", boundaryRoot, new Vector3(-capOffset, 0.38f, axis), new Vector3(capThickness, capHeight, capWidth), UrbanPalette.Boundary);
                CreateBox($"East Boundary Cap {index + 1}", boundaryRoot, new Vector3(capOffset, 0.38f, axis), new Vector3(capThickness, capHeight, capWidth), UrbanPalette.Boundary);
            }
        }

        private void BuildRoom(Transform mapRoot, RoomSpec spec)
        {
            var roomRootObject = NewObject(spec.DisplayName, mapRoot);
            var roomRoot = roomRootObject.transform;
            roomRoot.localPosition = GridToWorld(spec);

            var shellRoot = NewObject("Reusable Room Shell", roomRoot).transform;
            var contentRoot = NewObject("Reusable Room Content", roomRoot).transform;

            var width = spec.Width * cellSize - roomGap;
            var depth = spec.Height * cellSize - roomGap;
            var roomColor = UrbanPalette.ForRoom(spec.Type);

            if (spec.Type == RoomType.Trash)
            {
                roomColor = new Color(0.56f, 0.58f, 0.59f);
            }

            if (spec.StartsRecovering)
            {
                roomColor = Color.Lerp(roomColor, UrbanPalette.Recovery, 0.38f);
            }

            var floor = UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cube,
                "Clickable Floor",
                shellRoot,
                new Vector3(0f, 0.12f, 0f),
                new Vector3(width, 0.24f, depth),
                roomColor,
                surfaceMaterial,
                false,
                generatedHideFlags);

            BuildRoomShell(shellRoot, spec.Width, spec.Height, width, depth);
            if (spec.Type == RoomType.Garage)
            {
                GarageVisualLayout.BuildShellDetails(shellRoot, spec, depth, surfaceMaterial, generatedHideFlags);
            }
            if (spec.Type == RoomType.Trash)
            {
                BuildWasteRoomFloorDetails(shellRoot);
            }
            BuildRoomDecoration(contentRoot, spec, width, depth);
            if (spec.Type == RoomType.Trash)
            {
                var wasteVisual = contentRoot.GetComponent<WasteRoomLoadVisual>();
                if (wasteVisual != null)
                {
                    wasteRoomVisuals[spec.Id] = wasteVisual;
                }
            }
            if (spec.Type == RoomType.OakHabitat)
            {
                var oakVisual = contentRoot.GetComponent<OakTreeStageVisual>();
                if (oakVisual != null)
                {
                    oakTreeVisuals[spec.Id] = oakVisual;
                }
            }
            ValidateRoomContentClearances(contentRoot, spec);

            if (showRoomLabels && !Application.isPlaying)
            {
                BuildRoomLabel(contentRoot, spec);
            }

            var view = floor.AddComponent<RoomView>();
            view.Initialize(
                spec,
                roomRoot,
                floor.GetComponent<Renderer>(),
                roomColor,
                width,
                depth,
                generatedHideFlags);
            view.Clicked += SelectRoom;
            view.DoubleClicked += FocusRoom;
            view.HoverChanged += HandleRoomHover;
            roomViews.Add(view);
        }

        private void BuildRoomShell(Transform parent, int widthCells, int heightCells, float width, float depth)
        {
            const float borderThickness = 0.13f;
            const float borderHeight = 0.36f;
            const float borderY = 0.39f;
            const float doorwayWidth = 0.72f;

            var doorways = RoomShellLayout.CreateDoorways(widthCells, heightCells, cellSize, roomGap);
            foreach (var doorway in doorways)
            {
                if (doorway.Edge is RoomEdge.North or RoomEdge.South)
                {
                    var segmentMin = Mathf.Max(-width * 0.5f, doorway.LocalCenter.x - cellSize * 0.5f);
                    var segmentMax = Mathf.Min(width * 0.5f, doorway.LocalCenter.x + cellSize * 0.5f);
                    BuildHorizontalWallPieces(
                        parent,
                        doorway,
                        segmentMin,
                        segmentMax,
                        borderY,
                        borderHeight,
                        borderThickness,
                        doorwayWidth);
                }
                else
                {
                    var segmentMin = Mathf.Max(-depth * 0.5f, doorway.LocalCenter.z - cellSize * 0.5f);
                    var segmentMax = Mathf.Min(depth * 0.5f, doorway.LocalCenter.z + cellSize * 0.5f);
                    BuildVerticalWallPieces(
                        parent,
                        doorway,
                        segmentMin,
                        segmentMax,
                        borderY,
                        borderHeight,
                        borderThickness,
                        doorwayWidth);
                }

                BuildDoorframe(parent, doorway, borderY, borderHeight, borderThickness, doorwayWidth);
            }
        }

        private void ValidateRoomContentClearances(Transform contentRoot, RoomSpec spec)
        {
            const float doorwayWidth = 0.72f;
            const float clearanceDepth = 0.85f;
            const float sideMargin = 0.06f;
            const float largestGroundAgentRadius = 0.24f;
            const float navigationSampleSpacing = 0.10f;
            var clearances = RoomShellLayout.CreateDoorClearances(
                spec.Width,
                spec.Height,
                cellSize,
                roomGap,
                doorwayWidth,
                clearanceDepth,
                sideMargin);
            var obstacles = new List<RoomObstacle2D>();

            foreach (var renderer in contentRoot.GetComponentsInChildren<Renderer>(true))
            {
                GetRendererBoundsInLocalSpace(renderer, contentRoot, out var center, out var size);
                obstacles.Add(new RoomObstacle2D(center, size));
                foreach (var clearance in clearances)
                {
                    if (!clearance.Overlaps(center, size))
                    {
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"Room '{spec.DisplayName}' content '{renderer.gameObject.name}' blocks " +
                        $"{clearance.Edge} door {clearance.SegmentIndex + 1}.");
                }
            }

            var roomWidth = spec.Width * cellSize - roomGap;
            var roomDepth = spec.Height * cellSize - roomGap;
            if (!RoomShellLayout.AreDoorClearancesConnected(
                    roomWidth,
                    roomDepth,
                    clearances,
                    obstacles,
                    largestGroundAgentRadius,
                    navigationSampleSpacing,
                    out var unreachable))
            {
                throw new InvalidOperationException(
                    $"Room '{spec.DisplayName}' furnishings disconnect " +
                    $"{unreachable.Edge} door {unreachable.SegmentIndex + 1} for a " +
                    $"{largestGroundAgentRadius * 2f:0.00}-unit-wide ground agent.");
            }
        }

        private static void GetRendererBoundsInLocalSpace(
            Renderer renderer,
            Transform roomContentRoot,
            out Vector2 center,
            out Vector2 size)
        {
            var bounds = renderer.localBounds;
            var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var rendererLocal = bounds.center + Vector3.Scale(
                            bounds.extents,
                            new Vector3(x, y, z));
                        var world = renderer.transform.TransformPoint(rendererLocal);
                        var roomLocal = roomContentRoot.InverseTransformPoint(world);
                        minimum = Vector2.Min(minimum, new Vector2(roomLocal.x, roomLocal.z));
                        maximum = Vector2.Max(maximum, new Vector2(roomLocal.x, roomLocal.z));
                    }
                }
            }

            center = (minimum + maximum) * 0.5f;
            size = maximum - minimum;
        }

        private void BuildHorizontalWallPieces(
            Transform parent,
            RoomDoorwaySlot doorway,
            float segmentMin,
            float segmentMax,
            float wallY,
            float wallHeight,
            float wallThickness,
            float doorwayWidth)
        {
            var doorwayMin = doorway.LocalCenter.x - doorwayWidth * 0.5f;
            var doorwayMax = doorway.LocalCenter.x + doorwayWidth * 0.5f;
            CreateHorizontalWallPiece(parent, doorway, "A", segmentMin, doorwayMin, wallY, wallHeight, wallThickness);
            CreateHorizontalWallPiece(parent, doorway, "B", doorwayMax, segmentMax, wallY, wallHeight, wallThickness);
        }

        private void CreateHorizontalWallPiece(
            Transform parent,
            RoomDoorwaySlot doorway,
            string suffix,
            float minimum,
            float maximum,
            float wallY,
            float wallHeight,
            float wallThickness)
        {
            var length = maximum - minimum;
            if (length <= 0.01f)
            {
                return;
            }

            CreateBox(
                $"{doorway.Edge} Wall {doorway.SegmentIndex + 1}{suffix}",
                parent,
                new Vector3((minimum + maximum) * 0.5f, wallY, doorway.LocalCenter.z),
                new Vector3(length, wallHeight, wallThickness),
                UrbanPalette.Wall);
        }

        private void BuildVerticalWallPieces(
            Transform parent,
            RoomDoorwaySlot doorway,
            float segmentMin,
            float segmentMax,
            float wallY,
            float wallHeight,
            float wallThickness,
            float doorwayWidth)
        {
            var doorwayMin = doorway.LocalCenter.z - doorwayWidth * 0.5f;
            var doorwayMax = doorway.LocalCenter.z + doorwayWidth * 0.5f;
            CreateVerticalWallPiece(parent, doorway, "A", segmentMin, doorwayMin, wallY, wallHeight, wallThickness);
            CreateVerticalWallPiece(parent, doorway, "B", doorwayMax, segmentMax, wallY, wallHeight, wallThickness);
        }

        private void CreateVerticalWallPiece(
            Transform parent,
            RoomDoorwaySlot doorway,
            string suffix,
            float minimum,
            float maximum,
            float wallY,
            float wallHeight,
            float wallThickness)
        {
            var length = maximum - minimum;
            if (length <= 0.01f)
            {
                return;
            }

            CreateBox(
                $"{doorway.Edge} Wall {doorway.SegmentIndex + 1}{suffix}",
                parent,
                new Vector3(doorway.LocalCenter.x, wallY, (minimum + maximum) * 0.5f),
                new Vector3(wallThickness, wallHeight, length),
                UrbanPalette.Wall);
        }

        private void BuildDoorframe(
            Transform parent,
            RoomDoorwaySlot doorway,
            float wallY,
            float wallHeight,
            float wallThickness,
            float doorwayWidth)
        {
            const float postWidth = 0.08f;
            var name = $"{doorway.Edge} Door {doorway.SegmentIndex + 1}";
            var horizontal = doorway.Edge is RoomEdge.North or RoomEdge.South;

            var thresholdScale = horizontal
                ? new Vector3(doorwayWidth, 0.05f, wallThickness * 1.72f)
                : new Vector3(wallThickness * 1.72f, 0.05f, doorwayWidth);
            CreateBox(
                $"{name} Threshold",
                parent,
                doorway.LocalCenter + new Vector3(0f, 0.255f, 0f),
                thresholdScale,
                UrbanPalette.Doorframe);

            var postScale = horizontal
                ? new Vector3(postWidth, wallHeight, wallThickness * 1.36f)
                : new Vector3(wallThickness * 1.36f, wallHeight, postWidth);
            var postOffset = horizontal
                ? new Vector3(doorwayWidth * 0.5f, 0f, 0f)
                : new Vector3(0f, 0f, doorwayWidth * 0.5f);
            CreateBox($"{name} Post A", parent, doorway.LocalCenter + postOffset + new Vector3(0f, wallY, 0f), postScale, UrbanPalette.Doorframe);
            CreateBox($"{name} Post B", parent, doorway.LocalCenter - postOffset + new Vector3(0f, wallY, 0f), postScale, UrbanPalette.Doorframe);

            var lintelScale = horizontal
                ? new Vector3(doorwayWidth + postWidth * 2f, 0.06f, wallThickness * 1.36f)
                : new Vector3(wallThickness * 1.36f, 0.06f, doorwayWidth + postWidth * 2f);
            CreateBox(
                $"{name} Lintel",
                parent,
                doorway.LocalCenter + new Vector3(0f, wallY + wallHeight * 0.58f, 0f),
                lintelScale,
                UrbanPalette.Doorframe);
        }

        private void BuildRoomLabel(Transform parent, RoomSpec spec)
        {
            var labelObject = NewObject("Room Label", parent);
            labelObject.transform.localPosition = new Vector3(0f, 1.86f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.font = UrbanFontResolver.GetFont();
            textMesh.fontSize = 72;
            textMesh.characterSize = spec.Width == 1 && spec.Height == 1 ? 0.026f : 0.031f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = UrbanPalette.LightText;
            textMesh.text = spec.StartsRecovering
                ? $"{spec.DisplayName}\n幼树 · {spec.SizeLabel}"
                : $"{spec.DisplayName}\n{spec.SizeLabel}";
            textMesh.richText = false;

            var meshRenderer = labelObject.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = textMesh.font.material;
            meshRenderer.sortingOrder = 10;
        }

        private void BuildRoomDecoration(Transform parent, RoomSpec spec, float width, float depth)
        {
            var corner = new Vector3(-width * 0.5f + 0.56f, 0.48f, depth * 0.5f - 0.56f);

            switch (spec.Type)
            {
                case RoomType.CentralPark:
                    CreatePond(parent, new Vector3(-0.8f, 0.34f, -0.72f), 1.05f);
                    CreateTree(parent, new Vector3(-1.45f, 0f, 1.15f), 1.12f, false);
                    CreateTree(parent, new Vector3(1.22f, 0f, 1.22f), 1.02f, false);
                    CreateBush(parent, new Vector3(1.35f, 0f, -1.1f), 0.55f);
                    CreateBench(parent, new Vector3(0.20f, 0f, -2.18f), 0f, "South Park Bench");
                    CreateBench(parent, new Vector3(2.18f, 0f, 0.24f), 90f, "East Park Bench");
                    break;
                case RoomType.Residence:
                case RoomType.Office:
                case RoomType.Canteen:
                case RoomType.Supermarket:
                case RoomType.Garage:
                    RoomInteriorVisualBuilder.Build(
                        parent,
                        spec,
                        width,
                        depth,
                        surfaceMaterial,
                        generatedHideFlags);
                    break;
                case RoomType.Trash:
                    var wasteVisual = parent.gameObject.AddComponent<WasteRoomLoadVisual>();
                    wasteVisual.Initialize(surfaceMaterial, generatedHideFlags);
                    break;
                case RoomType.PigeonHabitat:
                    RoomInteriorVisualBuilder.Build(
                        parent,
                        spec,
                        width,
                        depth,
                        surfaceMaterial,
                        generatedHideFlags);
                    break;
                case RoomType.OakHabitat:
                    CreateOakTreeLifecycleVisual(parent, corner, spec.StartsRecovering);
                    CreateOakGroundDetails(parent, corner);
                    break;
                case RoomType.ShrubHabitat:
                case RoomType.FoxDen:
                    RoomInteriorVisualBuilder.Build(
                        parent,
                        spec,
                        width,
                        depth,
                        surfaceMaterial,
                        generatedHideFlags);
                    break;
            }
        }

        private void BuildWasteRoomFloorDetails(Transform parent)
        {
            const float routeHalfLength = 1.27f;
            const float brickLength = 0.29f;
            const float brickWidth = 0.31f;
            const float brickGap = 0.025f;
            const float brickY = 0.258f;
            var brickColourA = new Color(0.74f, 0.72f, 0.67f);
            var brickColourB = new Color(0.68f, 0.67f, 0.63f);
            var step = brickLength + brickGap;
            var index = 0;

            for (var offset = -routeHalfLength; offset <= routeHalfLength + 0.001f; offset += step)
            {
                for (var lane = -1; lane <= 1; lane += 2)
                {
                    var laneOffset = lane * (brickWidth * 0.5f + brickGap * 0.25f);
                    CreateBox(
                        $"North South Route Brick {++index}",
                        parent,
                        new Vector3(laneOffset, brickY, offset),
                        new Vector3(brickWidth, 0.035f, brickLength),
                        index % 3 == 0 ? brickColourB : brickColourA);
                }
            }

            for (var offset = -routeHalfLength; offset <= routeHalfLength + 0.001f; offset += step)
            {
                if (Mathf.Abs(offset) < 0.36f)
                {
                    continue;
                }

                for (var lane = -1; lane <= 1; lane += 2)
                {
                    var laneOffset = lane * (brickWidth * 0.5f + brickGap * 0.25f);
                    CreateBox(
                        $"East West Route Brick {++index}",
                        parent,
                        new Vector3(offset, brickY, laneOffset),
                        new Vector3(brickLength, 0.035f, brickWidth),
                        index % 3 == 0 ? brickColourB : brickColourA);
                }
            }

            CreateBox(
                "Central Waste Drain",
                parent,
                new Vector3(0f, brickY + 0.025f, 0f),
                new Vector3(0.31f, 0.07f, 0.31f),
                new Color(0.16f, 0.17f, 0.18f));
        }

        private void CreateTree(Transform parent, Vector3 localPosition, float scale, bool seedling)
        {
            LowPolyTreeVisualBuilder.Build(
                parent, localPosition, scale, seedling, surfaceMaterial, generatedHideFlags);
        }

        private void CreateOakTreeLifecycleVisual(
            Transform parent,
            Vector3 localPosition,
            bool startsRecovering)
        {
            var visual = parent.gameObject.AddComponent<OakTreeStageVisual>();
            var felled = NewObject("Felled Tree Plot", parent);
            CreateCylinder(
                "Disturbed Earth",
                felled.transform,
                localPosition + new Vector3(0f, -0.12f, 0f),
                new Vector3(0.72f, 0.035f, 0.72f),
                new Color(0.44f, 0.32f, 0.20f));

            var sapling = NewObject("Sapling Tree", parent);
            CreateTree(sapling.transform, localPosition, 0.30f, true);

            var young = NewObject("Young Tree", parent);
            CreateTree(young.transform, localPosition, 0.50f, true);

            var mature = NewObject("Mature Oak Tree", parent);
            CreateTree(mature.transform, localPosition, 0.82f, false);

            visual.Initialize(
                felled,
                sapling,
                young,
                mature,
                startsRecovering ? OakTreeStage.Young : OakTreeStage.Mature);
        }

        private void CreateBush(Transform parent, Vector3 localPosition, float scale)
        {
            var color = new Color(0.37f, 0.66f, 0.36f);
            for (var index = 0; index < 3; index++)
            {
                var offset = index switch
                {
                    0 => new Vector3(-0.25f, 0.36f, 0f),
                    1 => new Vector3(0.25f, 0.36f, 0.08f),
                    _ => new Vector3(0f, 0.42f, -0.25f)
                };
                UrbanVisualFactory.CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Shrub Cluster",
                    parent,
                    localPosition + offset * scale,
                    Vector3.one * scale * 0.72f,
                    color,
                    surfaceMaterial,
                    true,
                    generatedHideFlags);
            }
        }

        private void CreateBench(
            Transform parent,
            Vector3 localPosition,
            float yRotation,
            string objectName)
        {
            var bench = NewObject(objectName, parent).transform;
            bench.localPosition = localPosition;
            bench.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            var wood = new Color(0.50f, 0.31f, 0.17f);
            var metal = new Color(0.18f, 0.21f, 0.21f);
            CreateBox("Seat", bench, new Vector3(0f, 0.43f, 0f), new Vector3(0.82f, 0.11f, 0.28f), wood);
            CreateBox("Back", bench, new Vector3(0f, 0.65f, 0.11f), new Vector3(0.82f, 0.34f, 0.08f), wood);
            CreateBox("Left Leg", bench, new Vector3(-0.29f, 0.32f, 0f), new Vector3(0.07f, 0.25f, 0.21f), metal);
            CreateBox("Right Leg", bench, new Vector3(0.29f, 0.32f, 0f), new Vector3(0.07f, 0.25f, 0.21f), metal);
        }

        private void CreateOakGroundDetails(Transform parent, Vector3 treeAnchor)
        {
            CreateCylinder(
                "Squirrel Cache Hollow",
                parent,
                treeAnchor + new Vector3(0.20f, -0.07f, -0.24f),
                new Vector3(0.11f, 0.025f, 0.11f),
                new Color(0.16f, 0.12f, 0.08f));
            var twig = CreateBox(
                "Fallen Twig",
                parent,
                treeAnchor + new Vector3(-0.24f, -0.10f, -0.24f),
                new Vector3(0.35f, 0.045f, 0.07f),
                new Color(0.38f, 0.25f, 0.15f));
            twig.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Sphere,
                "Oak Stone",
                parent,
                treeAnchor + new Vector3(0.28f, -0.08f, 0.22f),
                new Vector3(0.18f, 0.12f, 0.16f),
                new Color(0.48f, 0.50f, 0.45f),
                surfaceMaterial,
                true,
                generatedHideFlags);
        }

        private void CreatePond(Transform parent, Vector3 localPosition, float scale)
        {
            var pond = UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cylinder,
                "Park Pond",
                parent,
                localPosition,
                new Vector3(scale, 0.05f, scale * 0.75f),
                new Color(0.30f, 0.71f, 0.78f),
                surfaceMaterial,
                true,
                generatedHideFlags);
            pond.transform.localRotation = Quaternion.identity;
        }

        private void CreateAnimalDot(Transform parent, Vector3 localPosition, Color color, float scale)
        {
            UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Sphere,
                "Animal Marker",
                parent,
                localPosition + new Vector3(0f, 0.40f, 0f),
                new Vector3(scale * 1.2f, scale * 0.75f, scale),
                color,
                surfaceMaterial,
                true,
                generatedHideFlags);
        }

        private void BuildMapCaption(Transform parent, float mapSize)
        {
            var caption = NewObject("Map Caption", parent);
            caption.transform.localPosition = new Vector3(0f, 0.22f, mapSize * 0.5f + 0.46f);
            caption.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var text = caption.AddComponent<TextMesh>();
            text.font = UrbanFontResolver.GetFont();
            text.fontSize = 64;
            text.characterSize = 0.036f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = UrbanPalette.Text;
            text.text = "7×7 模块化房间 · 35 个逻辑房间 · 无走廊";
            text.richText = false;
            caption.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        }

        private void BuildHud()
        {
            var hudObject = NewObject("HUD Canvas", generatedRoot);
            hud = hudObject.AddComponent<UrbanWildlifeHud>();
            hud.Build(
                LayoutCamera,
                UrbanFontResolver.GetFont(),
                generatedHideFlags,
                runtimeController,
                boardCameraController);
            resultsOverlay = hudObject.AddComponent<EndRunResultsOverlay>();
            resultsOverlay.Build(UrbanFontResolver.GetFont(), generatedHideFlags, runtimeController, LayoutCamera);
        }

        private void BuildLayoutEditor()
        {
            var editorObject = NewObject("Room Layout Editor", generatedRoot);
            layoutEditorController = editorObject.AddComponent<RoomLayoutEditorController>();
            layoutEditorController.Initialize(
                roomViews,
                runtimeController,
                boardCameraController,
                LayoutCamera,
                surfaceMaterial,
                generatedHideFlags,
                cellSize);
            hud.BindLayoutEditor(layoutEditorController);
            runtimeController.RestartRequested += HandleRestartRequested;
        }

        private void BuildPhysicalBoardCamera()
        {
            if (!BuildVariantSettings.UsesCameraRecognition)
            {
                return;
            }
            var cameraObject = NewObject("Physical Board Camera", generatedRoot);
            physicalBoardCameraController = cameraObject.AddComponent<PhysicalBoardCameraController>();
            physicalBoardCameraController.Initialize(runtimeController, layoutEditorController, hud);
        }

        private void HandleRestartRequested()
        {
            boardCameraController?.ReturnToOverviewIfNeeded();
            layoutEditorController?.ResetToInitialLayout();
        }

        private void BuildSessionPersistence()
        {
            var persistenceObject = NewObject("Session Persistence", generatedRoot);
            persistenceController = persistenceObject.AddComponent<SessionPersistenceController>();
            persistenceController.Initialize(
                runtimeController,
                layoutEditorController,
                wasteManagementController,
                resourceEconomyController,
                residentPopulationController,
                oakTreeLifecycleController,
                playerFeedingController,
                animalMortalityController,
                naturalFoodController,
                animalNeedsController);
        }

        private void BuildWasteManagement()
        {
            var wasteObject = NewObject("Waste Management", generatedRoot);
            wasteManagementController = wasteObject.AddComponent<WasteManagementController>();
            wasteManagementController.Initialize(
                runtimeController,
                layoutEditorController,
                wasteRoomVisuals,
                cellSize);
            hud.BindWasteManagement(wasteManagementController);
        }

        private void BuildResourceEconomy()
        {
            var resourceObject = NewObject("Resource Economy", generatedRoot);
            resourceEconomyController = resourceObject.AddComponent<ResourceEconomyController>();
            resourceEconomyController.Initialize(
                runtimeController,
                wasteManagementController,
                layoutEditorController);
            hud.BindResourceEconomy(resourceEconomyController);
        }

        private void BuildResidentPopulation()
        {
            var residentObject = NewObject("Resident Population", generatedRoot);
            residentPopulationController = residentObject.AddComponent<ResidentPopulationController>();
            residentPopulationController.Initialize(
                runtimeController,
                layoutEditorController,
                wasteManagementController,
                resourceEconomyController,
                cellSize);
            hud.BindResidentPopulation(
                residentPopulationController,
                roomId => roomViews.Find(view => view.Spec.Id == roomId)?.VisualRoot);
        }

        private void BuildOakTreeLifecycle()
        {
            var treeObject = NewObject("Oak Tree Lifecycle", generatedRoot);
            oakTreeLifecycleController = treeObject.AddComponent<OakTreeLifecycleController>();
            oakTreeLifecycleController.Initialize(
                runtimeController,
                layoutEditorController,
                resourceEconomyController,
                oakTreeVisuals);
            hud.BindOakTreeLifecycle(oakTreeLifecycleController);
        }

        private void BuildNaturalFood()
        {
            var foodObject = NewObject("Natural Food Ecology", generatedRoot);
            naturalFoodController = foodObject.AddComponent<NaturalFoodController>();
            naturalFoodController.Initialize(
                runtimeController,
                wasteManagementController,
                oakTreeLifecycleController,
                resourceEconomyController,
                roomViews,
                surfaceMaterial,
                generatedHideFlags);
        }

        private void BuildPigeonAnimationDemo()
        {
            var demoRoot = NewObject("Pigeon Population", generatedRoot).transform;
            var effectsRoot = NewObject("Temporary Animal Traces", demoRoot).transform;
            var homes = new List<string>
            {
                "pigeon-a", "pigeon-a", "pigeon-b", "pigeon-b",
                "pigeon-c", "pigeon-c", "pigeon-d", "pigeon-d",
                "central-park", "pigeon-a", "pigeon-c", "central-park"
            };
            for (var index = 0; index < homes.Count; index++)
            {
                var home = FindRoomSpec(homes[index]);
                if (home == null)
                {
                    continue;
                }
                var angle = index * 2.399f;
                var radius = 0.28f + index % 3 * 0.16f;
                var spawnPosition = GridToWorld(home) + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.30f,
                    Mathf.Sin(angle) * radius);
                var pigeonObject = NewObject($"Pigeon {index + 1:00}", demoRoot);
                var pigeon = pigeonObject.AddComponent<PigeonDemoAgent>();
                pigeon.Initialize(
                    surfaceMaterial,
                    effectsRoot,
                    spawnPosition,
                    new Vector2(0.74f, 0.74f),
                    generatedHideFlags,
                    4900 + index);
                pigeonAgents.Add(pigeon);
            }
            pigeonDemoAgent = pigeonAgents.Count > 0 ? pigeonAgents[0] : null;

            var directorObject = NewObject("Pigeon Demo Director", demoRoot);
            var director = directorObject.AddComponent<PigeonDemoDirector>();
            director.Initialize(boardCameraController, pigeonAgents);
        }

        private void BuildCitizenAnimationDemo()
        {
            RoomSpec centralPark = null;
            foreach (var room in RoomLayoutData.All)
            {
                if (room.Type == RoomType.CentralPark)
                {
                    centralPark = room;
                    break;
                }
            }

            if (centralPark == null)
            {
                return;
            }

            var demoRoot = NewObject("Citizen Animation Demo", generatedRoot).transform;
            var citizenObject = NewObject("Citizen Demo Agent", demoRoot);
            var citizen = citizenObject.AddComponent<CitizenDemoAgent>();
            var parkCenter = GridToWorld(centralPark);
            var spawnPosition = parkCenter + new Vector3(-0.82f, 0.30f, -0.68f);
            citizen.Initialize(surfaceMaterial, spawnPosition, new Vector2(1.55f, 1.55f), generatedHideFlags);
            citizenDemoAgent = citizen;
        }

        private void BuildSquirrelAnimationDemo()
        {
            var demoRoot = NewObject("Squirrel Population", generatedRoot).transform;
            var cacheContainer = NewObject("Fixed Wildlife Locations", generatedRoot).transform;
            var homes = new[] { "oak-a", "oak-b", "oak-d", "central-park" };
            for (var index = 0; index < homes.Length; index++)
            {
                var home = FindRoomSpec(homes[index]);
                if (home == null)
                {
                    continue;
                }
                var center = GridToWorld(home);
                var direction = new Vector3(index % 2 == 0 ? -0.34f : 0.34f, 0f, index < 2 ? 0.25f : -0.25f);
                var squirrelObject = NewObject($"Squirrel {index + 1:00}", demoRoot);
                var squirrel = squirrelObject.AddComponent<SquirrelDemoAgent>();
                squirrel.Initialize(
                    surfaceMaterial,
                    center + direction + Vector3.up * 0.30f,
                    new Vector2(0.64f, 0.64f),
                    generatedHideFlags,
                    24900 + index);
                squirrel.InitializeCache(
                    cacheContainer,
                    center - direction + Vector3.up * 0.20f);
                squirrelAgents.Add(squirrel);
            }
            squirrelDemoAgent = squirrelAgents.Count > 0 ? squirrelAgents[0] : null;
        }

        private void BuildHedgehogAnimationDemo()
        {
            var demoRoot = NewObject("Hedgehog Population", generatedRoot).transform;
            var homes = new[] { "shrub-a", "central-park" };
            for (var index = 0; index < homes.Length; index++)
            {
                var home = FindRoomSpec(homes[index]);
                if (home == null)
                {
                    continue;
                }
                var center = GridToWorld(home);
                var spawnPosition = center + new Vector3(index == 0 ? 0.24f : -0.62f, 0.30f, index == 0 ? -0.22f : -0.74f);
                var hedgehogObject = NewObject($"Hedgehog {index + 1:00}", demoRoot);
                var hedgehog = hedgehogObject.AddComponent<HedgehogDemoAgent>();
                hedgehog.Initialize(
                    surfaceMaterial,
                    spawnPosition,
                    new Vector2(0.58f, 0.52f),
                    generatedHideFlags,
                    34900 + index);
                hedgehogAgents.Add(hedgehog);
            }
            hedgehogDemoAgent = hedgehogAgents.Count > 0 ? hedgehogAgents[0] : null;
        }

        private void BuildFoxAnimationDemo()
        {
            var den = FindRoomSpec("fox-den");
            if (den == null)
            {
                return;
            }
            var root = NewObject("Fox Population", generatedRoot).transform;
            var center = GridToWorld(den);
            for (var index = 0; index < AnimalPopulationDefaults.Foxes; index++)
            {
                var foxObject = NewObject($"Fox {index + 1:00}", root);
                var fox = foxObject.AddComponent<FoxDemoAgent>();
                fox.Initialize(
                    surfaceMaterial,
                    center + new Vector3(-0.18f, 0.30f, 0.12f),
                    new Vector2(0.34f, 0.34f),
                    generatedHideFlags,
                    44900 + index);
                foxAgents.Add(fox);
            }
            foxDemoAgent = foxAgents.Count > 0 ? foxAgents[0] : null;
        }

        private void BuildAnimalPopulation()
        {
            var populationObject = NewObject("Animal Population", generatedRoot);
            animalPopulationController = populationObject.AddComponent<AnimalPopulationController>();
            animalPopulationController.Initialize(pigeonAgents, squirrelAgents, hedgehogAgents, foxAgents);
            hud.BindAnimalPopulation(animalPopulationController);
        }

        private void BuildAnimalActivity()
        {
            var activityObject = NewObject("Animal Day Night Activity", generatedRoot);
            animalActivityController = activityObject.AddComponent<AnimalActivityController>();
            animalActivityController.Initialize(
                runtimeController,
                pigeonAgents,
                squirrelAgents,
                hedgehogAgents,
                foxAgents);
        }

        private void BuildAnimalNavigation()
        {
            var navigationObject = NewObject("Animal Navigation Coordinator", generatedRoot);
            animalNavigationCoordinator = navigationObject.AddComponent<AnimalNavigationCoordinator>();
            var agents = new List<IWildlifeLayoutAgent>();
            agents.AddRange(pigeonAgents);
            agents.AddRange(squirrelAgents);
            agents.AddRange(hedgehogAgents);
            agents.AddRange(foxAgents);
            animalNavigationCoordinator.Initialize(
                agents,
                layoutEditorController,
                mapRoot,
                cellSize);
        }

        private void BuildPlayerFeeding()
        {
            var foodRoot = NewObject("Player Food Sources", generatedRoot).transform;
            var feedingObject = NewObject("Player Feeding", generatedRoot);
            playerFeedingController = feedingObject.AddComponent<PlayerFeedingController>();
            playerFeedingController.Initialize(
                runtimeController,
                resourceEconomyController,
                layoutEditorController,
                animalNavigationCoordinator,
                garageTrafficController,
                LayoutCamera,
                mapRoot,
                foodRoot,
                surfaceMaterial,
                generatedHideFlags,
                pigeonAgents,
                squirrelAgents);
            hud.BindPlayerFeeding(playerFeedingController);
        }

        private void BuildGarageTraffic()
        {
            var trafficObject = NewObject("Continuous Garage Traffic", generatedRoot);
            garageTrafficController = trafficObject.AddComponent<GarageTrafficController>();
            garageTrafficController.Initialize(
                runtimeController,
                animalNavigationCoordinator,
                roomViews,
                surfaceMaterial,
                generatedHideFlags);
        }

        private void BuildSquirrelTreeResponse()
        {
            Transform parkTransform = null;
            foreach (var room in roomViews)
            {
                if (room.Spec.Id == "central-park")
                {
                    parkTransform = room.VisualRoot;
                    break;
                }
            }
            var responseObject = NewObject("Squirrel Tree Felling Response", generatedRoot);
            squirrelTreeResponseController = responseObject.AddComponent<SquirrelTreeResponseController>();
            squirrelTreeResponseController.Initialize(
                oakTreeLifecycleController,
                playerFeedingController,
                squirrelAgents,
                parkTransform);
        }

        private void BuildAnimalMortality()
        {
            var mortalityObject = NewObject("Animal Mortality", generatedRoot);
            animalMortalityController = mortalityObject.AddComponent<AnimalMortalityController>();
            var vitalities = new List<WildlifeVitality>();
            foreach (var squirrel in squirrelAgents)
            {
                vitalities.Add(squirrel.Vitality);
            }
            foreach (var hedgehog in hedgehogAgents)
            {
                vitalities.Add(hedgehog.Vitality);
            }
            foreach (var fox in foxAgents)
            {
                vitalities.Add(fox.Vitality);
            }
            animalMortalityController.Initialize(
                runtimeController,
                resourceEconomyController,
                residentPopulationController,
                oakTreeLifecycleController,
                layoutEditorController,
                wasteManagementController,
                pigeonAgents,
                vitalities);
        }

        private void BuildAnimalNeeds()
        {
            var needsObject = NewObject("Animal Daily Needs", generatedRoot);
            animalNeedsController = needsObject.AddComponent<AnimalNeedsController>();
            animalNeedsController.Initialize(
                runtimeController,
                resourceEconomyController,
                naturalFoodController,
                playerFeedingController,
                animalMortalityController,
                pigeonAgents,
                squirrelAgents,
                hedgehogAgents,
                foxAgents);
        }

        private void BuildFoxPredation()
        {
            var predationObject = NewObject("Fox Predation", generatedRoot);
            foxPredationController = predationObject.AddComponent<FoxPredationController>();
            foxPredationController.Initialize(
                runtimeController,
                animalNeedsController,
                animalMortalityController,
                animalNavigationCoordinator,
                garageTrafficController,
                mapRoot,
                foxAgents,
                pigeonAgents,
                squirrelAgents,
                hedgehogAgents);
        }

        private void BuildHedgehogForaging()
        {
            var foragingObject = NewObject("Hedgehog Night Foraging", generatedRoot);
            hedgehogForagingController = foragingObject.AddComponent<HedgehogForagingController>();
            hedgehogForagingController.Initialize(
                runtimeController,
                naturalFoodController,
                animalNeedsController,
                animalNavigationCoordinator,
                garageTrafficController,
                mapRoot,
                hedgehogAgents);
        }

        private void BuildSquirrelNaturalForaging()
        {
            var foragingObject = NewObject("Squirrel Natural Foraging", generatedRoot);
            squirrelNaturalForagingController = foragingObject.AddComponent<SquirrelNaturalForagingController>();
            squirrelNaturalForagingController.Initialize(
                runtimeController,
                naturalFoodController,
                animalNavigationCoordinator,
                garageTrafficController,
                mapRoot,
                squirrelAgents);
        }

        private void BuildEcologicalMetrics()
        {
            var metricsObject = NewObject("Live Ecological Metrics", generatedRoot);
            ecologicalMetricsController = metricsObject.AddComponent<EcologicalMetricsController>();
            ecologicalMetricsController.Initialize(
                wasteManagementController,
                residentPopulationController,
                naturalFoodController,
                playerFeedingController,
                animalPopulationController,
                oakTreeLifecycleController,
                animalNeedsController,
                animalMortalityController);
            hud.BindEcologicalMetrics(ecologicalMetricsController);
        }

        private void BuildResearchSession()
        {
            var researchObject = NewObject("Research Session", generatedRoot);
            researchSessionController = researchObject.AddComponent<ResearchSessionController>();
            researchSessionController.Initialize(
                runtimeController,
                layoutEditorController,
                animalMortalityController,
                ecologicalMetricsController);
            hud.BindResearchSession(researchSessionController);
            resultsOverlay.BindResearchSession(researchSessionController);
        }

        private void BuildFirstRunOnboarding()
        {
            var onboardingObject = NewObject("First Run Onboarding", generatedRoot);
            onboardingController = onboardingObject.AddComponent<FirstRunOnboardingController>();
            onboardingController.Initialize(
                runtimeController,
                layoutEditorController,
                playerFeedingController,
                residentPopulationController,
                citizenDemoAgent,
                roomViews,
                hud,
                LayoutCamera,
                UrbanFontResolver.GetFont(),
                generatedHideFlags);
            hud.BindOnboarding(onboardingController);
        }

        private static RoomSpec FindRoomSpec(string roomId)
        {
            foreach (var room in RoomLayoutData.All)
            {
                if (room.Id == roomId)
                {
                    return room;
                }
            }
            return null;
        }

        private void SelectRoom(RoomView room)
        {
            if (selectedRoom != null && selectedRoom != room)
            {
                selectedRoom.SetSelected(false);
            }

            selectedRoom = room;
            selectedRoom.SetSelected(true);
            hud.ShowRoom(room.Spec, room.transform.parent);
        }

        private void HandleRoomHover(RoomView room, bool hovering)
        {
            if (room == null || hud == null)
            {
                return;
            }

            if (hovering && runtimeController != null && !runtimeController.LayoutEditing)
            {
                hud.ShowRoomHover(room.Spec, room.VisualRoot);
            }
            else
            {
                hud.HideRoomHover(room.Spec);
            }
        }

        private void FocusRoom(RoomView room)
        {
            if (room == null || boardCameraController == null)
            {
                return;
            }

            var footprint = Mathf.Max(room.Spec.Width, room.Spec.Height) * cellSize;
            boardCameraController.FocusRoom(room.transform.parent, footprint);
        }

        private Vector3 GridToWorld(RoomSpec room)
        {
            var x = (room.Column + room.Width * 0.5f - RoomLayoutData.GridSize * 0.5f) * cellSize;
            var z = (RoomLayoutData.GridSize * 0.5f - room.Row - room.Height * 0.5f) * cellSize;
            return new Vector3(x, 0f, z);
        }

        private GameObject NewObject(string objectName, Transform parent)
        {
            var instance = new GameObject(objectName)
            {
                hideFlags = generatedHideFlags
            };
            instance.transform.SetParent(parent, false);
            return instance;
        }

        private GameObject CreateBox(string objectName, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            return UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cube,
                objectName,
                parent,
                position,
                scale,
                color,
                surfaceMaterial,
                true,
                generatedHideFlags);
        }

        private GameObject CreateCylinder(string objectName, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            return UrbanVisualFactory.CreatePrimitive(
                PrimitiveType.Cylinder,
                objectName,
                parent,
                position,
                scale,
                color,
                surfaceMaterial,
                true,
                generatedHideFlags);
        }

        private void ClearGenerated()
        {
            if (runtimeController != null)
            {
                runtimeController.RestartRequested -= HandleRestartRequested;
            }

            if (generatedRoot != null)
            {
                DestroyGeneratedObject(generatedRoot.gameObject);
            }
            else
            {
                var existing = transform.Find(GeneratedRootName);
                if (existing != null)
                {
                    DestroyGeneratedObject(existing.gameObject);
                }
            }

            generatedRoot = null;
            LayoutCamera = null;
            hud = null;
            resultsOverlay = null;
            boardCameraController = null;
            runtimeController = null;
            layoutEditorController = null;
            persistenceController = null;
            physicalBoardCameraController = null;
            wasteManagementController = null;
            resourceEconomyController = null;
            residentPopulationController = null;
            oakTreeLifecycleController = null;
            naturalFoodController = null;
            animalNavigationCoordinator = null;
            playerFeedingController = null;
            animalMortalityController = null;
            animalPopulationController = null;
            animalNeedsController = null;
            garageTrafficController = null;
            animalActivityController = null;
            squirrelTreeResponseController = null;
            foxPredationController = null;
            hedgehogForagingController = null;
            squirrelNaturalForagingController = null;
            ecologicalMetricsController = null;
            researchSessionController = null;
            onboardingController = null;
            mapRoot = null;
            citizenDemoAgent = null;
            pigeonDemoAgent = null;
            squirrelDemoAgent = null;
            hedgehogDemoAgent = null;
            foxDemoAgent = null;
            pigeonAgents.Clear();
            squirrelAgents.Clear();
            hedgehogAgents.Clear();
            foxAgents.Clear();
            selectedRoom = null;
            roomViews.Clear();
            wasteRoomVisuals.Clear();
            oakTreeVisuals.Clear();

            if (surfaceMaterial != null)
            {
                DestroyGeneratedObject(surfaceMaterial);
                surfaceMaterial = null;
            }
        }

        private static void DestroyGeneratedObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
