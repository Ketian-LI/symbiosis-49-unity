using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private bool showRoomLabels;

        private readonly List<RoomView> roomViews = new();
        private readonly Dictionary<string, WasteRoomLoadVisual> wasteRoomVisuals = new();
        private readonly Dictionary<string, OakTreeStageVisual> oakTreeVisuals = new();
        private readonly List<PigeonDemoAgent> pigeonAgents = new();
        private readonly Dictionary<PigeonDemoAgent, string> pigeonHomeRooms = new();
        private readonly Dictionary<IWildlifeLayoutAgent, string> wildlifeHomeRooms = new();
        private readonly List<SquirrelDemoAgent> squirrelAgents = new();
        private readonly List<HedgehogDemoAgent> hedgehogAgents = new();
        private readonly List<FoxDemoAgent> foxAgents = new();
        private Transform generatedRoot;
        private Material surfaceMaterial;
        private Material tabletopMaterial;
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
        private WorkerPasserbyFeedingController workerFeedingController;
        private GarageTrafficController garageTrafficController;
        private AnimalActivityController animalActivityController;
        private SquirrelTreeResponseController squirrelTreeResponseController;
        private FoxPredationController foxPredationController;
        private HedgehogForagingController hedgehogForagingController;
        private HedgehogSocialController hedgehogSocialController;
        private DailyOutcomeController dailyOutcomeController;
        private SquirrelNaturalForagingController squirrelNaturalForagingController;
        private PigeonNaturalForagingController pigeonNaturalForagingController;
        private EcologicalMetricsController ecologicalMetricsController;
        private ResearchSessionController researchSessionController;
        private FirstRunOnboardingController onboardingController;
        private Transform mapRoot;
        private CitizenPopulationPresenter citizenPopulationPresenter;
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
                if (!Application.isPlaying)
                {
                    generatedRoot = existing;
                    return;
                }

                // A generated editor preview can be embedded in a player build.
                // Its runtime references are not initialized, so rebuild it for play.
                existing.gameObject.SetActive(false);
                Destroy(existing.gameObject);
            }

            BuildLayout();
        }

        private void BuildLayout()
        {
            isBuilding = true;
            generatedHideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSaveInEditor;
            roomViews.Clear();
            pigeonHomeRooms.Clear();
            wildlifeHomeRooms.Clear();
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
            BuildGameplayTabletopBackdrop();
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
            layoutEditorController.BindPigeonHomes(pigeonHomeRooms);
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
            BuildWorkerPasserbyFeeding();
            BuildHedgehogForaging();
            BuildHedgehogSocial();
            BuildFoxPredation();
            BuildSquirrelNaturalForaging();
            BuildPigeonNaturalForaging();
            BuildEcologicalMetrics();
            BuildDailyOutcome();
            BuildResearchSession();
            BuildFirstRunOnboarding();
            BuildSessionPersistence();

            if (validationErrors.Count == 0)
            {
                Debug.Log("[Urban Wildlife Layout] 7×7 layout validated: 49 single-cell rooms occupy all 49 cells.", this);
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
            camera.orthographicSize = 12.9f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UrbanPalette.Background;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 180f;
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

        private void BuildGameplayTabletopBackdrop()
        {
            const string resourcePath = "UI/GameplayHud/gameplay-tabletop-backdrop-v01";
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"[Urban Wildlife Layout] Missing gameplay tabletop backdrop: {resourcePath}", this);
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Texture") ??
                         Shader.Find("Standard");
            tabletopMaterial = new Material(shader)
            {
                name = "Gameplay Tabletop Backdrop Material",
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = texture
            };
            if (tabletopMaterial.HasProperty("_BaseColor"))
            {
                tabletopMaterial.SetColor("_BaseColor", Color.white);
            }

            // A world-space plane sits below the board, so it never blocks room selection
            // or bakes a non-interactive copy of the room layout into the background.
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Plane);
            backdrop.name = "Gameplay Tabletop Backdrop";
            backdrop.hideFlags = generatedHideFlags;
            backdrop.transform.SetParent(generatedRoot, false);
            backdrop.transform.localPosition = new Vector3(0f, -0.58f, 0f);
            // Unity's Plane UV appears 180 degrees rotated from the top-down camera.
            backdrop.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            backdrop.transform.localScale = new Vector3(5.6f, 1f, 3.15f);
            backdrop.AddComponent<TabletopBackdropAlignment>().Initialize(LayoutCamera);
            var renderer = backdrop.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = tabletopMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var collider = backdrop.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyGeneratedObject(collider);
            }
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
            if (spec.Type is RoomType.Residence or RoomType.Office or
                RoomType.Supermarket or RoomType.PigeonHabitat)
            {
                BuildInteriorFloorDetails(shellRoot, spec.Type, width, depth);
            }
            if (spec.Type == RoomType.Canteen)
            {
                BuildFoodShopFloorDetails(shellRoot, width, depth);
            }
            if (spec.Type == RoomType.CentralPark)
            {
                BuildCentralParkGroundDetails(shellRoot, spec);
            }
            if (spec.Type is RoomType.SharedSpace or RoomType.CommunitySquare)
            {
                BuildSharedSpaceGroundDetails(shellRoot, spec, width, depth);
            }
            if (spec.Type == RoomType.EcologicalBuffer)
            {
                CreateBox("Buffer Meadow", shellRoot, new Vector3(0f, 0.246f, 0f),
                    new Vector3(width - 0.10f, 0.010f, depth - 0.10f),
                    new Color(0.57f, 0.66f, 0.43f));
            }
            if (spec.Type == RoomType.ShrubHabitat)
            {
                BuildShrubGroundRoute(shellRoot, spec);
            }
            if (spec.Type == RoomType.OakHabitat)
            {
                BuildOakGroundTexture(shellRoot, width, depth);
                BuildAuthoredGroundRoute(shellRoot, spec, new Color(0.70f, 0.61f, 0.43f), 0.25f);
            }
            if (spec.Type == RoomType.FoxDen)
            {
                BuildFoxGroundTexture(shellRoot);
            }
            if (spec.Type == RoomType.Garage)
            {
                GarageVisualLayout.BuildShellDetails(shellRoot, spec, width, depth, surfaceMaterial, generatedHideFlags);
            }
            if (spec.Type == RoomType.Trash)
            {
                BuildWasteRoomFloorDetails(shellRoot);
            }
            AnimalPassageVisual.Build(shellRoot, spec, cellSize, roomGap,
                surfaceMaterial, generatedHideFlags);
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
                if (RoomInteriorVisualBuilder.IsPassThroughFurnishing(renderer))
                {
                    continue;
                }

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
            const float postWidth = 0.055f;
            var name = $"{doorway.Edge} Door {doorway.SegmentIndex + 1}";
            var horizontal = doorway.Edge is RoomEdge.North or RoomEdge.South;

            var thresholdScale = horizontal
                ? new Vector3(doorwayWidth, 0.018f, wallThickness * 0.72f)
                : new Vector3(wallThickness * 0.72f, 0.018f, doorwayWidth);
            CreateBox(
                $"{name} Threshold",
                parent,
                doorway.LocalCenter + new Vector3(0f, 0.255f, 0f),
                thresholdScale,
                UrbanPalette.Doorframe);

            var postScale = horizontal
                ? new Vector3(postWidth, wallHeight, wallThickness * 0.82f)
                : new Vector3(wallThickness * 0.82f, wallHeight, postWidth);
            var postOffset = horizontal
                ? new Vector3(doorwayWidth * 0.5f, 0f, 0f)
                : new Vector3(0f, 0f, doorwayWidth * 0.5f);
            CreateBox($"{name} Post A", parent, doorway.LocalCenter + postOffset + new Vector3(0f, wallY, 0f), postScale, UrbanPalette.Doorframe);
            CreateBox($"{name} Post B", parent, doorway.LocalCenter - postOffset + new Vector3(0f, wallY, 0f), postScale, UrbanPalette.Doorframe);

            var lintelScale = horizontal
                ? new Vector3(doorwayWidth + postWidth * 2f, 0.026f, wallThickness * 0.40f)
                : new Vector3(wallThickness * 0.40f, 0.026f, doorwayWidth + postWidth * 2f);
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
                    CreatePond(parent, new Vector3(-0.78f, 0.34f, -0.78f), 0.30f);
                    CreateTree(parent, new Vector3(-0.96f, 0f, 0.88f), 0.55f, false);
                    CreateBush(parent, new Vector3(0.88f, 0f, -0.76f), 0.38f);
                    break;
                case RoomType.SharedSpace:
                    var variant = spec.Id[spec.Id.Length - 1] - 'a';
                    CreateBush(parent,
                        new Vector3(variant % 2 == 0 ? -0.92f : 0.92f, 0f, 0.82f), 0.35f);
                    break;
                case RoomType.EcologicalBuffer:
                    CreateBush(parent, new Vector3(-0.92f, 0f, 0.88f), 0.40f);
                    CreateBush(parent, new Vector3(0.92f, 0f, -0.88f), 0.40f);
                    break;
                case RoomType.CommunitySquare:
                    CreateBush(parent, new Vector3(-0.98f, 0f, 0.95f), 0.28f);
                    CreateBush(parent, new Vector3(0.98f, 0f, -0.95f), 0.28f);
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

        private void BuildCentralParkGroundDetails(Transform parent, RoomSpec spec)
        {
            CreateBox("Park Meadow Surface", parent, new Vector3(0f, 0.245f, 0f),
                new Vector3(2.84f, 0.008f, 2.84f), new Color(0.49f, 0.59f, 0.30f));
            var path = new Color(0.76f, 0.66f, 0.49f);
            BuildAuthoredGroundRoute(parent, spec, path, 0.35f);

            var meadowTufts = new[]
            {
                new Vector3(-1.11f, 0f, 1.12f), new Vector3(-1.14f, 0f, -0.94f),
                new Vector3(-0.84f, 0f, -1.08f), new Vector3(1.08f, 0f, 1.12f),
                new Vector3(1.14f, 0f, -1.04f)
            };
            for (var index = 0; index < meadowTufts.Length; index++)
            {
                CreateGroundTuft(parent, $"Park Meadow Tuft {index + 1}", meadowTufts[index],
                    index % 3 == 0 ? 0.16f : 0.12f,
                    index % 2 == 0 ? new Color(0.30f, 0.47f, 0.23f) : new Color(0.43f, 0.55f, 0.27f));
            }
            for (var index = 0; index < 4; index++)
            {
                var point = new Vector3(-1.08f + index * 0.71f, 0.260f,
                    index % 2 == 0 ? -1.12f : 1.13f);
                CreateCylinder($"Park Wildflower {index + 1}", parent, point,
                    new Vector3(0.045f, 0.006f, 0.045f),
                    index % 3 == 0 ? new Color(0.94f, 0.81f, 0.36f) : new Color(0.93f, 0.87f, 0.72f));
            }

            // The park remains the fixed anchor; specialist wildlife refuges
            // are now movable green rooms connected by the animal route graph.
            CreateBox("Pigeon Resting Perch", parent,
                new Vector3(0f, 0.283f, 1.03f), new Vector3(0.42f, 0.065f, 0.24f),
                new Color(0.86f, 0.83f, 0.70f));

            var restingColor = new Color(0.94f, 0.51f, 0.25f);
            var westRest = CreateBox("Park Rest Warning West", parent,
                new Vector3(-1.32f, 0.30f, 0f), new Vector3(0.18f, 0.025f, 0.95f), restingColor);
            var eastRest = CreateBox("Park Rest Warning East", parent,
                new Vector3(1.32f, 0.30f, 0f), new Vector3(0.18f, 0.025f, 0.95f), restingColor);
            var southRest = CreateBox("Park Rest Warning South", parent,
                new Vector3(0f, 0.30f, -1.32f), new Vector3(0.95f, 0.025f, 0.18f), restingColor);
            var northRest = CreateBox("Park Rest Warning North", parent,
                new Vector3(0f, 0.30f, 1.32f), new Vector3(0.95f, 0.025f, 0.18f), restingColor);
            parent.gameObject.AddComponent<ParkEdgeRestVisual>().Initialize(
                runtimeController, westRest, eastRest, southRest, northRest);

            // Small faceted edging makes the pond legible from the full-board camera.
            for (var index = 0; index < 9; index++)
            {
                var angle = index * Mathf.PI * 2f / 9f;
                var x = -0.78f + Mathf.Cos(angle) * 0.20f;
                var z = -0.78f + Mathf.Sin(angle) * 0.14f;
                var stone = CreateBox($"Pond Edge Stone {index + 1}", parent,
                    new Vector3(x, 0.28f, z),
                    new Vector3(0.14f + index % 3 * 0.03f, 0.075f, 0.13f),
                    index % 2 == 0
                        ? new Color(0.58f, 0.59f, 0.54f)
                        : new Color(0.66f, 0.64f, 0.57f));
                stone.transform.localRotation = Quaternion.Euler(0f, index * 31f, 0f);
            }

        }

        private void BuildSharedSpaceGroundDetails(
            Transform parent, RoomSpec spec, float width, float depth)
        {
            var green = spec.IsGreen;
            CreateBox(green ? "Linked Green Meadow" : "Shared Space Stone Paving", parent,
                new Vector3(0f, 0.246f, 0f),
                new Vector3(width - 0.10f, 0.010f, depth - 0.10f),
                green ? new Color(0.48f, 0.59f, 0.34f) : new Color(0.72f, 0.70f, 0.62f));
            BuildAuthoredGroundRoute(parent, spec,
                green ? new Color(0.77f, 0.68f, 0.50f) : new Color(0.84f, 0.77f, 0.64f),
                green ? 0.32f : 0.38f);
            if (green)
            {
                CreateGroundTuft(parent, $"{spec.Id} Green Niche", new Vector3(-0.88f, 0f, 0.88f),
                    0.18f, new Color(0.33f, 0.50f, 0.26f));
                switch (spec.GreenRole)
                {
                    case ParkGreenRole.SquirrelGrove:
                        CreateCylinder("Squirrel Grove Cache", parent,
                            new Vector3(-0.78f, 0.29f, -0.80f), new Vector3(0.24f, 0.035f, 0.24f),
                            new Color(0.61f, 0.40f, 0.23f));
                        break;
                    case ParkGreenRole.HedgehogGarden:
                        CreateBox("Hedgehog Garden Shelter", parent,
                            new Vector3(-0.80f, 0.30f, -0.80f), new Vector3(0.42f, 0.08f, 0.27f),
                            new Color(0.37f, 0.49f, 0.28f));
                        break;
                    case ParkGreenRole.FoxEdge:
                        CreateBox("Fox Edge Nook", parent,
                            new Vector3(-0.78f, 0.30f, -0.78f), new Vector3(0.38f, 0.08f, 0.38f),
                            new Color(0.59f, 0.36f, 0.23f));
                        break;
                    case ParkGreenRole.Connector:
                        CreateCylinder("Green Connector Flower", parent,
                            new Vector3(0.82f, 0.29f, -0.82f), new Vector3(0.12f, 0.035f, 0.12f),
                            new Color(0.92f, 0.81f, 0.53f));
                        break;
                }
            }
        }

        private void BuildShrubGroundRoute(Transform parent, RoomSpec spec)
        {
            BuildAuthoredGroundRoute(parent, spec, UrbanPalette.ShrubPathLight, 0.28f);
            var edgeTufts = new[]
            {
                new Vector3(-1.12f, 0f, 0.42f), new Vector3(-1.08f, 0f, -0.38f),
                new Vector3(1.10f, 0f, 0.73f), new Vector3(1.09f, 0f, -0.71f),
                new Vector3(-0.82f, 0f, 1.18f), new Vector3(0.81f, 0f, -1.17f)
            };
            for (var index = 0; index < edgeTufts.Length; index++)
            {
                CreateGroundTuft(parent, $"{spec.Id} Meadow Edge {index + 1}", edgeTufts[index],
                    0.12f + index % 2 * 0.04f,
                    index % 2 == 0 ? UrbanPalette.ShrubDeepLeaf : UrbanPalette.ShrubLeaf);
            }
        }

        private void BuildAuthoredGroundRoute(Transform parent, RoomSpec spec, Color color, float width)
        {
            GroundTrailVisualBuilder.Build(parent, spec, cellSize, roomGap,
                color, width, surfaceMaterial, generatedHideFlags);
        }

        private void BuildOakGroundTexture(Transform parent, float width, float depth)
        {
            CreateBox("Oak Meadow Surface", parent, new Vector3(0f, 0.245f, 0f),
                new Vector3(width - 0.08f, 0.008f, depth - 0.08f),
                new Color(0.48f, 0.59f, 0.30f));
            var treeX = -width * 0.5f + 0.56f;
            var treeZ = depth * 0.5f - 0.56f;
            CreateCylinder("Oak Root Earth", parent, new Vector3(treeX, 0.252f, treeZ),
                new Vector3(0.47f, 0.006f, 0.42f), new Color(0.50f, 0.43f, 0.28f));
            var tufts = new[]
            {
                new Vector3(-0.92f, 0f, -0.96f), new Vector3(-1.05f, 0f, 0.16f),
                new Vector3(0.82f, 0f, 1.00f), new Vector3(1.08f, 0f, -0.87f),
                new Vector3(0.17f, 0f, -1.07f), new Vector3(0.92f, 0f, 0.22f)
            };
            for (var index = 0; index < tufts.Length; index++)
            {
                var position = tufts[index];
                position.x = Mathf.Clamp(position.x, -width * 0.5f + 0.20f, width * 0.5f - 0.20f);
                position.z = Mathf.Clamp(position.z, -depth * 0.5f + 0.20f, depth * 0.5f - 0.20f);
                CreateGroundTuft(parent, $"Oak Meadow Tuft {index + 1}", position,
                    index % 2 == 0 ? 0.16f : 0.11f,
                    index % 2 == 0 ? new Color(0.29f, 0.44f, 0.20f) : new Color(0.55f, 0.63f, 0.32f));
            }
        }

        private void BuildFoxGroundTexture(Transform parent)
        {
            var dryLeaf = UrbanPalette.FoxDryLeaf;
            var moss = UrbanPalette.FoxLeaf;
            var details = new[]
            {
                new Vector3(-1.10f, 0f, 0.22f), new Vector3(-0.68f, 0f, -1.02f),
                new Vector3(1.02f, 0f, 0.30f), new Vector3(0.92f, 0f, -1.08f)
            };
            for (var index = 0; index < details.Length; index++)
            {
                CreateGroundTuft(parent, $"Fox Den Moss {index + 1}", details[index],
                    0.12f + index % 2 * 0.05f, moss);
                var leaf = CreateBox($"Fox Den Dry Leaf {index + 1}", parent,
                    details[index] + new Vector3(0.14f, 0.259f, -0.12f),
                    new Vector3(0.12f, 0.009f, 0.055f), dryLeaf);
                leaf.transform.localRotation = Quaternion.Euler(0f, 24f + index * 37f, 0f);
            }
        }

        private void CreateGroundTuft(Transform parent, string name, Vector3 position, float size, Color color)
        {
            for (var blade = 0; blade < 3; blade++)
            {
                var tuft = CreateBox($"{name} Blade {blade + 1}", parent,
                    position + new Vector3(0f, 0.258f + blade * 0.001f, 0f),
                    new Vector3(size * 0.28f, 0.009f, size),
                    blade == 1 ? Color.Lerp(color, Color.white, 0.13f) : color);
                tuft.transform.localRotation = Quaternion.Euler(0f, blade * 60f + 16f, 0f);
            }
        }

        private void BuildInteriorFloorDetails(Transform parent, RoomType type, float width, float depth)
        {
            // All surface inlays are renderer-only, leaving the clickable floor collider and
            // authored door navigation unchanged. The room's furnishings remain a separate kit.
            switch (type)
            {
                case RoomType.Residence:
                    CreateBox("Residence Honey Timber Base", parent, new Vector3(0f, 0.246f, 0f),
                        new Vector3(width - 0.08f, 0.012f, depth - 0.08f),
                        new Color(0.74f, 0.57f, 0.38f));
                    var plankCount = Mathf.Max(1, Mathf.RoundToInt(depth / 0.34f));
                    var plankStep = (depth - 0.10f) / plankCount;
                    for (var index = 0; index < plankCount; index++)
                    {
                        var z = -(depth - 0.10f) * 0.5f + (index + 0.5f) * plankStep;
                        CreateBox($"Residence Timber Board {index + 1}", parent,
                            new Vector3(0f, 0.254f, z),
                            new Vector3(width - 0.12f, 0.006f, plankStep - 0.009f),
                            index % 3 == 0 ? new Color(0.83f, 0.67f, 0.47f) :
                            index % 3 == 1 ? new Color(0.79f, 0.62f, 0.43f) :
                            new Color(0.86f, 0.70f, 0.50f));
                        var jointCount = width > 4f ? 2 : 1;
                        for (var joint = 0; joint < jointCount; joint++)
                        {
                            var jointX = jointCount == 1
                                ? (index % 2 == 0 ? -0.52f : 0.52f)
                                : (joint == 0 ? -1.40f : 1.36f) + (index % 2 == 0 ? -0.22f : 0.22f);
                            CreateBox($"Residence Timber Staggered Joint {index + 1}-{joint + 1}", parent,
                                new Vector3(jointX, 0.260f, z),
                                new Vector3(0.011f, 0.003f, plankStep - 0.014f),
                                new Color(0.70f, 0.53f, 0.36f));
                        }
                    }
                    // The slim oval rug is walkable and sits next to the bed,
                    // not in the spare southwest furnishing corner.
                    var rugX = -width * 0.5f + 1.16f;
                    CreateCylinder("Small Oval Rug", parent, new Vector3(rugX, 0.264f, 0.19f),
                        new Vector3(0.59f, 0.004f, 0.84f), new Color(0.49f, 0.55f, 0.43f));
                    CreateCylinder("Small Oval Rug Ivory Band", parent, new Vector3(rugX, 0.270f, 0.19f),
                        new Vector3(0.47f, 0.003f, 0.70f), new Color(0.89f, 0.82f, 0.68f));
                    CreateCylinder("Small Oval Rug Centre", parent, new Vector3(rugX, 0.275f, 0.19f),
                        new Vector3(0.29f, 0.003f, 0.50f), new Color(0.60f, 0.65f, 0.51f));
                    break;
                case RoomType.Office:
                    CreateBox("Office Lilac Carpet", parent, new Vector3(0f, 0.248f, 0f),
                        new Vector3(width - 0.08f, 0.016f, depth - 0.08f),
                        new Color(0.49f, 0.43f, 0.57f));
                    break;
                case RoomType.Supermarket:
                    CreateBox("Supermarket Cream Tile Base", parent, new Vector3(0f, 0.248f, 0f),
                        new Vector3(width - 0.08f, 0.016f, depth - 0.08f),
                        new Color(0.84f, 0.79f, 0.66f));
                    const float tileSize = 0.36f;
                    for (var x = -width * 0.5f + tileSize; x < width * 0.5f - 0.05f; x += tileSize)
                    {
                        CreateBox("Supermarket Tile Joint X", parent,
                            new Vector3(x, 0.259f, 0f),
                            new Vector3(0.009f, 0.004f, depth - 0.08f),
                            new Color(0.73f, 0.68f, 0.58f));
                    }
                    for (var z = -depth * 0.5f + tileSize; z < depth * 0.5f - 0.05f; z += tileSize)
                    {
                        CreateBox("Supermarket Tile Joint Z", parent,
                            new Vector3(0f, 0.259f, z),
                            new Vector3(width - 0.08f, 0.004f, 0.009f),
                            new Color(0.73f, 0.68f, 0.58f));
                    }
                    break;
                case RoomType.PigeonHabitat:
                    // A paved pocket plaza rather than an industrial rooftop.
                    // These are thin, collider-free floor details; the centre stays walkable.
                    CreateBox("Pigeon Plaza Stone Paving", parent, new Vector3(0f, 0.247f, 0f),
                        new Vector3(width - 0.08f, 0.014f, depth - 0.08f),
                        new Color(0.70f, 0.67f, 0.61f));
                    const float plazaPaverStep = 0.48f;
                    for (var x = -width * 0.5f + plazaPaverStep; x < width * 0.5f - 0.10f; x += plazaPaverStep)
                    {
                        CreateBox("Plaza Paver Joint X", parent, new Vector3(x, 0.256f, 0f),
                            new Vector3(0.010f, 0.003f, depth - 0.08f), new Color(0.58f, 0.56f, 0.52f));
                    }
                    for (var z = -depth * 0.5f + plazaPaverStep; z < depth * 0.5f - 0.10f; z += plazaPaverStep)
                    {
                        CreateBox("Plaza Paver Joint Z", parent, new Vector3(0f, 0.256f, z),
                            new Vector3(width - 0.08f, 0.003f, 0.010f), new Color(0.58f, 0.56f, 0.52f));
                    }
                    CreateCylinder("Plaza Circular Paving Border", parent, new Vector3(0f, 0.261f, 0f),
                        new Vector3(0.90f, 0.002f, 0.90f), new Color(0.83f, 0.77f, 0.66f));
                    CreateCylinder("Plaza Circular Paving Centre", parent, new Vector3(0f, 0.265f, 0f),
                        new Vector3(0.74f, 0.002f, 0.74f), new Color(0.69f, 0.65f, 0.58f));
                    break;
            }
        }

        private void BuildFoodShopFloorDetails(Transform parent, float width, float depth)
        {
            // Thin, collider-free boards are part of the floor shell, not navigation obstacles.
            // Rotate the grain with the 1x2 footprint so both shops read as the same room kit.
            var horizontal = width >= depth;
            var across = horizontal ? depth : width;
            var along = horizontal ? width : depth;
            var plankCount = Mathf.Max(1, Mathf.RoundToInt(across / 0.27f));
            var step = across / plankCount;
            var tones = new[]
            {
                new Color(0.72f, 0.49f, 0.30f),
                new Color(0.76f, 0.53f, 0.33f),
                new Color(0.69f, 0.46f, 0.27f),
                new Color(0.73f, 0.50f, 0.31f)
            };

            for (var index = 0; index < plankCount; index++)
            {
                var coordinate = -across * 0.5f + (index + 0.5f) * step;
                var position = horizontal
                    ? new Vector3(0f, 0.247f, coordinate)
                    : new Vector3(coordinate, 0.247f, 0f);
                var size = horizontal
                    ? new Vector3(along - 0.08f, 0.012f, step - 0.007f)
                    : new Vector3(step - 0.007f, 0.012f, along - 0.08f);
                CreateBox($"Food Shop Wood Plank {index + 1}", parent, position, size,
                    tones[(index * 3 + index / 4) % tones.Length]);
            }

            // One shared dining rug makes the pair of two-person tables read as a
            // restaurant dining zone rather than two unrelated desks. It is a
            // floor inlay, not a pathfinding obstacle or a movable furnishing.
            var diningRug = NewObject("Food Shop Shared Dining Rug", parent).transform;
            if (!horizontal)
            {
                diningRug.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
            var rug = new Color(0.66f, 0.38f, 0.31f);
            var rugBorder = new Color(0.85f, 0.67f, 0.53f);
            CreateBox("Shared Dining Rug", diningRug, new Vector3(0f, 0.259f, -0.12f),
                new Vector3(2.62f, 0.012f, 1.08f), rug);
            foreach (var side in new[] { -1f, 1f })
            {
                CreateBox("Dining Rug Long Border", diningRug,
                    new Vector3(0f, 0.267f, -0.12f + side * 0.45f),
                    new Vector3(2.42f, 0.005f, 0.028f), rugBorder);
                CreateBox("Dining Rug Short Border", diningRug,
                    new Vector3(side * 1.20f, 0.267f, -0.12f),
                    new Vector3(0.028f, 0.005f, 0.90f), rugBorder);
            }

            for (var side = -1; side <= 1; side += 2)
            {
                var x = side * 0.82f;
                const float z = 0.43f;
                var fixture = horizontal ? new Vector3(x, 0f, z) : new Vector3(z, 0f, -x);
                CreateCylinder($"Food Shop Pendant {side}", parent,
                    fixture + new Vector3(0f, 1.02f, 0f), new Vector3(0.18f, 0.065f, 0.18f),
                    new Color(0.77f, 0.39f, 0.20f));
                CreateCylinder($"Food Shop Pendant Stem {side}", parent,
                    fixture + new Vector3(0f, 1.20f, 0f), new Vector3(0.012f, 0.12f, 0.012f),
                    new Color(0.39f, 0.25f, 0.17f));
                CreateCylinder($"Food Shop Pendant Glow {side}", parent,
                    fixture + new Vector3(0f, 0.94f, 0f), new Vector3(0.07f, 0.018f, 0.07f),
                    new Color(1f, 0.90f, 0.68f));
            }

            // A small physical wall plaque identifies the restaurant in the angled menu view.
            // It is a shell detail, so it does not consume floor space or block a doorway.
            var wallArt = NewObject("Food Shop Wall Sign", parent).transform;
            if (!horizontal)
            {
                wallArt.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
            var northWall = across * 0.5f - 0.085f;
            CreateBox("Restaurant Sign Frame", wallArt, new Vector3(0f, 0.43f, northWall),
                new Vector3(0.68f, 0.26f, 0.035f), new Color(0.48f, 0.29f, 0.16f));
            CreateBox("Restaurant Sign Paper", wallArt, new Vector3(0f, 0.43f, northWall - 0.025f),
                new Vector3(0.60f, 0.20f, 0.012f), new Color(0.93f, 0.83f, 0.66f));
            CreateBox("Restaurant Sign Bowl", wallArt, new Vector3(0f, 0.39f, northWall - 0.034f),
                new Vector3(0.23f, 0.045f, 0.012f), new Color(0.54f, 0.28f, 0.14f));
            for (var steam = -1; steam <= 1; steam++)
            {
                CreateBox($"Restaurant Sign Steam {steam}", wallArt,
                    new Vector3(steam * 0.06f, 0.48f, northWall - 0.034f),
                    new Vector3(0.012f, 0.055f, 0.012f), new Color(0.54f, 0.28f, 0.14f));
            }
        }

        private void BuildWasteRoomFloorDetails(Transform parent)
        {
            const float routeHalfLength = 1.37f;
            const float brickLength = 0.21f;
            const float brickWidth = 0.30f;
            const float brickGap = 0.018f;
            const float brickY = 0.258f;
            var tile = new Color(0.58f, 0.59f, 0.61f);
            var grout = new Color(0.46f, 0.47f, 0.50f);
            var brickColourA = new Color(0.87f, 0.82f, 0.72f);
            var brickColourB = new Color(0.81f, 0.77f, 0.68f);
            CreateBox("Waste Grey Tile Base", parent, new Vector3(0f, 0.247f, 0f),
                new Vector3(2.84f, 0.014f, 2.84f), tile);
            for (var joint = -3; joint <= 3; joint++)
            {
                var coordinate = joint * 0.39f;
                CreateBox("Waste Tile Joint X", parent, new Vector3(coordinate, 0.256f, 0f),
                    new Vector3(0.008f, 0.003f, 2.80f), grout);
                CreateBox("Waste Tile Joint Z", parent, new Vector3(0f, 0.256f, coordinate),
                    new Vector3(2.80f, 0.003f, 0.008f), grout);
            }
            CreateBox("Waste North South Path Bedding", parent, new Vector3(0f, 0.258f, 0f),
                new Vector3(0.65f, 0.008f, 2.80f), new Color(0.69f, 0.64f, 0.56f));
            CreateBox("Waste East West Path Bedding", parent, new Vector3(0f, 0.258f, 0f),
                new Vector3(2.80f, 0.008f, 0.65f), new Color(0.69f, 0.64f, 0.56f));
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
            CreateBox("Central Waste Drain Rim", parent, new Vector3(0f, 0.322f, 0f),
                new Vector3(0.38f, 0.012f, 0.38f), new Color(0.54f, 0.53f, 0.49f));
            CreateBox("Central Waste Drain Grate", parent, new Vector3(0f, 0.330f, 0f),
                new Vector3(0.31f, 0.008f, 0.31f), new Color(0.24f, 0.25f, 0.26f));
            for (var slot = -1; slot <= 1; slot++)
            {
                CreateBox("Central Waste Drain Slot", parent,
                    new Vector3(slot * 0.075f, 0.335f, 0f),
                    new Vector3(0.025f, 0.003f, 0.23f), new Color(0.075f, 0.08f, 0.09f));
            }
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
            CreateTree(young.transform, localPosition, 0.64f, true);

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
            text.text = "7×7 模块化房间 · 49 间单格房 · 无走廊";
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
            animalNavigationCoordinator?.ResetWildlifeForNewRun();
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
                animalNeedsController,
                workerFeedingController,
                hedgehogForagingController,
                dailyOutcomeController);
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
                cellSize,
                roomViews);
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
            layoutEditorController.BindOakTreeGrowth(oakTreeLifecycleController.Model);
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
                layoutEditorController,
                mapRoot,
                cellSize,
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
                var parkBird = homes[index] == "central-park";
                var spawnPosition = GridToWorld(home) + (parkBird
                    ? new Vector3(index == 8 ? -0.17f : 0.17f, 0.30f, 0.86f)
                    : new Vector3(Mathf.Cos(angle) * radius, 0.30f, Mathf.Sin(angle) * radius));
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
                pigeonHomeRooms[pigeon] = homes[index];
                wildlifeHomeRooms[pigeon] = homes[index];
            }
            pigeonDemoAgent = pigeonAgents.Count > 0 ? pigeonAgents[0] : null;

        }

        private void BuildCitizenAnimationDemo()
        {
            var peopleRoot = NewObject("Citizen Population", generatedRoot);
            citizenPopulationPresenter = peopleRoot.AddComponent<CitizenPopulationPresenter>();
            citizenPopulationPresenter.Initialize(
                () => residentPopulationController?.Model?.Residents,
                roomId => roomViews.Find(view => view.Spec.Id == roomId)?.VisualRoot,
                surfaceMaterial,
                generatedHideFlags,
                residentPopulationController.Model,
                mapRoot);
            residentPopulationController.StateChanged += citizenPopulationPresenter.Synchronize;
        }

        private void BuildSquirrelAnimationDemo()
        {
            var demoRoot = NewObject("Squirrel Population", generatedRoot).transform;
            var cacheContainer = NewObject("Fixed Wildlife Locations", generatedRoot).transform;
            var homes = new[] { "oak-a", "oak-b", "oak-d", "shared-f" };
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
                    center + (homes[index] == "shared-f"
                        ? new Vector3(0.36f, 0.30f, -0.22f)
                        : direction + Vector3.up * 0.30f),
                    new Vector2(0.82f, 0.82f),
                    generatedHideFlags,
                    24900 + index);
                squirrel.InitializeCache(
                    cacheContainer,
                    center - direction + Vector3.up * 0.20f);
                squirrelAgents.Add(squirrel);
                wildlifeHomeRooms[squirrel] = homes[index];
            }
            squirrelDemoAgent = squirrelAgents.Count > 0 ? squirrelAgents[0] : null;
        }

        private void BuildHedgehogAnimationDemo()
        {
            var demoRoot = NewObject("Hedgehog Population", generatedRoot).transform;
            var homes = new[] { "shrub-a", "shared-h" };
            for (var index = 0; index < homes.Length; index++)
            {
                var home = FindRoomSpec(homes[index]);
                if (home == null)
                {
                    continue;
                }
                var center = GridToWorld(home);
                var spawnPosition = center + (homes[index] == "shared-h"
                    ? new Vector3(-0.28f, 0.30f, -0.20f)
                    : new Vector3(0.24f, 0.30f, -0.22f));
                var hedgehogObject = NewObject($"Hedgehog {index + 1:00}", demoRoot);
                var hedgehog = hedgehogObject.AddComponent<HedgehogDemoAgent>();
                hedgehog.Initialize(
                    surfaceMaterial,
                    spawnPosition,
                    new Vector2(0.58f, 0.52f),
                    generatedHideFlags,
                    34900 + index);
                hedgehogAgents.Add(hedgehog);
                wildlifeHomeRooms[hedgehog] = homes[index];
            }
            hedgehogDemoAgent = hedgehogAgents.Count > 0 ? hedgehogAgents[0] : null;
        }

        private void BuildFoxAnimationDemo()
        {
            var foxRefuge = FindRoomSpec("shared-j");
            if (foxRefuge == null)
            {
                return;
            }
            var root = NewObject("Fox Population", generatedRoot).transform;
            var center = GridToWorld(foxRefuge);
            for (var index = 0; index < AnimalPopulationDefaults.Foxes; index++)
            {
                var foxObject = NewObject($"Fox {index + 1:00}", root);
                var fox = foxObject.AddComponent<FoxDemoAgent>();
                fox.Initialize(
                    surfaceMaterial,
                    center + new Vector3(-0.32f, 0.30f, 0.02f),
                    new Vector2(0.86f, 0.78f),
                    generatedHideFlags,
                    44900 + index);
                foxAgents.Add(fox);
                wildlifeHomeRooms[fox] = "shared-j";
            }
            foxDemoAgent = foxAgents.Count > 0 ? foxAgents[0] : null;
        }

        private void BuildAnimalPopulation()
        {
            var populationObject = NewObject("Animal Population", generatedRoot);
            animalPopulationController = populationObject.AddComponent<AnimalPopulationController>();
            animalPopulationController.Initialize(pigeonAgents, squirrelAgents, hedgehogAgents, foxAgents);
            hud.BindAnimalPopulation(animalPopulationController);
            layoutEditorController.BindAnimalPopulation(animalPopulationController);

            var followObject = NewObject("Wildlife Follow Director", generatedRoot);
            var followDirector = followObject.AddComponent<PigeonDemoDirector>();
            followDirector.Initialize(
                boardCameraController,
                runtimeController,
                pigeonAgents,
                squirrelAgents,
                hedgehogAgents,
                foxAgents);
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
                cellSize,
                pigeonHomeRooms,
                roomId => roomViews.Find(view => view.Spec.Id == roomId)?.VisualRoot,
                runtimeController,
                wildlifeHomeRooms);
        }

        private void BuildPlayerFeeding()
        {
            var foodRoot = NewObject("Player Food Sources", generatedRoot).transform;
            var feedingObject = NewObject("Player Feeding", generatedRoot);
            playerFeedingController = feedingObject.AddComponent<PlayerFeedingController>();
            playerFeedingController.Initialize(
                runtimeController,
                resourceEconomyController,
                wasteManagementController,
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
            hud.BindAnimalMortality(animalMortalityController);
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
                foxAgents,
                wildlifeHomeRooms,
                animalNavigationCoordinator);
            animalNavigationCoordinator.BindNeeds(animalNeedsController);
            hud.BindAnimalNeeds(animalNeedsController);
        }

        private void BuildWorkerPasserbyFeeding()
        {
            var feedingObject = NewObject("Worker Passerby Feeding", generatedRoot);
            workerFeedingController = feedingObject.AddComponent<WorkerPasserbyFeedingController>();
            var animals = new List<IWildlifeLayoutAgent>();
            animals.AddRange(pigeonAgents);
            animals.AddRange(squirrelAgents);
            animals.AddRange(hedgehogAgents);
            animals.AddRange(foxAgents);
            workerFeedingController.Initialize(
                runtimeController,
                residentPopulationController,
                citizenPopulationPresenter,
                animalNeedsController,
                mapRoot,
                generatedHideFlags,
                animals);
        }

        private void BuildFoxPredation()
        {
            var predationObject = NewObject("Fox Predation", generatedRoot);
            foxPredationController = predationObject.AddComponent<FoxPredationController>();
            foxPredationController.Initialize(
                runtimeController,
                animalNeedsController,
                naturalFoodController,
                animalMortalityController,
                animalNavigationCoordinator,
                garageTrafficController,
                mapRoot,
                foxAgents,
                pigeonAgents,
                squirrelAgents,
                hedgehogAgents,
                hedgehogForagingController);
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
                hedgehogAgents,
                layoutEditorController);
            layoutEditorController.BindShrubShelter(hedgehogForagingController.Shelter);
            hud.BindHedgehogForaging(hedgehogForagingController);
        }

        private void BuildHedgehogSocial()
        {
            var socialObject = NewObject("Hedgehog Night Meeting", generatedRoot);
            hedgehogSocialController = socialObject.AddComponent<HedgehogSocialController>();
            hedgehogSocialController.Initialize(
                runtimeController,
                animalNeedsController,
                animalNavigationCoordinator,
                mapRoot,
                roomId => roomViews.Find(view => view.Spec.Id == roomId)?.VisualRoot,
                hedgehogAgents,
                wildlifeHomeRooms);
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

        private void BuildPigeonNaturalForaging()
        {
            var foragingObject = NewObject("Pigeon Flock Natural Foraging", generatedRoot);
            pigeonNaturalForagingController = foragingObject.AddComponent<PigeonNaturalForagingController>();
            pigeonNaturalForagingController.Initialize(
                runtimeController,
                naturalFoodController,
                animalNeedsController,
                animalNavigationCoordinator,
                mapRoot,
                pigeonHomeRooms);
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

        private void BuildDailyOutcome()
        {
            var outcomeObject = NewObject("Daily Outcome", generatedRoot);
            dailyOutcomeController = outcomeObject.AddComponent<DailyOutcomeController>();
            dailyOutcomeController.Initialize(runtimeController, layoutEditorController,
                resourceEconomyController, animalMortalityController,
                wasteManagementController, residentPopulationController,
                animalNeedsController);
            hud.BindDailyOutcome(dailyOutcomeController);
        }

        private void BuildResearchSession()
        {
            var researchObject = NewObject("Research Session", generatedRoot);
            researchSessionController = researchObject.AddComponent<ResearchSessionController>();
            researchSessionController.Initialize(
                runtimeController,
                layoutEditorController,
                animalMortalityController,
                ecologicalMetricsController,
                hedgehogForagingController,
                playerFeedingController);
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
                wasteManagementController,
                citizenPopulationPresenter,
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
            if (residentPopulationController != null && citizenPopulationPresenter != null)
            {
                residentPopulationController.StateChanged -= citizenPopulationPresenter.Synchronize;
            }
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
            pigeonNaturalForagingController = null;
            garageTrafficController = null;
            animalActivityController = null;
            squirrelTreeResponseController = null;
            foxPredationController = null;
            hedgehogForagingController = null;
            hedgehogSocialController = null;
            dailyOutcomeController = null;
            squirrelNaturalForagingController = null;
            ecologicalMetricsController = null;
            researchSessionController = null;
            onboardingController = null;
            mapRoot = null;
            citizenPopulationPresenter = null;
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
            if (tabletopMaterial != null)
            {
                DestroyGeneratedObject(tabletopMaterial);
                tabletopMaterial = null;
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
