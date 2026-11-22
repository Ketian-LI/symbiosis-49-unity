using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public sealed partial class ConstructionWorldView : MonoBehaviour
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
        private static readonly Vector3[] IdleOffsets =
        {
            new(0.25f, 0f, 0.15f), new(-0.22f, 0f, -0.14f),
            new(0.10f, 0f, -0.24f), new(-0.18f, 0f, 0.22f)
        };
        private static readonly ConstructionCategory[] HabitatVisitKinds =
        {
            ConstructionCategory.Green, ConstructionCategory.Square,
            ConstructionCategory.Waste, ConstructionCategory.Restaurant,
            ConstructionCategory.Supermarket
        };
        private sealed class IdleMotion
        {
            public Vector3 from;
            public Vector3 to;
            public float elapsed;
            public float duration;
            public float pauseRemaining;
            public int step;
        }
        private sealed class PendingForage
        {
            public IReadOnlyList<ConstructionTileData> route;
            public int index;
            public bool bird;
        }
        private readonly Dictionary<string, GameObject> rooms = new();
        private readonly Dictionary<string, ConstructionStreetVehicleVisual> streetVehicles = new();
        private readonly Dictionary<string, OakTreeStageVisual> growthVisuals = new();
        private readonly Dictionary<string, int> growthStages = new();
        private readonly Dictionary<string, GameObject[]> greenUpgradeFoliage = new();
        private Transform world;
        private GameObject boardBase;
        private Transform endlessGround;
        private Material endlessGroundMaterial;
        private Texture2D endlessGroundTexture;
        private bool endlessMode;
        private readonly List<(Renderer renderer, bool enabled)> hiddenOpeningBoard = new();
        private readonly List<Renderer> openingLidRenderers = new();
        private Camera boardCamera;
        private Material surface;
        private Material tabletopMaterial;
        private Transform tabletop;
        private Vector3 cameraOffset;
        private RectTransform boardAlignmentArea;
        private float boardCellSpacing;
        private int framedScreenWidth;
        private int framedScreenHeight;
        private SquirrelDemoVisual squirrel;
        private PigeonDemoVisual pigeon;
        private HedgehogDemoVisual hedgehog;
        private FoxDemoVisual fox;
        private SquirrelDemoVisual openingSquirrelVisual;
        internal const int MaxVisibleAnimalsPerSpecies = 8;
        private readonly List<SquirrelDemoVisual> squirrels = new();
        private readonly List<PigeonDemoVisual> pigeons = new();
        private readonly List<PigeonDemoVisual> visitorPigeons = new();
        private readonly List<HedgehogDemoVisual> hedgehogs = new();
        private readonly Dictionary<Transform, Vector3> animalHomes = new();
        private readonly Dictionary<Transform, string> animalHomeTiles = new();
        private readonly HashSet<Transform> travelingAnimals = new();
        private readonly HashSet<Transform> feedingAnimals = new();
        private readonly HashSet<Transform> restingAnimals = new();
        private readonly Dictionary<Transform, IdleMotion> idleMotions = new();
        private readonly Dictionary<Transform, Coroutine> foragingTrips = new();
        private readonly Dictionary<Transform, PendingForage> pendingForaging = new();
        private readonly Dictionary<Transform, Coroutine> habitatVisits = new();
        private readonly HashSet<Transform> visitingHabitat = new();
        private Transform travelingFox;
        private float foxContactUntil;
        private GameObject transientFoxPrey;
        private bool transientFoxPreyIsGhost;
        private Coroutine trafficCollision;
        private ConstructionStreetVehicleVisual collisionVehicle;
        private GameObject transientTrafficVictim;
        private GameObject trafficImpactMarker;
        private readonly Dictionary<string, CitizenDemoVisual> residents = new();
        private readonly Dictionary<CitizenDemoVisual, Vector3> commutingHomes = new();
        private readonly HashSet<CitizenDemoVisual> restingResidents = new();
        public bool HasActiveDayMotion => travelingAnimals.Count > 0 ||
            commutingHomes.Count > 0;
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
        private int viewportMinColumn;
        private int viewportMinRow;

        public bool IsReady => world != null && boardCamera != null;
        public Camera BoardCamera => boardCamera;
        public Transform BoardFoundation => boardBase == null ? null : boardBase.transform;
        public bool UsesOpeningBoard => usingOpeningBoard;
        public bool IsEntranceCameraMoving => entranceCameraMotion != null;

        public void Initialize(Camera camera, RectTransform boardArea,
            float cellSpacing, ConstructionGameMode mode = ConstructionGameMode.Fill)
        {
            if (world != null || camera == null || boardArea == null ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            surface = UrbanVisualFactory.CreateSurfaceMaterial();
            endlessMode = mode == ConstructionGameMode.Endless;
            world = new GameObject("Construction low-poly world").transform;
            world.SetParent(transform, false);
            world.localPosition = Vector3.zero;
            boardCamera = camera;
            boardAlignmentArea = boardArea;
            boardCellSpacing = cellSpacing;
            var cameraStartPosition = camera.transform.position;
            var cameraStartRotation = camera.transform.rotation;
            var cameraStartSize = camera.orthographicSize;
            var gameplaySize = CalculateCameraFraming();
            framedScreenWidth = Screen.width;
            framedScreenHeight = Screen.height;
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
            // The opening board shares Main's camera. Its menu tabletop is
            // placed beyond 180 m, so preserve the menu camera's clip range
            // for the return transition. Only a standalone scene owns its
            // camera clip distance.
            if (!usingOpeningBoard) camera.farClipPlane = 180f;
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
                ConstructionIllustratedVisualStyle.ApplyStarterOak(starter);
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
                if (!endlessMode) BuildBoardFoundation();
            }
            if (endlessMode)
            {
                HideFixedBoardSurface(opening?.CaseShell);
                BuildEndlessGround();
            }
            else BuildBoardCaseHardware();
        }

        private float CalculateCameraFraming()
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            boardAlignmentArea.GetWorldCorners(corners);
            var lower = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var upper = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            var screenCentre = (lower + upper) * 0.5f;
            var canvas = boardAlignmentArea.GetComponentInParent<Canvas>();
            var spacingPixels = boardCellSpacing * canvas.rootCanvas.scaleFactor;
            var gameplaySize = Pitch * Screen.height /
                (2f * Mathf.Max(1f, spacingPixels));
            cameraOffset = new Vector3(
                (0.5f - screenCentre.x / Screen.width) *
                    2f * gameplaySize * boardCamera.aspect,
                42f,
                (0.5f - screenCentre.y / Screen.height) *
                    2f * gameplaySize);
            return gameplaySize;
        }

        private void RefreshCameraFramingAfterResize()
        {
            // The opening transition owns the camera until it finishes. A
            // resize during that short animation is applied at its end.
            if (!IsReady || boardAlignmentArea == null ||
                entranceCameraMotion != null || Screen.width <= 0 ||
                Screen.height <= 0 ||
                (Screen.width == framedScreenWidth &&
                 Screen.height == framedScreenHeight))
                return;

            // CanvasScaler updates its pixel scale when the window changes;
            // the world camera must use that same new scale and board centre.
            boardCamera.orthographicSize = CalculateCameraFraming();
            framedScreenWidth = Screen.width;
            framedScreenHeight = Screen.height;
            SetViewport(viewportMinColumn, viewportMinRow);
        }

        private void LateUpdate()
        {
            RefreshCameraFramingAfterResize();
        }

        private void HideFixedBoardSurface(Transform caseShell)
        {
            if (boardBase == null) return;
            var westLid = caseShell == null ? null : caseShell.Find("West lid hinge");
            var eastLid = caseShell == null ? null : caseShell.Find("East lid hinge");
            foreach (var renderer in boardBase.GetComponentsInChildren<Renderer>(true))
            {
                // The illustrated halves still open as a physical box. Only
                // its finite 7x7 tray, slots and wall disappear in this mode.
                if (westLid != null && renderer.transform.IsChildOf(westLid) ||
                    eastLid != null && renderer.transform.IsChildOf(eastLid))
                {
                    openingLidRenderers.Add(renderer);
                    continue;
                }
                hiddenOpeningBoard.Add((renderer, renderer.enabled));
                renderer.enabled = false;
            }
        }

        private void BuildEndlessGround()
        {
            endlessGround = new GameObject("Unbounded moving plot surface").transform;
            endlessGround.SetParent(world, false);
            // Nine printed cells cover the seven interactive cells plus one
            // feathered row on each edge. The patch follows the camera; its
            // alpha fades into the tabletop instead of drawing a final rim.
            const int resolution = 512;
            const float cellsAcross = 9f;
            endlessGroundTexture = new Texture2D(resolution, resolution,
                TextureFormat.RGBA32, false)
            {
                name = "Endless plot print with feathered edge",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[resolution * resolution];
            for (var y = 0; y < resolution; y++)
            for (var x = 0; x < resolution; x++)
            {
                var gridX = (x + 0.5f) * cellsAcross / resolution;
                var gridY = (y + 0.5f) * cellsAcross / resolution;
                var distanceToLine = Mathf.Min(
                    Mathf.Min(Mathf.Repeat(gridX, 1f),
                        1f - Mathf.Repeat(gridX, 1f)),
                    Mathf.Min(Mathf.Repeat(gridY, 1f),
                        1f - Mathf.Repeat(gridY, 1f)));
                var ground = ((Mathf.FloorToInt(gridX) +
                    Mathf.FloorToInt(gridY)) & 1) == 0
                    ? UrbanPalette.ConstructionGround
                    : UrbanPalette.ConstructionGroundAlternate;
                var color = distanceToLine < 0.018f
                    ? UrbanPalette.ConstructionGrid : ground;
                var edgeDistance = Mathf.Min(gridX, cellsAcross - gridX,
                    gridY, cellsAcross - gridY);
                color.a = Mathf.SmoothStep(0f, 1f, edgeDistance);
                pixels[y * resolution + x] = color;
            }
            endlessGroundTexture.SetPixels32(pixels);
            endlessGroundTexture.Apply(false, true);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default");
            endlessGroundMaterial = new Material(shader)
            {
                mainTexture = endlessGroundTexture,
                renderQueue = (int)RenderQueue.Transparent
            };
            if (endlessGroundMaterial.HasProperty("_BaseMap"))
                endlessGroundMaterial.SetTexture("_BaseMap", endlessGroundTexture);
            if (endlessGroundMaterial.HasProperty("_Surface"))
            {
                endlessGroundMaterial.SetFloat("_Surface", 1f);
                endlessGroundMaterial.SetFloat("_Blend", 0f);
                endlessGroundMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                endlessGroundMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                endlessGroundMaterial.SetInt("_ZWrite", 0);
                endlessGroundMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Feathered continuous grid";
            plane.transform.SetParent(endlessGround, false);
            plane.transform.localPosition = new Vector3(0f, -0.03f, 0f);
            plane.transform.localScale = new Vector3(
                cellsAcross * Pitch / 10f, 1f, cellsAcross * Pitch / 10f);
            var renderer = plane.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = endlessGroundMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var collider = plane.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            SetLayer(endlessGround.gameObject, 29);
        }

        private IEnumerator MoveOpeningCamera(Vector3 fromPosition,
            Quaternion fromRotation, float fromSize, float toSize)
        {
            // TabletopBackdropAlignment keeps the photographed desk behind the
            // case and parallel to the camera in perspective. Interpolating
            // that far plane into its final floor position crosses the board.
            const float duration = 1.75f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                var eased = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / duration));
                boardCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, cameraOffset, eased),
                    Quaternion.Slerp(fromRotation, Quaternion.Euler(90f, 0f, 0f), eased));
                var visibleHalfHeight = Mathf.Lerp(fromSize, toSize, eased);
                boardCamera.orthographicSize = visibleHalfHeight;
                if (!boardCamera.orthographic)
                {
                    var distanceToGround = boardCamera.transform.position.y /
                        Mathf.Max(0.001f, -boardCamera.transform.forward.y);
                    boardCamera.fieldOfView = 2f * Mathf.Atan(
                        visibleHalfHeight / distanceToGround) * Mathf.Rad2Deg;
                }
                yield return null;
            }
            boardCamera.transform.SetPositionAndRotation(cameraOffset,
                Quaternion.Euler(90f, 0f, 0f));
            boardCamera.orthographicSize = toSize;
            boardCamera.orthographic = true;
            entranceCameraMotion = null;
            if (Screen.width != framedScreenWidth ||
                Screen.height != framedScreenHeight)
                RefreshCameraFramingAfterResize();
            else SetViewport(viewportMinColumn, viewportMinRow);
            if (endlessMode)
            {
                // The printed UI leaves now cover the opened physical lids.
                // Keep the 3D lids for the entrance, then hide them so panning
                // cannot reveal their navy backs above the fixed UI panels.
                foreach (var renderer in openingLidRenderers)
                {
                    if (renderer == null) continue;
                    hiddenOpeningBoard.Add((renderer, renderer.enabled));
                    renderer.enabled = false;
                }
                openingLidRenderers.Clear();
            }
        }

        public void RevealRestoredRooms()
        {
            // Endless saves can contain hundreds of rooms. Sync has already
            // placed them, so leave them visible instead of staggering every
            // restored room for an ever-growing entrance animation.
            if (endlessMode) return;
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
            viewportMinColumn = minColumn;
            viewportMinRow = minRow;
            if (!IsReady || boardCamera == null) return;
            // Refresh() runs in the same frame that Initialize starts the
            // entrance camera move. Do not teleport the camera (or tabletop)
            // to its final viewport before that move has rendered a frame.
            if (usingOpeningBoard && entranceCameraMotion != null) return;
            var centre = Cell(minColumn + 3, minRow + 3);
            boardCamera.transform.position = cameraOffset +
                new Vector3(centre.x, 0f, centre.z);
            if (endlessGround != null) endlessGround.localPosition = centre;
            if (boardBase != null && !endlessMode)
                boardBase.transform.localPosition = centre;
            if (tabletop != null)
            {
                var position = new Vector3(boardCamera.transform.position.x,
                    -0.58f, boardCamera.transform.position.z);
                var scale = new Vector3(
                    boardCamera.orthographicSize * boardCamera.aspect *
                    2f * 1.22f / 10f, 1f,
                    boardCamera.orthographicSize * 2f * 1.22f / 10f);
                var alignment = tabletop.GetComponent<TabletopBackdropAlignment>();
                if (alignment != null)
                    alignment.SetGameplayOverride(position, scale);
                else
                {
                    tabletop.position = position;
                    tabletop.localScale = scale;
                }
            }
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
            var paperBoard = Box("Paper Board", boardBase.transform,
                new Vector3(0f, -0.18f, 0f),
                new Vector3(mapSize + 1.25f, 0.28f, mapSize + 1.25f),
                UrbanPalette.ConstructionGrid);
            UrbanVisualFactory.SetShadowStrength(
                paperBoard.GetComponent<Renderer>(), 0f);
            // Thin ivory joints between near-flush green plots read as a
            // single printed game field, not 49 raised empty buttons.
            var slotSize = Pitch - 0.07f;
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
            {
                var slot = Box($"Empty plot {column + 1},{row + 1}", boardBase.transform,
                    Cell(column, row) + new Vector3(0f, -0.025f, 0f),
                    new Vector3(slotSize, 0.08f, slotSize),
                    (column + row) % 2 == 0
                        ? UrbanPalette.ConstructionGround :
                            UrbanPalette.ConstructionGroundAlternate);
                var face = Box("Pastel plot face", slot.transform,
                    new Vector3(0f, 0.54f, 0f),
                    new Vector3(1f, 0.20f, 1f),
                    (column + row) % 2 == 0
                        ? UrbanPalette.ConstructionGround :
                            UrbanPalette.ConstructionGroundAlternate);
                UrbanVisualFactory.SetInkStrength(face.GetComponent<Renderer>(), 0f);
                UrbanVisualFactory.SetShadowStrength(slot.GetComponent<Renderer>(), 0f);
                UrbanVisualFactory.SetShadowStrength(face.GetComponent<Renderer>(), 0f);
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
            ConstructionBoardFinishVisual.Build(boardBase.transform, mapSize, surface);
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
                SyncGreenUpgradeFoliage(run, tile);
            }
            foreach (var tile in run.BuiltTiles)
                if (rooms.TryGetValue(tile.id, out var room))
                    ConstructionIllustratedVisualStyle.ApplyRoom(room, tile, run);
            SyncStreetVehicles(run);
            SyncActors(run);
            SyncIllustratedActors();
        }

        public void ClearBuiltWorld()
        {
            // A new construction run keeps this view object alive. Remove the
            // previous run's meshes before Sync creates its starter oak again.
            StopTrafficCollision();
            StopForaging(true);
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
            animalHomeTiles.Clear();
            idleMotions.Clear();
            feedingAnimals.Clear();
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
            streetVehicles.Clear();
            growthVisuals.Clear();
            growthStages.Clear();
            greenUpgradeFoliage.Clear();
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
            foreach (var visitor in visitorPigeons)
            {
                if (visitor == null) continue;
                visitor.gameObject.SetActive(false);
                Destroy(visitor.gameObject);
            }
            visitorPigeons.Clear();
            foreach (var visitor in hedgehogs)
            {
                if (visitor == null) continue;
                visitor.gameObject.SetActive(false);
                Destroy(visitor.gameObject);
            }
            hedgehogs.Clear();
            hedgehog = null;
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
            IReadOnlyList<IReadOnlyList<ConstructionTileData>> pigeonRoutes,
            ConstructionRunModel run = null,
            IReadOnlyList<ConstructionTileData> hedgehogRoute = null,
            IReadOnlyList<IReadOnlyList<ConstructionTileData>>
                hedgehogRoutes = null)
        {
            if (!IsReady) return;
            StopHabitatVisits(true);
            // Let a journey already on the board finish on its valid route.
            // A new day's route is queued for that animal after it returns.
            pendingForaging.Clear();
            if (foxHunt != null) StopCoroutine(foxHunt);
            HideTransientFoxPrey();
            foxHunt = null;
            travelingFox = null;
            foxContactUntil = 0f;
            StartForagingTrips(squirrels, squirrelRoutes, false, run,
                matchHomeTile: run?.ScoringVersion >= 7);
            StartForagingTrips(pigeons, pigeonRoutes, true, run);
            if (hedgehogRoutes != null)
                StartForagingTrips(hedgehogs, hedgehogRoutes, false, run,
                    matchHomeTile: true);
            else if (hedgehogRoute != null && hedgehog != null)
                StartForagingTrips(new[] { hedgehog },
                    new IReadOnlyList<ConstructionTileData>[] { hedgehogRoute },
                    false, run);
        }

        public void PlayHabitatVisits(ConstructionRunModel run)
        {
            if (!IsReady || run == null) return;
            // A small number of visible visits keeps a busy board readable.
            // Meal journeys take precedence; visiting alone never feeds or scores.
            QueueHabitatVisit(FirstActive(squirrels), run, 0, false);
            QueueHabitatVisit(FirstActive(pigeons), run, 1, true);
            QueueHabitatVisit(FirstActive(hedgehogs), run, 2, false);
            if (fox != null && fox.gameObject.activeInHierarchy)
                QueueHabitatVisit(fox.transform, run, 3, false);
        }

        private static Transform FirstActive<T>(IReadOnlyList<T> actors)
            where T : MonoBehaviour
        {
            foreach (var actor in actors)
                if (actor != null && actor.gameObject.activeInHierarchy)
                    return actor.transform;
            return null;
        }

        private void QueueHabitatVisit(Transform actor,
            ConstructionRunModel run, int speciesIndex, bool bird)
        {
            if (actor == null ||
                actor.gameObject == transientFoxPrey ||
                actor.gameObject == transientTrafficVictim ||
                !animalHomeTiles.TryGetValue(actor, out var homeId)) return;
            for (var kindOffset = 0; kindOffset < HabitatVisitKinds.Length;
                 kindOffset++)
            {
                var kind = HabitatVisitKinds[(run.CurrentDay + speciesIndex +
                    kindOffset) % HabitatVisitKinds.Length];
                IReadOnlyList<ConstructionTileData> bestRoute = null;
                Vector3? bestDoorway = null;
                foreach (var target in run.BuiltTiles)
                {
                    if (target.category != kind) continue;
                    var doorstepVisit = kind is
                        ConstructionCategory.Restaurant or
                        ConstructionCategory.Supermarket;
                    var destinations = doorstepVisit
                        ? run.BuiltTiles.Where(tile =>
                            ConstructionBoardModel.AnimalCanTraverse(
                                tile.category) &&
                            ConstructionBoardModel.AreCardinalNeighbours(
                                tile.column, tile.row,
                                target.column, target.row))
                        : new[] { target };
                    foreach (var destination in destinations)
                    {
                        var route = run.GreenFoodRouteTo(homeId,
                            destination.id);
                        if (route.Count == 0 || route.Count > 6 ||
                            bestRoute != null && route.Count >= bestRoute.Count)
                            continue;
                        bestRoute = route;
                        bestDoorway = doorstepVisit
                            ? Cell(destination.column, destination.row) +
                              new Vector3((target.column -
                                  destination.column) * Pitch * 0.35f,
                                  0.23f, (target.row -
                                  destination.row) * Pitch * 0.35f)
                            : null;
                    }
                }
                if (bestRoute == null) continue;
                habitatVisits[actor] = StartCoroutine(HabitatVisitWhenFree(
                    actor, bestRoute, speciesIndex, bird, bestDoorway));
                return;
            }
        }

        private IEnumerator HabitatVisitWhenFree(Transform actor,
            IReadOnlyList<ConstructionTileData> route, int index,
            bool bird, Vector3? doorway)
        {
            while (actor != null && actor.gameObject.activeInHierarchy &&
                   (travelingAnimals.Contains(actor) || actor == travelingFox))
                yield return null;
            if (actor == null || !actor.gameObject.activeInHierarchy)
            {
                habitatVisits.Remove(actor);
                yield break;
            }
            idleMotions.Remove(actor);
            visitingHabitat.Add(actor);
            travelingAnimals.Add(actor);
            yield return TravelAnimal(actor, route, bird ? 1.20f : 1.05f,
                index, bird, false, doorway);
            travelingAnimals.Remove(actor);
            visitingHabitat.Remove(actor);
            habitatVisits.Remove(actor);
            idleMotions.Remove(actor);
        }

        private void StopHabitatVisits(bool restoreHomes)
        {
            foreach (var trip in habitatVisits.Values)
                if (trip != null) StopCoroutine(trip);
            habitatVisits.Clear();
            foreach (var actor in visitingHabitat)
            {
                travelingAnimals.Remove(actor);
                idleMotions.Remove(actor);
                if (restoreHomes && actor != null &&
                    animalHomes.TryGetValue(actor, out var home))
                    actor.position = home;
            }
            visitingHabitat.Clear();
            restingAnimals.Clear();
        }

        public void PlayPigeonArrival()
        {
            if (!IsReady || pigeon == null || !pigeon.gameObject.activeSelf) return;
            if (pigeonArrival != null) StopCoroutine(pigeonArrival);
            idleMotions.Remove(pigeon.transform);
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

        // The model has already settled this casualty before this is called.
        // A separate, short-lived actor shows the crossing without reviving a
        // removed population member or changing the authoritative run state.
        public void PlayCollision(string streetTileId,
            ConstructionTrafficSpecies species, string wildlifeTileId)
        {
            if (!IsReady || species == ConstructionTrafficSpecies.None ||
                string.IsNullOrEmpty(streetTileId) ||
                !streetVehicles.TryGetValue(streetTileId, out var vehicle) ||
                vehicle == null ||
                !rooms.TryGetValue(streetTileId, out var street) ||
                street == null) return;
            StopTrafficCollision();
            var timeToContact = vehicle.QueueCollision();
            if (timeToContact < 0f) return;
            collisionVehicle = vehicle;
            var wildlifePosition = rooms.TryGetValue(wildlifeTileId ??
                string.Empty, out var habitat) && habitat != null
                    ? habitat.transform.position : street.transform.position;
            trafficCollision = StartCoroutine(AnimateTrafficCollision(
                vehicle, species, wildlifePosition, street.transform.position,
                timeToContact));
        }

        private IEnumerator AnimateTrafficCollision(
            ConstructionStreetVehicleVisual vehicle,
            ConstructionTrafficSpecies species, Vector3 wildlifePosition,
            Vector3 streetPosition, float timeToContact)
        {
            var direction = streetPosition - wildlifePosition;
            direction.y = 0f;
            var adjacent = direction.sqrMagnitude > 0.01f &&
                direction.magnitude <= Pitch * 1.5f;
            if (adjacent)
            {
                direction.Normalize();
                var start = wildlifePosition + direction * 0.65f +
                    Vector3.up * 0.23f;
                var contact = streetPosition - direction * 0.25f +
                    Vector3.up * 0.23f;
                transientTrafficVictim = CreateTrafficVictim(species, start);
                var elapsed = 0f;
                var duration = Mathf.Max(0.2f, timeToContact);
                while (vehicle != null && !vehicle.CollisionContact &&
                       elapsed < duration + 1f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    if (transientTrafficVictim != null)
                        transientTrafficVictim.transform.position =
                            Vector3.Lerp(start, contact,
                                Mathf.SmoothStep(0f, 1f,
                                    Mathf.Clamp01(elapsed / duration)));
                    yield return null;
                }
            }
            else
                while (vehicle != null && !vehicle.CollisionContact)
                    yield return null;
            if (vehicle != null && vehicle.CollisionContact)
            {
                if (transientTrafficVictim != null)
                {
                    transientTrafficVictim.SetActive(false);
                    Destroy(transientTrafficVictim);
                    transientTrafficVictim = null;
                }
                trafficImpactMarker = CreateTrafficImpactMarker(streetPosition);
                while (vehicle != null && vehicle.CollisionContact)
                {
                    if (trafficImpactMarker != null)
                        trafficImpactMarker.transform.localScale =
                            Vector3.one * (1f + 0.12f *
                                Mathf.Sin(Time.unscaledTime * 18f));
                    yield return null;
                }
            }
            trafficCollision = null;
            StopTrafficCollision();
        }

        private GameObject CreateTrafficVictim(
            ConstructionTrafficSpecies species, Vector3 position)
        {
            var actor = NewActor("Traffic crossing silhouette", world,
                world.InverseTransformPoint(position));
            switch (species)
            {
                case ConstructionTrafficSpecies.Squirrel:
                    actor.AddComponent<SquirrelDemoVisual>()
                        .Initialize(surface, HideFlags.None);
                    break;
                case ConstructionTrafficSpecies.Pigeon:
                    actor.AddComponent<PigeonDemoVisual>()
                        .Initialize(surface, HideFlags.None);
                    break;
                case ConstructionTrafficSpecies.Hedgehog:
                    actor.AddComponent<HedgehogDemoVisual>()
                        .Initialize(surface, HideFlags.None);
                    break;
                case ConstructionTrafficSpecies.Fox:
                    actor.AddComponent<FoxDemoVisual>()
                        .Initialize(surface, HideFlags.None);
                    break;
                default:
                    Destroy(actor);
                    return null;
            }
            SetLayer(actor, 29);
            ConstructionIllustratedVisualStyle.AttachActor(actor, species switch
            {
                ConstructionTrafficSpecies.Squirrel => ConstructionIllustratedVisualStyle.ActorKind.Squirrel,
                ConstructionTrafficSpecies.Pigeon => ConstructionIllustratedVisualStyle.ActorKind.Pigeon,
                ConstructionTrafficSpecies.Hedgehog => ConstructionIllustratedVisualStyle.ActorKind.Hedgehog,
                _ => ConstructionIllustratedVisualStyle.ActorKind.Fox
            });
            return actor;
        }

        private GameObject CreateTrafficImpactMarker(Vector3 position)
        {
            var marker = NewActor("Traffic impact glint", world,
                world.InverseTransformPoint(position + Vector3.up * 0.72f));
            var colour = new Color(0.94f, 0.65f, 0.35f);
            var first = Box("Impact glint A", marker.transform,
                Vector3.zero, new Vector3(0.57f, 0.035f, 0.10f), colour);
            first.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            var second = Box("Impact glint B", marker.transform,
                Vector3.zero, new Vector3(0.57f, 0.035f, 0.10f), colour);
            second.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);
            SetLayer(marker, 29);
            return marker;
        }

        private void StopTrafficCollision()
        {
            if (trafficCollision != null) StopCoroutine(trafficCollision);
            trafficCollision = null;
            if (collisionVehicle != null) collisionVehicle.CancelCollision();
            collisionVehicle = null;
            if (transientTrafficVictim != null)
            {
                transientTrafficVictim.SetActive(false);
                Destroy(transientTrafficVictim);
                transientTrafficVictim = null;
            }
            if (trafficImpactMarker != null)
            {
                Destroy(trafficImpactMarker);
                trafficImpactMarker = null;
            }
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
                ConstructionIllustratedVisualStyle.AttachActor(actor, species switch
                {
                    ConstructionPreySpecies.Squirrel => ConstructionIllustratedVisualStyle.ActorKind.Squirrel,
                    ConstructionPreySpecies.Pigeon => ConstructionIllustratedVisualStyle.ActorKind.Pigeon,
                    _ => ConstructionIllustratedVisualStyle.ActorKind.Hedgehog
                });
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
            idleMotions.Remove(travelingFox);
            var home = animalHomes[travelingFox];
            var centreOffset = new Vector3(0f, 0.23f, 0f);
            var routeOffset = new Vector3(0.25f, 0.23f, -0.18f);
            var preyPosition = transientFoxPrey == null
                ? Cell(route[route.Count - 1].column,
                    route[route.Count - 1].row) +
                  new Vector3(0.55f, 0.23f, -0.5f)
                : transientFoxPrey.transform.position;
            for (var index = 1; index < route.Count; index++)
            {
                if (IsDiagonalStep(route[index - 1], route[index]))
                {
                    // Enter and leave a corner from the cell centres. A
                    // shifted diagonal would cross one of the side rooms.
                    yield return MoveBetween(travelingFox, travelingFox.position,
                        Cell(route[index - 1].column,
                            route[index - 1].row) + centreOffset,
                        0.14f, minimumDuration: 0.14f);
                    yield return MoveBetween(travelingFox, travelingFox.position,
                        Cell(route[index].column, route[index].row) +
                        centreOffset, 0.32f);
                    if (index == route.Count - 1)
                        yield return MoveBetween(travelingFox,
                            travelingFox.position, preyPosition, 0.14f,
                            minimumDuration: 0.14f);
                }
                else
                    yield return MoveBetween(travelingFox, travelingFox.position,
                        index == route.Count - 1 ? preyPosition :
                            Cell(route[index].column, route[index].row) +
                            routeOffset, 0.32f);
            }
            HideTransientFoxPrey();
            foxContactUntil = Time.unscaledTime + 0.2f;
            yield return new WaitForSecondsRealtime(0.2f);
            for (var index = route.Count - 2; index >= 0; index--)
            {
                if (IsDiagonalStep(route[index + 1], route[index]))
                {
                    yield return MoveBetween(travelingFox, travelingFox.position,
                        Cell(route[index + 1].column,
                            route[index + 1].row) + centreOffset,
                        0.14f, minimumDuration: 0.14f);
                    yield return MoveBetween(travelingFox, travelingFox.position,
                        Cell(route[index].column, route[index].row) +
                        centreOffset, 0.32f);
                    if (index == 0)
                        yield return MoveBetween(travelingFox,
                            travelingFox.position, home, 0.14f,
                            minimumDuration: 0.14f);
                }
                else
                    yield return MoveBetween(travelingFox, travelingFox.position,
                        index == 0 ? home : Cell(route[index].column,
                            route[index].row) + routeOffset, 0.32f);
            }
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
                if (journey == null ||
                    !residents.TryGetValue(journey.homeTileId ??
                        (journey.routeTileIds != null &&
                         journey.routeTileIds.Count > 0
                            ? journey.routeTileIds[0] : string.Empty),
                        out var resident) ||
                    resident == null || !resident.gameObject.activeSelf)
                    continue;
                var route = new List<ConstructionTileData>();
                if (journey.routeTileIds != null)
                    foreach (var id in journey.routeTileIds)
                        if (byId.TryGetValue(id, out var tile))
                            route.Add(tile);
                var commute = (journey.worked || journey.ate) &&
                    route.Count >= 2 &&
                    route.Count == journey.routeTileIds.Count;
                byId.TryGetValue(journey.homeTileId ?? string.Empty,
                    out var homeTile);
                byId.TryGetValue(journey.greenVisitTileId ?? string.Empty,
                    out var greenTile);
                if (!commute && (homeTile == null || greenTile == null))
                    continue;
                commutingHomes[resident] = resident.transform.localPosition;
                commuteTrips.Add(StartCoroutine(Commute(resident,
                    commute ? route : null, homeTile, greenTile)));
            }
        }

        // Use the settled day, not an idle animation pose, to identify who
        // missed an activity. Fox hunting is optional and is not a daily meal.
        public IReadOnlyList<Transform> IdleCueActorsForDay(
            ConstructionEcologyDayResult ecology,
            ConstructionHumanDayResult human)
        {
            var idle = new List<Transform>();
            if (!IsReady || ecology == null) return idle;

            var fedSquirrelHomes = new Dictionary<string, int>();
            if (ecology.squirrelMealHomeTileIds != null)
                foreach (var homeId in ecology.squirrelMealHomeTileIds)
                {
                    if (string.IsNullOrEmpty(homeId)) continue;
                    fedSquirrelHomes.TryGetValue(homeId, out var count);
                    fedSquirrelHomes[homeId] = count + 1;
                }
            for (var index = 0;
                 index < squirrels.Count && index < ecology.squirrelsPresent;
                 index++)
            {
                var visual = squirrels[index];
                if (visual == null || !visual.gameObject.activeInHierarchy ||
                    visual.gameObject == transientFoxPrey) continue;
                if (animalHomeTiles.TryGetValue(visual.transform,
                        out var homeId) &&
                    fedSquirrelHomes.TryGetValue(homeId, out var meals) &&
                    meals > 0)
                {
                    fedSquirrelHomes[homeId] = meals - 1;
                    continue;
                }
                idle.Add(visual.transform);
            }

            var hungryPigeons = Mathf.Max(0,
                ecology.pigeonsPresent - ecology.pigeonsFed);
            for (var index = Mathf.Min(pigeons.Count,
                     ecology.pigeonsPresent) - 1;
                 index >= 0 && hungryPigeons > 0; index--)
            {
                var visual = pigeons[index];
                if (visual == null || !visual.gameObject.activeInHierarchy ||
                    visual.gameObject == transientFoxPrey) continue;
                idle.Add(visual.transform);
                hungryPigeons--;
            }
            foreach (var visitor in hedgehogs)
                if (visitor != null && visitor.gameObject.activeInHierarchy &&
                    animalHomeTiles.TryGetValue(visitor.transform,
                        out var homeId) &&
                    ecology.hedgehogHomeTileIds.Contains(homeId) &&
                    !ecology.hedgehogMealHomeTileIds.Contains(homeId))
                    idle.Add(visitor.transform);

            if (human?.residents != null)
                foreach (var person in human.residents)
                    if (person != null && !person.completedCycle &&
                        !string.IsNullOrEmpty(person.homeTileId) &&
                        residents.TryGetValue(person.homeTileId,
                            out var resident) && resident != null &&
                        resident.gameObject.activeInHierarchy)
                        idle.Add(resident.transform);
            return idle;
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
                new Vector3(4.08f, 0.24f, 4.08f),
                UrbanPalette.ForConstructionRoom(type));
            ConstructionPlotFrameVisual.BuildFrame(root.transform, surface);
            if (tile.category == ConstructionCategory.Green)
                AddGreen(root.transform, tile);
            else if (tile.category == ConstructionCategory.Street)
                AddStreet(root.transform, tile);
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

        private void AddStreet(Transform parent, ConstructionTileData tile)
        {
            var stone = Color.Lerp(UrbanPalette.Board, Color.white, 0.12f);
            // Three flush pieces form one cross without two overlapping top
            // faces. Suppress the sketch shader's box-edge ink on the paving;
            // its UV outlines read as stray black stripes across the road.
            const float halfRoadWidth = 0.62f;
            const float halfRoadLength = 1.915f;
            var branchLength = halfRoadLength - halfRoadWidth;
            var branchCentre = (halfRoadLength + halfRoadWidth) * 0.5f;
            foreach (var side in new[] { -1f, 1f })
            {
                var branch = Box("Street north-south paving", parent,
                    new Vector3(0f, 0.26f, side * branchCentre),
                    new Vector3(1.24f, 0.022f, branchLength), stone);
                UrbanVisualFactory.SetInkStrength(branch.GetComponent<Renderer>(), 0f);
            }
            var cross = Box("Street east-west paving", parent,
                new Vector3(0f, 0.26f, 0f),
                new Vector3(3.83f, 0.022f, 1.24f), stone);
            UrbanVisualFactory.SetInkStrength(cross.GetComponent<Renderer>(), 0f);
            var vehicle = new GameObject("Passing street car");
            vehicle.transform.SetParent(parent, false);
            vehicle.transform.localScale = Vector3.one * 1.2f;
            var paint = (tile.buildIndex % 3) switch
            {
                0 => new Color(0.75f, 0.29f, 0.20f),
                1 => new Color(0.25f, 0.46f, 0.61f),
                _ => new Color(0.80f, 0.63f, 0.29f)
            };
            Box("Car body", vehicle.transform, new Vector3(0f, 0.07f, 0f),
                new Vector3(0.72f, 0.27f, 1.10f), paint);
            Box("Car cabin", vehicle.transform, new Vector3(0f, 0.27f, -0.05f),
                new Vector3(0.57f, 0.21f, 0.55f), paint);
            var glass = new Color(0.28f, 0.38f, 0.43f);
            Box("Front windscreen", vehicle.transform,
                new Vector3(0f, 0.28f, 0.238f),
                new Vector3(0.48f, 0.12f, 0.045f), glass);
            Box("Rear window", vehicle.transform,
                new Vector3(0f, 0.28f, -0.34f),
                new Vector3(0.46f, 0.11f, 0.045f), glass);
            for (var side = -1; side <= 1; side += 2)
            {
                for (var axle = -1; axle <= 1; axle += 2)
                {
                    var wheel = Box($"Wheel {side} {axle}", vehicle.transform,
                        new Vector3(side * 0.36f, -0.035f, axle * 0.34f),
                        new Vector3(0.075f, 0.12f, 0.15f),
                        new Color(0.25f, 0.27f, 0.29f));
                    UrbanVisualFactory.SetInkStrength(
                        wheel.GetComponent<Renderer>(), 0f);
                }
                Box($"Headlight {side}", vehicle.transform,
                    new Vector3(side * 0.25f, 0.09f, 0.563f),
                    new Vector3(0.11f, 0.07f, 0.025f),
                    new Color(0.97f, 0.89f, 0.67f));
            }
            var brakeLamps = new GameObject("Brake lamps");
            brakeLamps.transform.SetParent(vehicle.transform, false);
            for (var side = -1; side <= 1; side += 2)
            {
                var lamp = Box($"Brake lamp {side}", brakeLamps.transform,
                    new Vector3(side * 0.25f, 0.09f, -0.563f),
                    new Vector3(0.12f, 0.07f, 0.035f),
                    new Color(0.93f, 0.22f, 0.15f));
                UrbanVisualFactory.SetInkStrength(lamp.GetComponent<Renderer>(), 0f);
            }
            var streetVehicle = vehicle.AddComponent<ConstructionStreetVehicleVisual>();
            streetVehicle.SetBrakeLamps(brakeLamps);
            streetVehicles[tile.id] = streetVehicle;
            SetLayer(vehicle, 29);
            ConstructionIllustratedVisualStyle.AttachActor(vehicle,
                ConstructionIllustratedVisualStyle.ActorKind.Car);
        }

        private void SyncStreetVehicles(ConstructionRunModel run)
        {
            var builtTiles = run.BuiltTiles;
            var byCoordinate = new Dictionary<(int column, int row),
                ConstructionTileData>(builtTiles.Count);
            foreach (var tile in builtTiles)
                byCoordinate[(tile.column, tile.row)] = tile;
            int ConnectionWeight(int column, int row) =>
                byCoordinate.TryGetValue((column, row), out var neighbour)
                    ? neighbour.category switch
                    {
                        ConstructionCategory.Street => 2,
                        ConstructionCategory.Residence or ConstructionCategory.Restaurant or
                            ConstructionCategory.Workshop or ConstructionCategory.Supermarket => 1,
                        _ => 0
                    }
                    : 0;
            foreach (var tile in builtTiles)
            {
                if (tile.category != ConstructionCategory.Street ||
                    !streetVehicles.TryGetValue(tile.id, out var vehicle) ||
                    vehicle == null) continue;
                var eastWest = ConnectionWeight(tile.column - 1, tile.row) +
                    ConnectionWeight(tile.column + 1, tile.row);
                var northSouth = ConnectionWeight(tile.column, tile.row - 1) +
                    ConnectionWeight(tile.column, tile.row + 1);
                // Cars follow the same edge-connected street network as
                // residents. A corner contact cannot create a road approach.
                vehicle.Configure(eastWest > 0, northSouth > 0,
                    Mathf.Repeat(tile.buildIndex * 0.37f, 1f));
                vehicle.SetRoadsideWildlife(run.StreetAdjacentWildlifeCount(tile.id));
            }
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

        private void SyncGreenUpgradeFoliage(ConstructionRunModel run,
            ConstructionTileData tile)
        {
            if (!run.UsesGreenUpgrades ||
                !rooms.TryGetValue(tile.id, out var room) || room == null)
                return;
            if (!greenUpgradeFoliage.TryGetValue(tile.id, out var foliage))
            {
                var levelTwo = new GameObject("Green habitat level 2");
                levelTwo.transform.SetParent(room.transform, false);
                var levelThree = new GameObject("Green habitat level 3");
                levelThree.transform.SetParent(room.transform, false);
                BuildGreenUpgradeFoliage(tile.planting, levelTwo.transform,
                    levelThree.transform);
                SetLayer(levelTwo, 29);
                SetLayer(levelThree, 29);
                foliage = new[] { levelTwo, levelThree };
                greenUpgradeFoliage[tile.id] = foliage;
            }
            var level = run.GreenUpgradeLevel(tile.id);
            foliage[0].SetActive(level >= 2);
            foliage[1].SetActive(level >= 3);
        }

        private void BuildGreenUpgradeFoliage(GreenPlanting planting,
            Transform levelTwo, Transform levelThree)
        {
            // Keep the middle and four door landings clear. The added growth
            // follows each planting's silhouette instead of marking a level
            // with a badge on the room.
            var lightLeaf = UrbanPalette.ShrubLeafHighlight;
            var deepLeaf = UrbanPalette.ShrubDeepLeaf;
            var cream = new Color(0.91f, 0.84f, 0.70f);
            var ochre = new Color(0.83f, 0.57f, 0.22f);
            switch (planting)
            {
                case GreenPlanting.Oak:
                    UpgradeFoliage(levelTwo, "Oak understory", -1.26f, -1.22f,
                        0.63f, lightLeaf);
                    UpgradeFoliage(levelTwo, "Oak understory", 1.23f, 1.21f,
                        0.61f, deepLeaf);
                    UpgradeFoliage(levelTwo, "Oak understory", 1.25f, -1.18f,
                        0.49f, lightLeaf);
                    UpgradeFoliage(levelThree, "Oak understory", -1.23f, 1.17f,
                        0.60f, deepLeaf);
                    UpgradeFoliage(levelThree, "Oak understory", 1.08f, -0.92f,
                        0.45f, lightLeaf);
                    UpgradeFoliage(levelThree, "Oak acorns", -1.27f, -1.20f,
                        0.28f, ochre, 0.80f);
                    UpgradeFoliage(levelThree, "Oak acorns", 1.24f, 1.18f,
                        0.25f, ochre, 0.80f);
                    break;
                case GreenPlanting.Shrub:
                    UpgradeFoliage(levelTwo, "Shrub thicket", -1.20f, -1.18f,
                        0.75f, deepLeaf);
                    UpgradeFoliage(levelTwo, "Shrub thicket", 1.17f, 1.17f,
                        0.72f, lightLeaf);
                    UpgradeFoliage(levelTwo, "Shrub thicket", -1.15f, 1.15f,
                        0.59f, deepLeaf);
                    UpgradeFoliage(levelThree, "Shrub thicket", 1.15f, -1.17f,
                        0.69f, lightLeaf);
                    UpgradeFoliage(levelThree, "Shrub thicket", 1.05f, 0.92f,
                        0.52f, deepLeaf);
                    UpgradeFoliage(levelThree, "Shrub berries", -1.18f, -1.17f,
                        0.27f, ochre, 0.80f);
                    UpgradeFoliage(levelThree, "Shrub berries", 1.18f, 1.16f,
                        0.27f, ochre, 0.80f);
                    UpgradeFoliage(levelThree, "Shrub berries", 1.14f, -1.16f,
                        0.24f, ochre, 0.78f);
                    break;
                default:
                    UpgradeFoliage(levelTwo, "Meadow grasses", -1.22f, -1.20f,
                        0.52f, deepLeaf);
                    UpgradeFoliage(levelTwo, "Meadow grasses", 1.19f, 1.20f,
                        0.52f, lightLeaf);
                    UpgradeFoliage(levelTwo, "Meadow grasses", -1.18f, 1.16f,
                        0.44f, lightLeaf);
                    UpgradeFoliage(levelTwo, "Meadow flowers", -1.21f, -1.18f,
                        0.30f, cream, 0.69f);
                    UpgradeFoliage(levelTwo, "Meadow flowers", 1.18f, 1.19f,
                        0.30f, cream, 0.69f);
                    UpgradeFoliage(levelThree, "Meadow grasses", 1.17f, -1.19f,
                        0.52f, deepLeaf);
                    UpgradeFoliage(levelThree, "Meadow grasses", -0.94f, 1.22f,
                        0.41f, lightLeaf);
                    UpgradeFoliage(levelThree, "Meadow flowers", -1.17f, 1.16f,
                        0.32f, ochre, 0.69f);
                    UpgradeFoliage(levelThree, "Meadow flowers", 1.15f, -1.18f,
                        0.32f, ochre, 0.69f);
                    UpgradeFoliage(levelThree, "Meadow flowers", -1.03f, -0.98f,
                        0.28f, cream, 0.69f);
                    break;
            }
        }

        private void UpgradeFoliage(Transform parent, string name, float x,
            float z, float size, Color color, float height = 0.42f) =>
            LowPolyTreeVisualBuilder.BuildFacetedFoliage(name, parent,
                new Vector3(x, height, z), Vector3.one * size, 17f, color,
                surface, HideFlags.None);

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
            var squirrelHomes = run.VisibleSquirrelHomeTiles(
                run.UsesFillHand ? int.MaxValue : MaxVisibleAnimalsPerSpecies);
            var squirrelCount = squirrelHomes.Count;
            while (squirrels.Count < squirrelCount)
            {
                var actor = NewActor($"Squirrel {squirrels.Count + 1}", world,
                    Vector3.zero);
                var visual = actor.AddComponent<SquirrelDemoVisual>();
                visual.Initialize(surface, HideFlags.None);
                SetLayer(actor, 29);
                squirrels.Add(visual);
            }
            for (var index = 0; index < squirrels.Count; index++)
            {
                var visual = squirrels[index];
                if (visual == null) continue;
                var active = index < squirrelCount ||
                    visual.gameObject == transientFoxPrey;
                var wasActive = visual.gameObject.activeSelf;
                visual.gameObject.SetActive(active);
                if (!active)
                {
                    idleMotions.Remove(visual.transform);
                    continue;
                }
                visual.SetVisible(true);
                if (visual.gameObject == transientFoxPrey) continue;
                var tile = squirrelHomes[index];
                var home = Cell(tile.column, tile.row) +
                    AnimalOffset(index, false);
                SetAnimalHome(visual.transform, home, tile.id,
                    travelingAnimals.Contains(visual.transform), !wasActive);
            }
            var pigeonTile = run.BuiltTiles == null ? null :
                System.Linq.Enumerable.FirstOrDefault(run.BuiltTiles,
                    tile => tile.id == run.PigeonTileId);
            var pigeonCount = pigeonTile == null ? 0 : run.UsesFillHand
                ? run.PigeonPopulation
                : Mathf.Min(run.PigeonPopulation, MaxVisibleAnimalsPerSpecies);
            // The hand game has no animal-count badge. Show every pigeon and
            // let the flock occupy its connected mature meadows instead of
            // stacking more than eight models on the founding meadow.
            var pigeonHomes = new List<ConstructionTileData>();
            if (run.UsesGreenPatches)
                pigeonHomes.AddRange(run.VisiblePigeonHomeTiles(pigeonCount));
            else if (pigeonTile != null)
            {
                pigeonHomes.Add(pigeonTile);
                if (run.UsesFillHand)
                {
                    foreach (var tile in run.BuiltTiles)
                    {
                        if (tile.id == pigeonTile.id ||
                            tile.category != ConstructionCategory.Green ||
                            tile.planting != GreenPlanting.Meadow ||
                            run.GreenGrowthStage(tile.id) != 2 ||
                            run.GreenFoodRouteTo(pigeonTile.id, tile.id).Count == 0)
                            continue;
                        pigeonHomes.Add(tile);
                    }
                }
            }
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
                var wasActive = visual.gameObject.activeSelf;
                visual.gameObject.SetActive(active);
                if (!active)
                {
                    idleMotions.Remove(visual.transform);
                    continue;
                }
                visual.SetVisible(true);
                if (visual.gameObject == transientFoxPrey) continue;
                var homeTile = pigeonHomes[index % pigeonHomes.Count];
                var localIndex = index / pigeonHomes.Count;
                var home = Cell(homeTile.column, homeTile.row) +
                    AnimalOffset(localIndex, true);
                SetAnimalHome(visual.transform, home, homeTile.id,
                    travelingAnimals.Contains(visual.transform) ||
                    (visual == pigeon && pigeonArrival != null), !wasActive);
            }
            var visitorMeadow = run.VisitorPigeonTile;
            while (visitorPigeons.Count < 2 && visitorMeadow != null)
            {
                var actor = NewActor($"Visiting pigeon {visitorPigeons.Count + 1}",
                    world, Vector3.zero);
                var visual = actor.AddComponent<PigeonDemoVisual>();
                visual.Initialize(surface, HideFlags.None);
                SetLayer(actor, 29);
                visitorPigeons.Add(visual);
            }
            for (var index = 0; index < visitorPigeons.Count; index++)
            {
                var visual = visitorPigeons[index];
                if (visual == null) continue;
                var wasActive = visual.gameObject.activeSelf;
                visual.gameObject.SetActive(visitorMeadow != null);
                if (visitorMeadow == null)
                {
                    idleMotions.Remove(visual.transform);
                    continue;
                }
                visual.SetVisible(true);
                var home = Cell(visitorMeadow.column, visitorMeadow.row) +
                    AnimalOffset(index, true);
                SetAnimalHome(visual.transform, home, visitorMeadow.id,
                    false, !wasActive);
            }
            var hedgehogHomes = run.HedgehogHabitats;
            while (hedgehogs.Count < hedgehogHomes.Count)
            {
                var actor = NewActor(hedgehogs.Count == 0
                    ? "Hedgehog visitor" :
                    $"Hedgehog visitor {hedgehogs.Count + 1}",
                    world, Vector3.zero);
                var visual = actor.AddComponent<HedgehogDemoVisual>();
                visual.Initialize(surface, HideFlags.None);
                SetLayer(actor, 29);
                hedgehogs.Add(visual);
                if (hedgehog == null) hedgehog = visual;
            }
            for (var index = 0; index < hedgehogs.Count; index++)
            {
                var visual = hedgehogs[index];
                if (visual == null) continue;
                var wasActive = visual.gameObject.activeSelf;
                var active = index < hedgehogHomes.Count ||
                    visual.gameObject == transientFoxPrey;
                visual.gameObject.SetActive(active);
                if (!active) idleMotions.Remove(visual.transform);
                if (index < hedgehogHomes.Count)
                {
                    var hedgehogTile = hedgehogHomes[index];
                    var home = Cell(hedgehogTile.column, hedgehogTile.row) +
                        new Vector3(-0.48f, 0.23f, 0.42f);
                    SetAnimalHome(visual.transform, home, hedgehogTile.id,
                        travelingAnimals.Contains(visual.transform), !wasActive);
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
                var wasActive = fox.gameObject.activeSelf;
                fox.gameObject.SetActive(foxTile != null);
                if (foxTile == null) idleMotions.Remove(fox.transform);
                if (foxTile != null)
                {
                    var home = Cell(foxTile.column, foxTile.row) +
                        new Vector3(0.46f, 0.23f, -0.36f);
                    SetAnimalHome(fox.transform, home, foxTile.id,
                        fox.transform == travelingFox, !wasActive);
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

        private void SyncIllustratedActors()
        {
            if (squirrel != null)
                ConstructionIllustratedVisualStyle.AttachActor(squirrel.gameObject,
                    ConstructionIllustratedVisualStyle.ActorKind.Squirrel);
            foreach (var visual in squirrels)
                if (visual != null)
                    ConstructionIllustratedVisualStyle.AttachActor(visual.gameObject,
                        ConstructionIllustratedVisualStyle.ActorKind.Squirrel);
            foreach (var visual in pigeons)
                if (visual != null)
                    ConstructionIllustratedVisualStyle.AttachActor(visual.gameObject,
                        ConstructionIllustratedVisualStyle.ActorKind.Pigeon);
            foreach (var visual in visitorPigeons)
                if (visual != null)
                    ConstructionIllustratedVisualStyle.AttachActor(visual.gameObject,
                        ConstructionIllustratedVisualStyle.ActorKind.Pigeon);
            foreach (var visual in hedgehogs)
                if (visual != null)
                    ConstructionIllustratedVisualStyle.AttachActor(visual.gameObject,
                        ConstructionIllustratedVisualStyle.ActorKind.Hedgehog);
            if (fox != null)
                ConstructionIllustratedVisualStyle.AttachActor(fox.gameObject,
                    ConstructionIllustratedVisualStyle.ActorKind.Fox);
            foreach (var visual in residents.Values)
                if (visual != null)
                    ConstructionIllustratedVisualStyle.AttachActor(visual.gameObject,
                        ConstructionIllustratedVisualStyle.ActorKind.Resident);
        }

        private void SetAnimalHome(Transform actor, Vector3 home,
            string tileId, bool preserveCurrentPosition,
            bool newlyActivated = false)
        {
            var changed = !animalHomes.TryGetValue(actor, out var previous) ||
                (previous - home).sqrMagnitude > 0.0001f;
            animalHomes[actor] = home;
            if (tileId != null) animalHomeTiles[actor] = tileId;
            if (!changed && !newlyActivated) return;
            idleMotions.Remove(actor);
            if (!preserveCurrentPosition) actor.position = home;
        }

        private void StartForagingTrips<T>(IReadOnlyList<T> actors,
            IReadOnlyList<IReadOnlyList<ConstructionTileData>> routes, bool bird,
            ConstructionRunModel run, bool matchHomeTile = false)
            where T : MonoBehaviour
        {
            if (routes == null) return;
            var assigned = matchHomeTile ? new HashSet<Transform>() : null;
            for (var routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                var route = routes[routeIndex];
                if (route == null || route.Count == 0) continue;
                var index = routeIndex;
                if (matchHomeTile)
                {
                    index = -1;
                    for (var candidate = 0; candidate < actors.Count; candidate++)
                    {
                        var possible = actors[candidate];
                        if (possible == null ||
                            !possible.gameObject.activeInHierarchy ||
                            assigned.Contains(possible.transform) ||
                            !animalHomeTiles.TryGetValue(possible.transform,
                                out var candidateHomeTileId) ||
                            candidateHomeTileId != route[0].id) continue;
                        index = candidate;
                        assigned.Add(possible.transform);
                        break;
                    }
                    // A meal at an unrepresented oak has no visible actor.
                    if (index < 0) continue;
                }
                else if (index >= actors.Count) break;
                var actor = actors[index];
                if (actor == null || !actor.gameObject.activeInHierarchy ||
                    route == null || route.Count == 0) continue;
                if (run != null && animalHomeTiles.TryGetValue(actor.transform,
                        out var homeTileId))
                    route = run.GreenFoodRouteTo(homeTileId,
                        route[route.Count - 1].id);
                if (route.Count == 0) continue;
                if (travelingAnimals.Contains(actor.transform))
                {
                    pendingForaging[actor.transform] = new PendingForage
                    {
                        route = new List<ConstructionTileData>(route),
                        index = index,
                        bird = bird
                    };
                    continue;
                }
                StartForagingTrip(actor.transform, route, index, bird);
            }
        }

        private void StartForagingTrip(Transform actor,
            IReadOnlyList<ConstructionTileData> route, int index, bool bird)
        {
            idleMotions.Remove(actor);
            travelingAnimals.Add(actor);
            foragingTrips[actor] = StartCoroutine(ForagingTrip(actor,
                route, index, bird));
        }

        private IEnumerator ForagingTrip(Transform actor,
            IReadOnlyList<ConstructionTileData> route, int index, bool bird)
        {
            // One visible journey per meal recorded by the day model. The
            // presentation never decides whether an animal ate.
            var pace = bird ? 1.20f :
                actor != null && actor.GetComponent<HedgehogDemoVisual>() != null
                    ? 1.00f : 1.05f;
            yield return TravelAnimal(actor, route, pace,
                index, bird);
            travelingAnimals.Remove(actor);
            foragingTrips.Remove(actor);
            idleMotions.Remove(actor);
            if (actor == null || !actor.gameObject.activeInHierarchy ||
                !pendingForaging.TryGetValue(actor, out var next))
                yield break;
            pendingForaging.Remove(actor);
            StartForagingTrip(actor, next.route, next.index, next.bird);
        }

        private IEnumerator TravelAnimal(Transform actor,
            IReadOnlyList<ConstructionTileData> route, float pace,
            int visualIndex, bool bird, bool feedAtDestination = true,
            Vector3? doorway = null)
        {
            if (!animalHomes.TryGetValue(actor, out var home)) yield break;
            var routeOffset = actor.GetComponent<HedgehogDemoVisual>() != null
                ? new Vector3(-0.48f, 0.23f, 0.42f)
                : AnimalOffset(visualIndex, bird);
            // Diagonal links run through the two tiles' shared corner. Keep
            // travel waypoints near the cell centres so a decorative home
            // offset cannot visually carry an animal through a closed room.
            var travelOffset = new Vector3(routeOffset.x * 0.2f,
                routeOffset.y, routeOffset.z * 0.2f);
            // A food source in the home tile is still a visible short forage,
            // rather than an invisible one-node route.
            if (route.Count == 1)
            {
                yield return MoveBetween(actor, actor.position,
                    doorway ?? home + new Vector3(-0.52f, 0f, 0.43f),
                    pace, true, true);
                if (actor == null || !actor.gameObject.activeInHierarchy)
                    yield break;
                if (feedAtDestination) yield return PauseToFeed(actor);
                else yield return PauseInHabitat(actor);
                yield return MoveBetween(actor, actor.position, home, pace,
                    true, true);
                yield break;
            }
            for (var index = 1; index < route.Count; index++)
            {
                var target = Cell(route[index].column, route[index].row) +
                    travelOffset;
                yield return MoveBetween(actor, actor.position, target, pace,
                    index == 1, index == route.Count - 1);
                if (actor == null || !actor.gameObject.activeInHierarchy)
                    yield break;
            }
            if (doorway.HasValue)
            {
                yield return MoveBetween(actor, actor.position,
                    doorway.Value, pace, false, true);
                if (actor == null || !actor.gameObject.activeInHierarchy)
                    yield break;
            }
            if (feedAtDestination) yield return PauseToFeed(actor);
            else yield return PauseInHabitat(actor);
            if (doorway.HasValue)
            {
                yield return MoveBetween(actor, actor.position,
                    Cell(route[route.Count - 1].column,
                        route[route.Count - 1].row) + travelOffset,
                    pace, true, false);
                if (actor == null || !actor.gameObject.activeInHierarchy)
                    yield break;
            }
            // Retrace the same animal-passable tiles on the way home.
            // A direct line from the food tile can cut across closed rooms.
            for (var index = route.Count - 2; index >= 0; index--)
            {
                var target = index == 0 ? home :
                    Cell(route[index].column, route[index].row) +
                    travelOffset;
                yield return MoveBetween(actor, actor.position, target, pace,
                    index == route.Count - 2, index == 0);
                if (actor == null || !actor.gameObject.activeInHierarchy)
                    yield break;
            }
        }

        private IEnumerator PauseToFeed(Transform actor)
        {
            feedingAnimals.Add(actor);
            yield return new WaitForSecondsRealtime(0.35f);
            feedingAnimals.Remove(actor);
        }

        private IEnumerator PauseInHabitat(Transform actor)
        {
            restingAnimals.Add(actor);
            yield return new WaitForSecondsRealtime(0.65f);
            restingAnimals.Remove(actor);
        }

        private static IEnumerator MoveBetween(Transform actor, Vector3 from,
            Vector3 to, float pace, bool easeIn = false, bool easeOut = false,
            float minimumDuration = 0.38f)
        {
            var distance = Vector3.Distance(from, to);
            if (distance < 0.001f) yield break;
            var duration = Mathf.Max(minimumDuration, distance * pace / Pitch);
            var elapsed = 0f;
            while (elapsed < duration && actor != null && actor.gameObject.activeInHierarchy)
            {
                var deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                elapsed += deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var blend = easeIn && easeOut
                    ? Mathf.SmoothStep(0f, 1f, progress)
                    : easeIn ? progress * progress * (2f - progress)
                    : easeOut ? progress + progress * progress -
                        progress * progress * progress
                    : progress;
                actor.position = Vector3.LerpUnclamped(from, to, blend);
                FaceDirection(actor, to - from, deltaTime);
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

        private void StopForaging(bool restoreHomes = false)
        {
            StopHabitatVisits(restoreHomes);
            foreach (var trip in foragingTrips.Values)
                if (trip != null) StopCoroutine(trip);
            foragingTrips.Clear();
            pendingForaging.Clear();
            foreach (var actor in travelingAnimals)
                idleMotions.Remove(actor);
            travelingAnimals.Clear();
            feedingAnimals.Clear();
            restingAnimals.Clear();
            if (restoreHomes) RestoreAnimalHomes();
        }

        private static Vector3 AnimalOffset(int index, bool bird)
        {
            var positions = bird ? PigeonOffsets : SquirrelOffsets;
            return positions[index % positions.Length];
        }

        private IEnumerator Commute(CitizenDemoVisual resident,
            IReadOnlyList<ConstructionTileData> route,
            ConstructionTileData homeTile, ConstructionTileData greenTile)
        {
            if (route != null)
                yield return Travel(resident.transform, route, 0.25f);
            if (resident != null && resident.gameObject.activeInHierarchy &&
                homeTile != null && greenTile != null)
                yield return VisitNeighbourGreen(resident.transform,
                    homeTile, greenTile);
            commutingHomes.Remove(resident);
        }

        private IEnumerator VisitNeighbourGreen(Transform actor,
            ConstructionTileData home, ConstructionTileData green)
        {
            // A diagonal neighbour is approached through the shared corner.
            // The mature-green score uses the same eight-neighbour relation.
            var start = actor.localPosition;
            var centre = new Vector3(0f, 0.23f, 0f);
            yield return MoveResidentLocal(actor,
                Cell(home.column, home.row) + centre, 0.18f);
            yield return MoveResidentLocal(actor,
                Cell(green.column, green.row) + centre, 0.28f);
            yield return MoveResidentLocal(actor,
                Cell(green.column, green.row) +
                new Vector3(0.30f, 0.23f, -0.20f), 0.16f);
            var resident = actor.GetComponent<CitizenDemoVisual>();
            if (resident != null) restingResidents.Add(resident);
            yield return new WaitForSecondsRealtime(0.70f);
            if (resident != null) restingResidents.Remove(resident);
            yield return MoveResidentLocal(actor,
                Cell(green.column, green.row) + centre, 0.16f);
            yield return MoveResidentLocal(actor,
                Cell(home.column, home.row) + centre, 0.28f);
            if (actor != null && actor.gameObject.activeInHierarchy)
                actor.localPosition = start;
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
            restingResidents.Clear();
        }

        private static IEnumerator Travel(Transform actor,
            IReadOnlyList<ConstructionTileData> route, float pace)
        {
            var start = actor.localPosition;
            var centreOffset = new Vector3(0f, 0.23f, 0f);
            var roomOffset = new Vector3(0.55f, 0.23f, -0.5f);
            for (var index = 1; index < route.Count; index++)
            {
                if (IsDiagonalStep(route[index - 1], route[index]))
                {
                    // The actor's room offset can move a diagonal line through
                    // a closed side room. Re-centre before crossing the corner.
                    yield return MoveResidentLocal(actor,
                        Cell(route[index - 1].column,
                            route[index - 1].row) + centreOffset, 0.18f);
                    yield return MoveResidentLocal(actor,
                        Cell(route[index].column, route[index].row) +
                        centreOffset, pace);
                    if (index == route.Count - 1)
                        yield return MoveResidentLocal(actor,
                            Cell(route[index].column, route[index].row) +
                            roomOffset, 0.18f);
                }
                else
                    yield return MoveResidentLocal(actor,
                        Cell(route[index].column, route[index].row) +
                        roomOffset, pace);
            }
            actor.localPosition = start;
        }

        private static bool IsDiagonalStep(ConstructionTileData first,
            ConstructionTileData second) =>
            first.column != second.column && first.row != second.row &&
            ConstructionBoardModel.AreNeighbours(first.column, first.row,
                second.column, second.row);

        private static IEnumerator MoveResidentLocal(Transform actor,
            Vector3 to, float duration)
        {
            var from = actor.localPosition;
            if (Vector3.Distance(from, to) < 0.001f) yield break;
            var elapsed = 0f;
            while (elapsed < duration && actor != null &&
                   actor.gameObject.activeInHierarchy)
            {
                elapsed += Time.unscaledDeltaTime;
                actor.localPosition = Vector3.Lerp(from, to,
                    Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(elapsed / duration)));
                FaceDirection(actor, to - from, Time.unscaledDeltaTime);
                yield return null;
            }
            if (actor != null && actor.gameObject.activeInHierarchy)
                actor.localPosition = to;
        }

        private void Update()
        {
            var time = Time.unscaledTime;
            var deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            for (var index = 0; index < squirrels.Count; index++)
            {
                var visual = squirrels[index];
                if (visual == null || !visual.gameObject.activeInHierarchy) continue;
                var traveling = travelingAnimals.Contains(visual.transform);
                var moving = Wander(visual.transform, index, deltaTime,
                    traveling ||
                    visual.gameObject == transientFoxPrey);
                visual.ApplyPose(visual.gameObject == transientFoxPrey
                    ? SquirrelDemoState.Alert
                    : feedingAnimals.Contains(visual.transform)
                        ? SquirrelDemoState.Forage
                         : restingAnimals.Contains(visual.transform)
                             ? SquirrelDemoState.Idle
                         : traveling || moving ? SquirrelDemoState.Hop
                        : SquirrelDemoState.Idle,
                    time + index * 0.23f);
            }
            for (var index = 0; index < pigeons.Count; index++)
            {
                var visual = pigeons[index];
                if (visual == null || !visual.gameObject.activeInHierarchy) continue;
                var arriving = visual == pigeon && pigeonArrival != null;
                var traveling = travelingAnimals.Contains(visual.transform);
                var moving = Wander(visual.transform, index + 5, deltaTime,
                    arriving || traveling ||
                    visual.gameObject == transientFoxPrey);
                visual.ApplyPose(arriving || visual.gameObject == transientFoxPrey
                    ? PigeonDemoState.Flutter
                    : feedingAnimals.Contains(visual.transform)
                        ? PigeonDemoState.Peck
                         : restingAnimals.Contains(visual.transform)
                             ? PigeonDemoState.Idle
                         : traveling || moving ? PigeonDemoState.Walk
                        : PigeonDemoState.Idle, time + index * 0.19f);
            }
            for (var index = 0; index < visitorPigeons.Count; index++)
            {
                var visual = visitorPigeons[index];
                if (visual == null || !visual.gameObject.activeInHierarchy)
                    continue;
                var moving = Wander(visual.transform, index + 17, deltaTime,
                    false);
                visual.ApplyPose(moving ? PigeonDemoState.Walk :
                    PigeonDemoState.Idle, time + index * 0.19f);
            }
            for (var index = 0; index < hedgehogs.Count; index++)
            {
                var visitor = hedgehogs[index];
                if (visitor == null || !visitor.gameObject.activeInHierarchy)
                    continue;
                var traveling = travelingAnimals.Contains(visitor.transform);
                var moving = Wander(visitor.transform, 11 + index,
                    deltaTime, traveling ||
                    visitor.gameObject == transientFoxPrey);
                visitor.ApplyPose(visitor.gameObject == transientFoxPrey
                    ? HedgehogDemoState.Curl
                    : feedingAnimals.Contains(visitor.transform)
                        ? HedgehogDemoState.Sniff
                         : restingAnimals.Contains(visitor.transform)
                             ? HedgehogDemoState.Idle
                         : traveling || moving ? HedgehogDemoState.Waddle
                    : HedgehogDemoState.Idle, time + index * 0.23f);
            }
            if (fox != null && fox.gameObject.activeInHierarchy)
            {
                var moving = Wander(fox.transform, 13, deltaTime,
                    fox.transform == travelingFox ||
                    travelingAnimals.Contains(fox.transform));
                fox.ApplyPose(time < foxContactUntil ? FoxDemoState.Sniff :
                    restingAnimals.Contains(fox.transform)
                        ? FoxDemoState.Rest :
                    fox.transform == travelingFox ||
                    travelingAnimals.Contains(fox.transform) || moving
                        ? FoxDemoState.Trot : FoxDemoState.Rest, time);
            }
            foreach (var resident in residents.Values)
                resident?.ApplyPose(restingResidents.Contains(resident)
                    ? CitizenDemoState.Idle : (resident != null &&
                    commutingHomes.ContainsKey(resident)) ||
                    resident == arrivingResident ? CitizenDemoState.Walk :
                    CitizenDemoState.Idle, time);
        }

        private bool Wander(Transform actor, int index, float deltaTime, bool busy)
        {
            if (busy || !animalHomes.TryGetValue(actor, out var home))
                return false;
            if (!idleMotions.TryGetValue(actor, out var motion))
            {
                motion = new IdleMotion();
                idleMotions[actor] = motion;
                BeginIdleLeg(actor, home, index, motion);
            }
            if (motion.pauseRemaining > 0f)
            {
                motion.pauseRemaining -= deltaTime;
                if (motion.pauseRemaining <= 0f)
                    BeginIdleLeg(actor, home, index, motion);
                return false;
            }
            var before = actor.position;
            motion.elapsed += deltaTime;
            var progress = Mathf.Clamp01(motion.elapsed / motion.duration);
            actor.position = Vector3.Lerp(motion.from, motion.to,
                Mathf.SmoothStep(0f, 1f, progress));
            var movement = actor.position - before;
            FaceDirection(actor, movement, deltaTime);
            if (progress >= 1f)
            {
                motion.step++;
                motion.pauseRemaining = 0.43f +
                    (index + motion.step) % 3 * 0.17f;
            }
            return movement.sqrMagnitude > 0.000001f;
        }

        private static void BeginIdleLeg(Transform actor, Vector3 home,
            int index, IdleMotion motion)
        {
            motion.from = actor.position;
            // An interrupted trip rejoins the home area gradually, while
            // ordinary activity stays inside the animal's own plot.
            motion.to = (motion.from - home).sqrMagnitude > 0.8f * 0.8f
                ? home : home + IdleOffsets[(index + motion.step) %
                    IdleOffsets.Length];
            var speed = index < 5 ? 0.52f : index < 11 ? 0.36f :
                index == 11 ? 0.25f : 0.48f;
            motion.duration = Mathf.Max(0.34f,
                Vector3.Distance(motion.from, motion.to) / speed);
            motion.elapsed = 0f;
            motion.pauseRemaining = 0f;
        }

        private static void FaceDirection(Transform actor, Vector3 direction,
            float deltaTime)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.000001f) return;
            var facing = Quaternion.LookRotation(direction, Vector3.up);
            actor.rotation = Quaternion.Slerp(actor.rotation, facing,
                1f - Mathf.Exp(-8f * deltaTime));
        }

        private void OnDestroy()
        {
            if (usingOpeningBoard && rooms.TryGetValue(
                ConstructionBoardModel.StarterOakId, out var openingOak))
                ConstructionIllustratedVisualStyle.RestoreRoom(openingOak);
            if (tabletop != null)
                tabletop.GetComponent<TabletopBackdropAlignment>()?
                    .ClearGameplayOverride();
            StopTrafficCollision();
            foreach (var entry in hiddenOpeningBoard)
                if (entry.renderer != null) entry.renderer.enabled = entry.enabled;
            if (openingSquirrelVisual != null)
                openingSquirrelVisual.SetVisible(true);
            if (caseHardware != null) Destroy(caseHardware);
            if (endlessGroundMaterial != null) Destroy(endlessGroundMaterial);
            if (endlessGroundTexture != null) Destroy(endlessGroundTexture);
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
        public static GameObject BuildMount(Transform parent, Material surface)
        {
            var mount = new GameObject("Ivory room mounting card");
            mount.transform.SetParent(parent, false);
            mount.layer = parent.gameObject.layer;
            Box("Soft tile shadow", mount.transform,
                new Vector3(0f, 0.025f, 0f), new Vector3(4.30f, 0.11f, 4.30f),
                UrbanPalette.ConstructionFrameShade, surface);
            Box("Warm paper edge", mount.transform,
                new Vector3(0f, 0.060f, 0f), new Vector3(4.23f, 0.075f, 4.23f),
                UrbanPalette.ConstructionIvory, surface);
            Box("Fine brass inset", mount.transform,
                new Vector3(0f, 0.105f, 0f), new Vector3(4.13f, 0.010f, 4.13f),
                UrbanPalette.ConstructionBrass, surface);
            return mount;
        }

        public static void BuildFrame(Transform parent, Material surface)
        {
            BuildMount(parent, surface);
            foreach (var side in new[] { -1f, 1f })
            {
                foreach (var end in new[] { -1f, 1f })
                {
                    Box("North-south wall", parent,
                        new Vector3(end * 1.37f, 0.43f, side * 1.97f),
                        new Vector3(1.16f, 0.42f, 0.14f),
                        UrbanPalette.ConstructionFrameNavy, surface);
                    Box("East-west wall", parent,
                        new Vector3(side * 1.97f, 0.43f, end * 1.37f),
                        new Vector3(0.14f, 0.42f, 1.16f),
                        UrbanPalette.ConstructionFrameNavy, surface);
                    Box("Room corner", parent,
                        new Vector3(side * 1.97f, 0.42f, end * 1.97f),
                        new Vector3(0.19f, 0.58f, 0.19f),
                        UrbanPalette.ConstructionFrameShade, surface);
                    Box("Brass corner tack", parent,
                        new Vector3(side * 1.97f, 0.725f, end * 1.97f),
                        new Vector3(0.07f, 0.018f, 0.07f),
                        UrbanPalette.ConstructionBrass, surface);
                }
                Box("North-south threshold", parent,
                    new Vector3(0f, 0.29f, side * 1.97f),
                    new Vector3(1.22f, 0.08f, 0.17f),
                    UrbanPalette.ConstructionIvory, surface);
                Box("East-west threshold", parent,
                    new Vector3(side * 1.97f, 0.29f, 0f),
                    new Vector3(0.17f, 0.08f, 1.22f),
                    UrbanPalette.ConstructionIvory, surface);
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

    // A shallow inset keeps the playable 7x7 grid unchanged. These layers sit
    // outside the cells and are shared by the opening board and scene fallback.
    public static class ConstructionBoardFinishVisual
    {
        public static GameObject Build(Transform parent, float mapSize, Material surface)
        {
            var finish = new GameObject("Construction board inset finish");
            finish.transform.SetParent(parent, false);
            var half = mapSize * 0.5f;
            foreach (var side in new[] { -1f, 1f })
            {
                Box("Ivory bevel", finish.transform,
                    new Vector3(0f, 0.135f, side * (half + 0.105f)),
                    new Vector3(mapSize + 0.35f, 0.075f, 0.15f),
                    UrbanPalette.ConstructionIvory, surface);
                Box("Ivory bevel", finish.transform,
                    new Vector3(side * (half + 0.105f), 0.135f, 0f),
                    new Vector3(0.15f, 0.075f, mapSize + 0.35f),
                    UrbanPalette.ConstructionIvory, surface);
                Box("Navy carved rail", finish.transform,
                    new Vector3(0f, 0.115f, side * (half + 0.42f)),
                    new Vector3(mapSize + 1.20f, 0.21f, 0.35f),
                    UrbanPalette.ConstructionFrameNavy, surface);
                Box("Navy carved rail", finish.transform,
                    new Vector3(side * (half + 0.42f), 0.115f, 0f),
                    new Vector3(0.35f, 0.21f, mapSize + 1.20f),
                    UrbanPalette.ConstructionFrameNavy, surface);
                Box("Brass hairline", finish.transform,
                    new Vector3(0f, 0.236f, side * (half + 0.245f)),
                    new Vector3(mapSize + 0.55f, 0.016f, 0.025f),
                    UrbanPalette.ConstructionBrass, surface);
                Box("Brass hairline", finish.transform,
                    new Vector3(side * (half + 0.245f), 0.236f, 0f),
                    new Vector3(0.025f, 0.016f, mapSize + 0.55f),
                    UrbanPalette.ConstructionBrass, surface);
                foreach (var end in new[] { -1f, 1f })
                {
                    Box("Brass corner clasp", finish.transform,
                        new Vector3(side * (half + 0.43f), 0.245f,
                            end * (half + 0.43f)),
                        new Vector3(0.31f, 0.04f, 0.31f),
                        UrbanPalette.ConstructionBrass, surface);
                    Box("Corner rivet", finish.transform,
                        new Vector3(side * (half + 0.43f), 0.272f,
                            end * (half + 0.43f)),
                        new Vector3(0.055f, 0.018f, 0.055f),
                        UrbanPalette.ConstructionFrameShade, surface);
                }
            }
            return finish;
        }

        private static void Box(string name, Transform parent, Vector3 position,
            Vector3 size, Color color, Material surface)
        {
            var item = UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cube,
                name, parent, position, size, color, surface, true, HideFlags.None);
            UrbanVisualFactory.SetInkStrength(item.GetComponent<Renderer>(), 0f);
        }
    }
}
