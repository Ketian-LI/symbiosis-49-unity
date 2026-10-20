using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.People;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Presentation
{
    // Keeps the original 3D board, tabletop and camera in the menu. This is
    // a reversible presentation state, not a second board or a 2D screenshot.
    public sealed class ConstructionOpeningBoardPreview : MonoBehaviour
    {
        private readonly List<(GameObject target, bool active)> hiddenRooms = new();
        private readonly List<(GameObject target, bool active)> hiddenActors = new();
        private readonly List<(Renderer target, bool enabled)> hiddenRenderers = new();
        private readonly List<(Collider target, bool enabled)> hiddenColliders = new();
        private readonly List<(Transform slot, Vector3 scale)> originalSlotScales = new();
        private readonly List<(Transform face, Vector3 scale)> originalFaceScales = new();
        private readonly List<(Renderer target, Color color)> originalSlotColors = new();
        private GameRuntimeController runtime;
        private RoomView oak;
        private SquirrelDemoAgent squirrel;
        private Transform foundation;
        private Renderer paperBoard;
        private Transform tabletop;
        private Vector3 oakPosition;
        private Vector3 openingOakWorldPosition;
        private Vector3 squirrelPosition;
        private Vector3 tabletopPosition;
        private Vector3 tabletopScale;
        private Transform openingTree;
        private Vector3 openingTreeScale;
        private Vector3 openingTreePosition;
        private bool previewing;
        private bool constructionActive;
        private BoardCameraController boardCamera;
        private TabletopBackdropAlignment tabletopAlignment;
        private GameObject caseShell;
        private Transform westLid;
        private Transform eastLid;
        private Material caseMaterial;
        private Material lidPrintMaterial;
        private readonly List<Material> interiorPrintMaterials = new();
        private Coroutine lidMotion;
        private float lidOpenAmount;

        public bool IsPreviewing => previewing;
        public bool IsCaseClosed => caseShell != null && caseShell.activeInHierarchy &&
            lidOpenAmount <= 0.001f;
        public bool IsCaseOpen => caseShell != null && caseShell.activeInHierarchy &&
            lidOpenAmount >= 0.999f;
        public Transform CaseShell => caseShell == null ? null : caseShell.transform;
        public Transform Foundation => foundation;
        public Transform Tabletop => tabletop;
        public RoomView OpeningOak => oak;
        public SquirrelDemoAgent OpeningSquirrel => squirrel;
        public Camera BoardCamera => boardCamera != null ? boardCamera.GetComponent<Camera>() : null;

        public void Initialize(GameRuntimeController gameRuntime, Transform generated,
            Transform map, IReadOnlyList<RoomView> rooms, SquirrelDemoAgent openingSquirrel)
        {
            runtime = gameRuntime;
            squirrel = openingSquirrel;
            foundation = map == null ? null : map.Find("Fixed Board Foundation");
            tabletop = generated == null ? null : generated.Find("Gameplay Tabletop Backdrop");
            if (tabletop != null)
            {
                tabletopPosition = tabletop.position;
                tabletopScale = tabletop.localScale;
            }
            boardCamera = generated == null ? null :
                generated.GetComponentInChildren<BoardCameraController>(true);
            tabletopAlignment = tabletop == null ? null :
                tabletop.GetComponent<TabletopBackdropAlignment>();
            if (rooms != null)
                foreach (var room in rooms)
                    if (room != null && room.Spec.Id == "oak-a")
                    {
                        oak = room;
                        break;
                    }
            if (runtime != null && runtime.AtDesktop) ShowOpening();
        }

        private void LateUpdate()
        {
            if (runtime == null || oak == null || squirrel == null) return;
            // The construction scene owns this sparse opening board while it is
            // loaded. The legacy runtime can leave Desktop during the additive
            // transition; restoring here would move the only oak back to its
            // old full-board position while the squirrel stays at the centre.
            if (constructionActive)
            {
                // Other legacy presentation systems can still refresh room
                // placements after the additive scene has loaded.
                if (previewing && oak?.VisualRoot != null &&
                    (oak.VisualRoot.position - openingOakWorldPosition).sqrMagnitude > 0.0001f)
                    oak.SetPresentationWorldPosition(openingOakWorldPosition);
                return;
            }
            if (runtime.AtDesktop && !previewing) ShowOpening();
            else if (!runtime.AtDesktop && previewing) RestoreFullBoard();
        }

        public void ShowOpening()
        {
            if (previewing || runtime == null || oak?.VisualRoot == null ||
                squirrel == null || foundation == null || tabletop == null) return;

            oakPosition = oak.VisualRoot.position;
            squirrelPosition = squirrel.transform.position;
            var shift = -new Vector3(oakPosition.x, 0f, oakPosition.z);
            openingOakWorldPosition = oak.VisualRoot.position + shift;
            oak.SetPresentationWorldPosition(openingOakWorldPosition);
            squirrel.transform.position += shift;
            // Emphasise the same authored oak mesh in the sparse opening.
            // Its inner tree is scaled, not the room or lifecycle root, so
            // stage changes cannot move the tree off its existing floor anchor.
            foreach (var child in oak.VisualRoot.GetComponentsInChildren<Transform>(true))
                if (child.name == "Low-poly Mature Tree")
                {
                    openingTree = child;
                    openingTreeScale = child.localScale;
                    openingTreePosition = child.localPosition;
                    child.localScale = openingTreeScale * 1.55f;
                    // The legacy oak sits in a corner. In a board containing
                    // just one room that puts it beneath the food badge, so
                    // bring this same mesh toward the visible room centre.
                    child.localPosition = new Vector3(-0.24f,
                        openingTreePosition.y, 0.14f);
                    break;
                }

            // The construction surface is printed sage with fine ivory joints.
            // Recolour the existing board instead of substituting a 2D image,
            // so the opening and playable board remain the same object.
            paperBoard = foundation.Find("Paper Board")?.GetComponent<Renderer>();
            if (paperBoard != null)
                UrbanVisualFactory.ApplyColor(paperBoard,
                    UrbanPalette.ConstructionGrid);
            var slots = foundation.Find("49 Fixed Cell Slots");
            if (slots != null)
            {
                var slotIndex = 0;
                foreach (Transform slot in slots)
                {
                    originalSlotScales.Add((slot, slot.localScale));
                    var size = WorldScaleStandards.CellSizeMeters - 0.07f;
                    slot.localScale = new Vector3(size, slot.localScale.y, size);
                    var original = (slotIndex / 7 + slotIndex % 7) % 2 == 0
                        ? UrbanPalette.BoardSlot : UrbanPalette.BoardSlotAlternate;
                    var ground = (slotIndex / 7 + slotIndex % 7) % 2 == 0
                        ? UrbanPalette.ConstructionGround :
                            UrbanPalette.ConstructionGroundAlternate;
                    var slotRenderer = slot.GetComponent<Renderer>();
                    if (slotRenderer != null)
                    {
                        originalSlotColors.Add((slotRenderer, original));
                        UrbanVisualFactory.ApplyColor(slotRenderer, ground);
                    }
                    var face = slot.Find("Pastel plot face");
                    if (face != null)
                    {
                        originalFaceScales.Add((face, face.localScale));
                        face.localScale = new Vector3(1f, face.localScale.y, 1f);
                        var faceRenderer = face.GetComponent<Renderer>();
                        if (faceRenderer != null)
                        {
                            originalSlotColors.Add((faceRenderer,
                                UrbanPalette.BoardSlotTop));
                            UrbanVisualFactory.ApplyColor(faceRenderer, ground);
                        }
                    }
                    slotIndex++;
                }
            }

            foreach (var room in GetComponentsInChildren<RoomView>(true))
            {
                if (room == oak || room.VisualRoot == null) continue;
                var root = room.VisualRoot.gameObject;
                hiddenRooms.Add((root, root.activeSelf));
                root.SetActive(false);
            }

            // Agents can re-enable their mesh renderers during Update. Hide
            // the complete legacy actors so an empty construction slot never
            // shows a pigeon, citizen, fox or hedgehog before it is introduced.
            foreach (var actor in GetComponentsInChildren<PigeonDemoAgent>(true))
                HideActor(actor.gameObject);
            foreach (var actor in GetComponentsInChildren<HedgehogDemoAgent>(true))
                HideActor(actor.gameObject);
            foreach (var actor in GetComponentsInChildren<FoxDemoAgent>(true))
                HideActor(actor.gameObject);
            foreach (var actor in GetComponentsInChildren<CitizenDemoAgent>(true))
                HideActor(actor.gameObject);
            foreach (var actor in GetComponentsInChildren<SquirrelDemoAgent>(true))
                if (actor != squirrel) HideActor(actor.gameObject);

            // Wildlife, food badges and traffic outside room roots are also
            // built by the legacy simulation. Hide them without mutating its
            // model, so the saved run can be resumed unchanged.
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (Keep(renderer.transform)) continue;
                hiddenRenderers.Add((renderer, renderer.enabled));
                renderer.enabled = false;
            }
            foreach (var collider in GetComponentsInChildren<Collider>(true))
            {
                if (Keep(collider.transform)) continue;
                hiddenColliders.Add((collider, collider.enabled));
                collider.enabled = false;
            }
            Physics.SyncTransforms();
            previewing = true;
            BuildCaseShell();
            caseShell.SetActive(true);
            SetLidOpenAmount(0f);
        }

        public void RestoreFullBoard()
        {
            if (!previewing) return;
            if (lidMotion != null) StopCoroutine(lidMotion);
            lidMotion = null;
            if (caseShell != null) caseShell.SetActive(false);
            if (paperBoard != null)
                UrbanVisualFactory.ApplyColor(paperBoard, UrbanPalette.Board);
            foreach (var entry in originalSlotScales)
                if (entry.slot != null) entry.slot.localScale = entry.scale;
            originalSlotScales.Clear();
            foreach (var entry in originalFaceScales)
                if (entry.face != null) entry.face.localScale = entry.scale;
            originalFaceScales.Clear();
            foreach (var entry in originalSlotColors)
                if (entry.target != null)
                    UrbanVisualFactory.ApplyColor(entry.target, entry.color);
            originalSlotColors.Clear();
            if (openingTree != null)
            {
                openingTree.localScale = openingTreeScale;
                openingTree.localPosition = openingTreePosition;
                openingTree = null;
            }
            oak.SetPresentationWorldPosition(null);
            oak.VisualRoot.position = oakPosition;
            squirrel.transform.position = squirrelPosition;
            foreach (var entry in hiddenRooms)
                if (entry.target != null) entry.target.SetActive(entry.active);
            foreach (var entry in hiddenActors)
                if (entry.target != null) entry.target.SetActive(entry.active);
            foreach (var entry in hiddenRenderers)
                if (entry.target != null) entry.target.enabled = entry.enabled;
            foreach (var entry in hiddenColliders)
                if (entry.target != null) entry.target.enabled = entry.enabled;
            hiddenRooms.Clear();
            hiddenActors.Clear();
            hiddenRenderers.Clear();
            hiddenColliders.Clear();
            Physics.SyncTransforms();
            previewing = false;
        }

        public void EnterConstruction()
        {
            constructionActive = true;
            SetLidOpenAmount(1f);
            if (oak != null) oak.enabled = false;
            if (squirrel != null) squirrel.enabled = false;
            if (boardCamera != null) boardCamera.enabled = false;
            if (tabletopAlignment != null) tabletopAlignment.enabled = false;
        }

        public void PrepareConstruction()
        {
            // Hold the same centre-oak preview throughout the camera fade.
            // LateUpdate must not restore the legacy room arrangement midway.
            if (!previewing) ShowOpening();
            if (previewing) constructionActive = true;
        }

        public void ExitConstruction()
        {
            constructionActive = false;
            SetInteriorPrintsVisible(true);
            BeginClosing();
            if (foundation != null) foundation.localPosition = Vector3.zero;
            if (tabletop != null)
            {
                tabletop.position = tabletopPosition;
                tabletop.localScale = tabletopScale;
            }
            if (oak != null) oak.enabled = true;
            if (squirrel != null) squirrel.enabled = true;
            if (boardCamera != null) boardCamera.enabled = true;
            if (tabletopAlignment != null)
            {
                tabletopAlignment.enabled = true;
                tabletopAlignment.AlignNow();
            }
        }

        private bool Keep(Transform target) =>
            target.IsChildOf(foundation) || target.IsChildOf(tabletop) ||
            target.IsChildOf(oak.VisualRoot) || target.IsChildOf(squirrel.transform);

        private void HideActor(GameObject actor)
        {
            if (actor == null) return;
            hiddenActors.Add((actor, actor.activeSelf));
            actor.SetActive(false);
        }

        public void BeginOpening()
        {
            if (!previewing || caseShell == null) return;
            AnimateLidTo(1f);
        }

        public void BeginClosing()
        {
            if (!previewing || caseShell == null) return;
            AnimateLidTo(0f);
        }

        public void SetInteriorPrintsVisible(bool visible)
        {
            if (caseShell == null) return;
            foreach (var path in new[]
                     {
                         "West lid hinge/Printed city inside lid",
                         "East lid hinge/Printed oak inside lid"
                     })
            {
                var print = caseShell.transform.Find(path);
                if (print != null && print.TryGetComponent<Renderer>(out var renderer))
                    renderer.enabled = visible;
            }
        }

        private void AnimateLidTo(float target)
        {
            if (lidMotion != null) StopCoroutine(lidMotion);
            lidMotion = StartCoroutine(MoveLid(target));
        }

        private IEnumerator MoveLid(float target)
        {
            var from = lidOpenAmount;
            var duration = 1.15f * Mathf.Abs(target - from);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / duration));
                SetLidOpenAmount(Mathf.Lerp(from, target, progress));
                yield return null;
            }
            SetLidOpenAmount(target);
            lidMotion = null;
        }

        private void SetLidOpenAmount(float amount)
        {
            lidOpenAmount = Mathf.Clamp01(amount);
            var angle = 178f * lidOpenAmount;
            if (westLid != null)
                westLid.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (eastLid != null)
                eastLid.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        private void BuildCaseShell()
        {
            if (caseShell != null) return;
            const float boardWidth = 7f * WorldScaleStandards.CellSizeMeters;
            const float halfCover = (boardWidth + 1.25f) * 0.5f;
            const float lidHeight = 2.8f;
            caseMaterial = UrbanVisualFactory.CreateSurfaceMaterial();
            var lidPrint = Resources.Load<Texture2D>(
                "UI/ConstructionCase/lid-city-oak-v01");
            if (lidPrint != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                    Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
                lidPrintMaterial = new Material(shader);
                lidPrintMaterial.mainTexture = lidPrint;
                if (lidPrintMaterial.HasProperty("_BaseMap"))
                    lidPrintMaterial.SetTexture("_BaseMap", lidPrint);
                if (lidPrintMaterial.HasProperty("_BaseColor"))
                    lidPrintMaterial.SetColor("_BaseColor", Color.white);
            }
            caseShell = new GameObject("Two-part folding board case");
            caseShell.transform.SetParent(foundation, false);

            // The walls turn the existing board into a shallow physical box.
            // They remain outside the plots so the open board stays playable.
            foreach (var side in new[] { -1f, 1f })
            {
                CaseBox("Long wooden case wall", caseShell.transform,
                    new Vector3(side * (halfCover + 0.08f), lidHeight * 0.5f, 0f),
                    new Vector3(0.22f, lidHeight, boardWidth + 1.4f),
                    UrbanPalette.BoardFrameWood);
                CaseBox("Short wooden case wall", caseShell.transform,
                    new Vector3(0f, lidHeight * 0.5f, side * (halfCover + 0.08f)),
                    new Vector3(boardWidth + 1.4f, lidHeight, 0.22f),
                    UrbanPalette.BoardFrameWood);
                CaseBox("Brass upper rim", caseShell.transform,
                    new Vector3(side * (halfCover + 0.08f), lidHeight, 0f),
                    new Vector3(0.25f, 0.055f, boardWidth + 1.4f),
                    UrbanPalette.BoardFrameBrass);
                var hinge = new GameObject(side < 0f ? "West lid hinge" :
                    "East lid hinge").transform;
                hinge.SetParent(caseShell.transform, false);
                hinge.localPosition = new Vector3(side * halfCover,
                    lidHeight + 0.07f, 0f);
                if (side < 0f) westLid = hinge;
                else eastLid = hinge;
                var panelCentre = -side * halfCover * 0.5f;
                CaseBox("Navy lid half", hinge,
                    new Vector3(panelCentre, 0f, 0f),
                    new Vector3(halfCover - 0.045f, 0.18f,
                        boardWidth + 1.25f), UrbanPalette.Boundary);
                CaseBox("Wood lid inset", hinge,
                    new Vector3(panelCentre, 0.105f, 0f),
                    new Vector3(halfCover - 0.37f, 0.04f,
                        boardWidth + 0.88f), UrbanPalette.BoardFrameWood);
                CaseBox("Brass lid outline", hinge,
                    new Vector3(panelCentre, 0.132f, 0f),
                    new Vector3(halfCover - 0.62f, 0.015f,
                        boardWidth + 0.60f), UrbanPalette.BoardFrameBrass);
                CaseBox("Navy lid face", hinge,
                    new Vector3(panelCentre, 0.146f, 0f),
                    new Vector3(halfCover - 0.78f, 0.018f,
                        boardWidth + 0.45f), UrbanPalette.Boundary);
                AddPrintedLidFace(hinge, panelCentre,
                    halfCover - 0.80f, boardWidth + 0.43f, side < 0f);
                // The underside is what the player sees while the physical
                // lid opens. Print the same paper artwork used by the UI leaf
                // so the image does not pop in only after the scene switch.
                AddPrintedInteriorFace(hinge, panelCentre,
                    halfCover - 0.80f, boardWidth + 0.43f, side < 0f);
                foreach (var hingeZ in new[] { -6f, 0f, 6f })
                    CaseBox("Visible brass hinge", hinge,
                        new Vector3(0f, 0.10f, hingeZ),
                        new Vector3(0.43f, 0.18f, 0.74f),
                        UrbanPalette.BoardFrameBrass);
                CaseBox("Front clasp half", hinge,
                    new Vector3(-side * (halfCover - 0.30f), 0.16f,
                        -halfCover + 0.48f),
                    new Vector3(0.38f, 0.07f, 0.62f),
                    UrbanPalette.BoardFrameBrass);
            }
            SetLidOpenAmount(0f);
        }

        private void AddPrintedLidFace(Transform hinge, float centre,
            float width, float depth, bool cityHalf)
        {
            if (lidPrintMaterial == null) return;
            var print = new GameObject(cityHalf ? "Printed city lid" :
                "Printed oak lid", typeof(MeshFilter), typeof(MeshRenderer));
            print.transform.SetParent(hinge, false);
            print.transform.localPosition = new Vector3(centre, 0.158f, 0f);
            print.layer = 29;
            var halfWidth = width * 0.5f;
            var halfDepth = depth * 0.5f;
            var leftU = cityHalf ? 0f : 0.5f;
            var rightU = cityHalf ? 0.5f : 1f;
            var mesh = new Mesh { name = "Printed case lid half" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, halfDepth),
                new Vector3(-halfWidth, 0f, halfDepth)
            };
            mesh.uv = new[]
            {
                new Vector2(leftU, 0f), new Vector2(rightU, 0f),
                new Vector2(rightU, 1f), new Vector2(leftU, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            print.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = print.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = lidPrintMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void AddPrintedInteriorFace(Transform hinge, float centre,
            float width, float depth, bool cityHalf)
        {
            var texture = Resources.Load<Texture2D>(cityHalf
                ? "UI/ConstructionCase/interior-city-v01"
                : "UI/ConstructionCase/interior-oak-v01");
            if (texture == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            var material = new Material(shader) { mainTexture = texture };
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Cull"))
                material.SetInt("_Cull", 0);
            interiorPrintMaterials.Add(material);

            var print = GameObject.CreatePrimitive(PrimitiveType.Cube);
            print.name = cityHalf ? "Printed city inside lid" :
                "Printed oak inside lid";
            print.transform.SetParent(hinge, false);
            print.transform.localPosition = new Vector3(centre, -0.112f, 0f);
            print.transform.localScale = new Vector3(width, 0.018f, depth);
            print.layer = 29;
            var collider = print.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            var renderer = print.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void CaseBox(string name, Transform parent, Vector3 position,
            Vector3 size, Color color)
        {
            var box = UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cube,
                name, parent, position, size, color, caseMaterial, true,
                HideFlags.None);
            box.layer = 29;
            var collider = box.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        private void OnDestroy()
        {
            if (caseMaterial != null) Destroy(caseMaterial);
            if (lidPrintMaterial != null) Destroy(lidPrintMaterial);
            foreach (var material in interiorPrintMaterials)
                if (material != null) Destroy(material);
        }

    }

    public static class ConstructionLaunchContext
    {
        private static ConstructionGameMode? pending;
        public static void Select(ConstructionGameMode mode) => pending = mode;
        public static ConstructionGameMode Take()
        {
            var mode = pending ?? ConstructionGameMode.Fill;
            pending = null;
            return mode;
        }
    }

    // Lives on the Main scene root, not in the legacy HUD implementation.
    // The two modes keep their own scenes and their own save files.
    public sealed class ConstructionMenuEntry : MonoBehaviour
    {
        private const string ConstructionScene = "ConstructionPrototype";
        private GameRuntimeController runtime;
        private Transform menu;
        private Button entrance;
        private Text caption;
        private InterfaceLanguage shownLanguage;
        private bool entering;

        private void LateUpdate()
        {
            // A player build can contain a stale editor preview, which the
            // bootstrap destroys before creating its live HUD. Bind only to
            // an active desktop menu, and rebind if that hierarchy is replaced.
            if (entrance == null || runtime == null || menu == null)
            {
                var liveRuntime = GetComponentInChildren<GameRuntimeController>(true);
                var hud = GetComponentInChildren<UrbanWildlifeHud>(true);
                var liveMenu = hud?.transform.Find("Main Menu");
                if (liveRuntime == null || liveMenu == null ||
                    !liveRuntime.AtDesktop || !liveMenu.gameObject.activeInHierarchy)
                    return;
                runtime = liveRuntime;
                menu = liveMenu;
                CreateEntrance();
            }
            if (shownLanguage != runtime.Language) UpdateCaption();
            var endlessSetup = menu.Find("Endless Difficulty Overlay");
            var researchSetup = menu.Find("Research Setup Overlay");
            var visible = runtime.AtDesktop && !runtime.SettingsOpen &&
                menu.gameObject.activeInHierarchy &&
                (endlessSetup == null || !endlessSetup.gameObject.activeSelf) &&
                (researchSetup == null || !researchSetup.gameObject.activeSelf);
            if (entrance.gameObject.activeSelf != visible)
                entrance.gameObject.SetActive(visible);
        }

        private void CreateEntrance()
        {
            var root = new GameObject("Construction Mode Entrance",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RoundedPanelGraphic), typeof(Button));
            root.transform.SetParent(menu, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.55f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(570f, 94f);
            rect.anchoredPosition = new Vector2(0f, 25f);

            var backing = root.GetComponent<RoundedPanelGraphic>();
            backing.CornerRadius = 26f;
            backing.BorderWidth = 2.5f;
            entrance = root.GetComponent<Button>();
            entrance.targetGraphic = backing;
            entrance.transition = Selectable.Transition.None;
            entrance.onClick.AddListener(EnterConstruction);
            root.AddComponent<MainMenuModeCardFeedback>().Initialize(backing);

            var iconObject = new GameObject("Construction pictogram",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(root.transform, false);
            var icon = iconObject.GetComponent<Image>();
            icon.sprite = ConstructionIconCatalog.GetControl(
                ConstructionControlIcon.FillMode);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(22f, 0f);
            iconRect.sizeDelta = new Vector2(68f, 68f);

            var labelObject = new GameObject("Construction Entry Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(root.transform, false);
            caption = labelObject.GetComponent<Text>();
            caption.alignment = TextAnchor.MiddleLeft;
            caption.fontSize = 32;
            caption.color = new Color(0.98f, 0.94f, 0.84f);
            caption.raycastTarget = false;
            var labelRect = caption.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(108f, 12f);
            labelRect.offsetMax = new Vector2(-82f, -12f);

            var arrowObject = new GameObject("Enter arrow",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            arrowObject.transform.SetParent(root.transform, false);
            var arrow = arrowObject.GetComponent<Text>();
            arrow.font = Resources.Load<Font>("Fonts/Nunito-Bold");
            arrow.fontSize = 46;
            arrow.color = new Color(0.98f, 0.94f, 0.84f);
            arrow.alignment = TextAnchor.MiddleCenter;
            arrow.raycastTarget = false;
            arrow.text = "›";
            var arrowRect = arrow.rectTransform;
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-22f, 0f);
            arrowRect.sizeDelta = new Vector2(42f, 70f);

            // Keep the new entry below existing setup overlays in raycast order.
            var setup = menu.Find("Endless Difficulty Overlay");
            if (setup != null) root.transform.SetSiblingIndex(setup.GetSiblingIndex());
            UpdateCaption();
        }

        private void UpdateCaption()
        {
            shownLanguage = runtime.Language;
            var chinese = shownLanguage == InterfaceLanguage.Chinese;
            caption.font = Resources.Load<Font>(chinese
                ? "Fonts/NotoSansSC-Regular" : "Fonts/Nunito-Bold");
            caption.text = chinese
                ? "填空模式 · 49 天" : "Fill Mode · 49 Days";
        }

        public void EnterConstruction()
        {
            EnterMode(ConstructionGameMode.Fill);
        }

        public void EnterEndlessConstruction()
        {
            EnterMode(ConstructionGameMode.Endless);
        }

        private void EnterMode(ConstructionGameMode mode)
        {
            if (entering || ConstructionSceneTransition.IsActive) return;
            if (!Application.CanStreamedLevelBeLoaded(ConstructionScene))
            {
                Debug.LogError("The construction scene is not in build settings.", this);
                return;
            }
            ConstructionLaunchContext.Select(mode);
            entering = true;
            entrance.interactable = false;
            if (!ConstructionSceneTransition.Enter(menu,
                    GetComponentInChildren<BoardCameraController>(true),
                    ConstructionScene))
            {
                entering = false;
                entrance.interactable = true;
            }
        }
    }

    // The construction UI loads additively over the Main scene. The real board,
    // camera and tabletop remain alive; only the menu/gameplay UI changes.
    internal sealed class ConstructionSceneTransition : MonoBehaviour
    {
        private const float CameraDuration = 1.8f;
        private const float MenuFadeDuration = 0.55f;
        private const float TabletopFadeDuration = 0.32f;
        private static ConstructionSceneTransition active;
        public static bool IsActive => active != null;

        private CanvasGroup tabletop;
        private readonly List<CanvasGroup> menuGroups = new();
        private readonly List<float> originalAlphas = new();

        public static bool Enter(Transform menu, BoardCameraController camera,
            string scene)
        {
            if (active != null) return false;
            var bridge = Create();
            bridge.StartCoroutine(bridge.EnterRoutine(menu, camera, scene));
            return true;
        }

        public static bool ReturnToMenu()
        {
            if (active != null) return false;
            var bridge = Create();
            bridge.StartCoroutine(bridge.ReturnRoutine());
            return true;
        }

        private static ConstructionSceneTransition Create()
        {
            var root = new GameObject("Tabletop scene transition", typeof(Canvas),
                typeof(CanvasScaler), typeof(CanvasGroup),
                typeof(ConstructionSceneTransition));
            DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var imageObject = new GameObject("Matching tabletop", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage), typeof(CanvasGroup));
            imageObject.transform.SetParent(root.transform, false);
            var image = imageObject.GetComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>(
                "UI/GameplayHud/gameplay-tabletop-backdrop-v01");
            image.color = image.texture == null
                ? new Color(0.93f, 0.89f, 0.79f) : Color.white;
            image.raycastTarget = true;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            active = root.GetComponent<ConstructionSceneTransition>();
            active.tabletop = imageObject.GetComponent<CanvasGroup>();
            active.tabletop.alpha = 0f;
            return active;
        }

        private void CaptureMenu(Transform menu)
        {
            menuGroups.Clear();
            originalAlphas.Clear();
            foreach (Transform child in menu)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                var group = child.GetComponent<CanvasGroup>();
                if (group == null)
                    group = child.gameObject.AddComponent<CanvasGroup>();
                menuGroups.Add(group);
                originalAlphas.Add(group.alpha);
            }
        }

        private void SetMenuVisibility(float fraction)
        {
            for (var i = 0; i < menuGroups.Count; i++)
                if (menuGroups[i] != null)
                    menuGroups[i].alpha = originalAlphas[i] * fraction;
        }

        private IEnumerator EnterRoutine(Transform menu,
            BoardCameraController camera, string scene)
        {
            var sharedBoard = FindFirstObjectByType<ConstructionOpeningBoardPreview>();
            sharedBoard?.PrepareConstruction();
            if (sharedBoard != null && sharedBoard.IsPreviewing)
            {
                sharedBoard.BeginOpening();
                CaptureMenu(menu);
                camera?.BeginGameplayViewTransition(CameraDuration);
                var elapsedShared = 0f;
                while (elapsedShared < CameraDuration)
                {
                    elapsedShared += Time.unscaledDeltaTime;
                    SetMenuVisibility(1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(elapsedShared / MenuFadeDuration)));
                    yield return null;
                }
                // Keep the actual board and tabletop visible on both sides of
                // the additive load. A full-screen tabletop image here used to
                // interrupt the camera move before the case leaves opened.
                SetMenuVisibility(1f);
                sharedBoard.EnterConstruction();
                var menuHud = menu.GetComponentInParent<UrbanWildlifeHud>();
                if (menuHud != null) menuHud.gameObject.SetActive(false);
                var load = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                while (!load.isDone) yield return null;
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
                Finish();
                yield break;
            }

            CaptureMenu(menu);
            camera?.BeginGameplayViewTransition(CameraDuration);
            var elapsed = 0f;
            while (elapsed < CameraDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var menuProgress = Mathf.Clamp01(elapsed / MenuFadeDuration);
                SetMenuVisibility(1f - Mathf.SmoothStep(0f, 1f, menuProgress));
                var tabletopProgress = Mathf.Clamp01(
                    (elapsed - CameraDuration + TabletopFadeDuration) /
                    TabletopFadeDuration);
                tabletop.alpha = Mathf.SmoothStep(0f, 1f, tabletopProgress);
                yield return null;
            }
            tabletop.alpha = 1f;
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            yield return null; // Let the construction controller build its UI.
            yield return FadeTabletop(1f, 0f, TabletopFadeDuration);
            Finish();
        }

        private IEnumerator ReturnRoutine()
        {
            var sharedBoard = FindFirstObjectByType<ConstructionOpeningBoardPreview>();
            if (sharedBoard != null && SceneManager.GetSceneByName("Main").isLoaded)
            {
                var main = SceneManager.GetSceneByName("Main");
                SceneManager.SetActiveScene(main);
                // The physical leaves must already be printed when the UI
                // pages disappear with the additive construction scene.
                sharedBoard.SetInteriorPrintsVisible(true);
                var construction = SceneManager.GetSceneByName("ConstructionPrototype");
                if (construction.isLoaded)
                {
                    var unload = SceneManager.UnloadSceneAsync(construction);
                    while (!unload.isDone) yield return null;
                }
                sharedBoard.ExitConstruction();
                var sharedHud = sharedBoard.GetComponentInChildren<UrbanWildlifeHud>(true);
                if (sharedHud != null)
                {
                    sharedHud.gameObject.SetActive(true);
                    var sharedMenu = sharedHud.transform.Find("Main Menu");
                    if (sharedMenu != null)
                    {
                        CaptureMenu(sharedMenu);
                        SetMenuVisibility(0f);
                    }
                }
                var sharedCamera = sharedBoard.GetComponentInChildren<BoardCameraController>(true);
                sharedCamera?.BeginMenuViewTransition(CameraDuration);
                var elapsedShared = 0f;
                while (elapsedShared < CameraDuration)
                {
                    elapsedShared += Time.unscaledDeltaTime;
                    SetMenuVisibility(Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(elapsedShared / MenuFadeDuration)));
                    yield return null;
                }
                SetMenuVisibility(1f);
                Finish();
                yield break;
            }

            yield return FadeTabletop(0f, 1f, TabletopFadeDuration);
            SceneManager.LoadScene("Main", LoadSceneMode.Single);

            Transform menu = null;
            BoardCameraController camera = null;
            for (var frame = 0; frame < 120; frame++)
            {
                var hud = FindFirstObjectByType<UrbanWildlifeHud>();
                menu = hud == null ? null : hud.transform.Find("Main Menu");
                camera = FindFirstObjectByType<BoardCameraController>();
                if (menu != null && camera != null &&
                    menu.Find("Construction Mode Entrance") != null) break;
                yield return null;
            }
            if (menu != null && camera != null)
            {
                CaptureMenu(menu);
                SetMenuVisibility(0f);
                camera.SetGameplayViewImmediate();
                camera.BeginMenuViewTransition(CameraDuration);
            }

            var elapsed = 0f;
            while (elapsed < CameraDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                tabletop.alpha = 1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / TabletopFadeDuration));
                var menuProgress = Mathf.Clamp01(
                    (elapsed - CameraDuration + MenuFadeDuration) /
                    MenuFadeDuration);
                SetMenuVisibility(Mathf.SmoothStep(0f, 1f, menuProgress));
                yield return null;
            }
            tabletop.alpha = 0f;
            SetMenuVisibility(1f);
            Finish();
        }

        private IEnumerator FadeTabletop(float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                tabletop.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            tabletop.alpha = to;
        }

        private void Finish()
        {
            active = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (active == this) active = null;
        }
    }
}
