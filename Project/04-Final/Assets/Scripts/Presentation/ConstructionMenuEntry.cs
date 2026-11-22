using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
        private Transform boundaryCover;
        private bool boundaryWasActive;
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
        private Transform openingIllustratedPlot;
        private Vector3 openingIllustratedPlotScale;
        private bool previewing;
        private bool constructionActive;
        private BoardCameraController boardCamera;
        private TabletopBackdropAlignment tabletopAlignment;
        private GameObject caseShell;
        private GameObject boardFinish;
        private GameObject openingOakMount;
        private Transform westLid;
        private Transform eastLid;
        private Transform frontCaseEdge;
        private Material caseMaterial;
        private Material contactShadowMaterial;
        private Texture2D contactShadowTexture;
        private Material lidPrintMaterial;
        private readonly List<Material> interiorPrintMaterials = new();
        private Coroutine lidMotion;
        private float lidOpenAmount;
        private bool oakWasActive;
        private bool squirrelWasActive;

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
                UpdateOpeningPlotHeight();
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
            oakWasActive = oak.VisualRoot.gameObject.activeSelf;
            squirrelWasActive = squirrel.gameObject.activeSelf;
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
            boundaryCover = foundation.Find("Automatic Boundary Cover Layer");
            boundaryWasActive = boundaryCover != null &&
                boundaryCover.gameObject.activeSelf;
            if (paperBoard != null)
            {
                UrbanVisualFactory.ApplyColor(paperBoard,
                    UrbanPalette.ConstructionGrid);
                UrbanVisualFactory.SetShadowStrength(paperBoard, 0f);
            }
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
                        UrbanVisualFactory.SetShadowStrength(slotRenderer, 0f);
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
                            UrbanVisualFactory.SetShadowStrength(faceRenderer, 0f);
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
            boardFinish = ConstructionBoardFinishVisual.Build(foundation,
                7f * WorldScaleStandards.CellSizeMeters, caseMaterial);
            openingOakMount = ConstructionPlotFrameVisual.BuildMount(
                oak.VisualRoot, caseMaterial);
            caseShell.SetActive(true);
            SetLidOpenAmount(0f);
        }

        public void RestoreFullBoard()
        {
            if (!previewing) return;
            if (lidMotion != null) StopCoroutine(lidMotion);
            lidMotion = null;
            if (caseShell != null) caseShell.SetActive(false);
            if (boardFinish != null) Destroy(boardFinish);
            if (openingOakMount != null) Destroy(openingOakMount);
            boardFinish = null;
            openingOakMount = null;
            if (boundaryCover != null)
                boundaryCover.gameObject.SetActive(boundaryWasActive);
            if (paperBoard != null)
            {
                UrbanVisualFactory.ApplyColor(paperBoard, UrbanPalette.Board);
                UrbanVisualFactory.SetShadowStrength(paperBoard, 1f);
            }
            foreach (var entry in originalSlotScales)
                if (entry.slot != null) entry.slot.localScale = entry.scale;
            originalSlotScales.Clear();
            foreach (var entry in originalFaceScales)
                if (entry.face != null) entry.face.localScale = entry.scale;
            originalFaceScales.Clear();
            foreach (var entry in originalSlotColors)
                if (entry.target != null)
                {
                    UrbanVisualFactory.ApplyColor(entry.target, entry.color);
                    UrbanVisualFactory.SetShadowStrength(entry.target, 1f);
                }
            originalSlotColors.Clear();
            if (openingTree != null)
            {
                openingTree.localScale = openingTreeScale;
                openingTree.localPosition = openingTreePosition;
                openingTree = null;
            }
            if (oak?.VisualRoot != null)
                ConstructionIllustratedVisualStyle.RestoreRoom(
                    oak.VisualRoot.gameObject);
            openingIllustratedPlot = null;
            oak.SetPresentationWorldPosition(null);
            oak.VisualRoot.position = oakPosition;
            squirrel.transform.position = squirrelPosition;
            oak.VisualRoot.gameObject.SetActive(oakWasActive);
            squirrel.gameObject.SetActive(squirrelWasActive);
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
            // The construction scene can now arrive while the case is still
            // unfolding. Let that same physical lid finish its motion.
            if (lidMotion == null) SetLidOpenAmount(1f);
            // The larger ivory mount belongs to the menu's sparse oak preview.
            // Gameplay reuses the oak itself, but not that presentation frame.
            if (openingOakMount != null) openingOakMount.SetActive(false);
            if (oak != null) oak.enabled = false;
            if (squirrel != null) squirrel.enabled = false;
            if (boardCamera != null) boardCamera.enabled = false;
            // The photographed desk must stay camera-facing during the move.
            // Moving its distant perspective plane into the gameplay plane
            // here makes it pass visibly through the board.
            if (tabletopAlignment != null)
            {
                tabletopAlignment.enabled = true;
                tabletopAlignment.AlignNow();
            }
        }

        public void PrepareConstruction()
        {
            // Hold the same centre-oak preview throughout the camera fade.
            // LateUpdate must not restore the legacy room arrangement midway.
            if (!previewing) ShowOpening();
            if (!previewing) return;
            constructionActive = true;
            // The gameplay scene replaces the old 3D oak with an illustrated
            // plot. Make that change under the closed lid, then reveal the
            // very same plot during the opening instead of swapping it after
            // the scene load.
            if (openingIllustratedPlot == null && oak?.VisualRoot != null)
            {
                ConstructionIllustratedVisualStyle.ApplyStarterOak(
                    oak.VisualRoot.gameObject);
                openingIllustratedPlot = oak.VisualRoot.Find(
                    "Illustrated construction plot");
                if (openingIllustratedPlot != null)
                {
                    openingIllustratedPlotScale =
                        openingIllustratedPlot.localScale;
                    UpdateOpeningPlotHeight();
                    SetLidOpenAmount(lidOpenAmount);
                }
            }
        }

        private void UpdateOpeningPlotHeight()
        {
            if (openingIllustratedPlot == null || oak?.VisualRoot == null ||
                boardCamera == null) return;
            // The illustrated tile normally sits above all 3D room meshes.
            // In perspective it stays on its physical cell. The final view
            // is orthographic, where height no longer shifts or scales it.
            // Switching the height in that same frame avoids a visible climb.
            var gameplayView = BoardCamera != null && BoardCamera.orthographic;
            var centre = oak.VisualRoot.position;
            openingIllustratedPlot.position = centre + Vector3.up *
                (gameplayView ? 5f : 0.20f);
        }

        public void ExitConstruction()
        {
            constructionActive = false;
            if (openingOakMount != null) openingOakMount.SetActive(true);
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
            var duration = 1.75f * Mathf.Abs(target - from);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
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
            var angle = 180f * lidOpenAmount;
            if (westLid != null)
                westLid.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (eastLid != null)
                eastLid.localRotation = Quaternion.Euler(0f, 0f, -angle);
            // Let the starter oak grow into view as soon as the leaves clear
            // the centre. Waiting until the final 2% left an empty board in
            // the opening shot and made the oak pop in at the scene handoff.
            var oakReveal = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((lidOpenAmount - 0.42f) / 0.36f));
            if (previewing && oak?.VisualRoot != null)
                oak.VisualRoot.gameObject.SetActive(oakWasActive &&
                    oakReveal > 0.001f);
            if (previewing && openingTree != null)
                openingTree.localScale = openingTreeScale *
                    (1.55f * oakReveal);
            if (previewing && openingIllustratedPlot != null)
                openingIllustratedPlot.localScale =
                    openingIllustratedPlotScale * oakReveal;
            if (previewing && squirrel != null)
                squirrel.gameObject.SetActive(squirrelWasActive &&
                    oakReveal >= 0.95f);
            // The gameplay board already has its own deep-blue/wood perimeter.
            // Showing that perimeter under the closed lid made a second hard
            // frame where the reference has only a soft tabletop shadow.
            if (previewing && boundaryCover != null)
                boundaryCover.gameObject.SetActive(boundaryWasActive &&
                    lidOpenAmount > 0.05f);
            if (previewing && frontCaseEdge != null)
                frontCaseEdge.gameObject.SetActive(lidOpenAmount > 0.05f);
        }

        private void BuildCaseShell()
        {
            if (caseShell != null) return;
            const float boardWidth = 7f * WorldScaleStandards.CellSizeMeters;
            const float halfCover = (boardWidth + 1.25f) * 0.5f;
            // Only a slim physical edge belongs below the printed lid. The
            // apparent extra depth in the reference is a shadow on the desk.
            const float lidHeight = 0.24f;
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
            BuildContactShadow(boardWidth, halfCover);

            // The walls turn the existing board into a shallow physical box.
            // They remain outside the plots so the open board stays playable.
            foreach (var side in new[] { -1f, 1f })
            {
                CaseBox("Long wooden case wall", caseShell.transform,
                    new Vector3(side * (halfCover + 0.08f), lidHeight * 0.5f, 0f),
                    new Vector3(0.22f, lidHeight, boardWidth + 0.75f),
                    UrbanPalette.CaseWall);
                CaseBox(side < 0f ? "Slim front case edge" :
                        "Rear wooden case wall", caseShell.transform,
                    new Vector3(0f, lidHeight * 0.5f, side * (halfCover + 0.08f)),
                    new Vector3(boardWidth + 1.4f, lidHeight, 0.22f),
                    UrbanPalette.CaseWall);
                CaseBox("Brass upper rim", caseShell.transform,
                    new Vector3(side * (halfCover + 0.08f), lidHeight, 0f),
                    new Vector3(0.25f, 0.055f, boardWidth + 1.4f),
                    UrbanPalette.BoardFrameBrass);
                var hinge = new GameObject(side < 0f ? "West lid hinge" :
                    "East lid hinge").transform;
                hinge.SetParent(caseShell.transform, false);
                // The open underside print is 0.122 m above its hinge.
                // Put it at the board rim's 0.15 m top, so the page and
                // board meet in one plane instead of forming a dark gutter.
                hinge.localPosition = new Vector3(side * halfCover,
                    0.15f - 0.122f, 0f);
                if (side < 0f) westLid = hinge;
                else eastLid = hinge;
                var panelCentre = -side * halfCover * 0.5f;
                CaseBox("Navy lid half", hinge,
                    new Vector3(panelCentre, 0f, 0f),
                    new Vector3(halfCover - 0.045f, 0.18f,
                        boardWidth + 0.75f), UrbanPalette.Boundary);
                CaseBox("Wood lid inset", hinge,
                    new Vector3(panelCentre, 0.105f, 0f),
                    new Vector3(halfCover - 0.37f, 0.04f,
                        boardWidth + 0.65f), UrbanPalette.BoardFrameWood);
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
                // A narrow binding rotates with the leaf. It bridges the
                // board rim when open and hides beneath the cover when shut.
                CaseBox("Continuous bookbinding", hinge,
                    new Vector3(0f, -0.15f, 0f),
                    new Vector3(0.46f, 0.035f, boardWidth + 0.65f),
                    UrbanPalette.BoardFrameWood);
                foreach (var hingeZ in new[] { -6f, 0f, 6f })
                    CaseBox("Visible brass hinge", hinge,
                        new Vector3(0f, -0.18f, hingeZ),
                        new Vector3(0.43f, 0.04f, 0.74f),
                        UrbanPalette.BoardFrameBrass);
                CaseBox("Front clasp half", hinge,
                    new Vector3(-side * (halfCover - 0.30f), 0.16f,
                        -halfCover + 0.48f),
                    new Vector3(0.38f, 0.07f, 0.62f),
                    UrbanPalette.BoardFrameBrass);
            }
            frontCaseEdge = caseShell.transform.Find("Slim front case edge");
            SetLidOpenAmount(0f);
        }

        private void BuildContactShadow(float boardWidth, float halfCover)
        {
            // The photographed tabletop cannot receive Unity shadows: its
            // plane is camera-facing and far behind the actual board. A soft
            // transparent footprint supplies the contact shadow without
            // inventing a second solid frame under the box.
            const int resolution = 256;
            // Leave enough transparent margin for the full feather; clipping
            // the quad inside the fade made its far edge look like a new rim.
            var shadowSize = boardWidth + 6.0f;
            contactShadowTexture = new Texture2D(resolution, resolution,
                TextureFormat.RGBA32, false)
            {
                name = "Folding case soft contact shadow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var worldX = (x / (resolution - 1f) - 0.5f) * shadowSize;
                    var worldZ = (y / (resolution - 1f) - 0.5f) * shadowSize;
                    var outsideX = Mathf.Max(0f, Mathf.Abs(worldX) - halfCover);
                    var outsideZ = Mathf.Max(0f, Mathf.Abs(worldZ) - halfCover);
                    var distance = Mathf.Sqrt(outsideX * outsideX +
                        outsideZ * outsideZ);
                    var fade = 1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(distance / 1.65f));
                    contactShadowTexture.SetPixel(x, y,
                        new Color(0.085f, 0.075f, 0.065f, 0.24f * fade));
                }
            }
            contactShadowTexture.Apply(false, false);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Transparent");
            contactShadowMaterial = new Material(shader)
            {
                name = "Tabletop contact shadow - no extra frame",
                mainTexture = contactShadowTexture,
                renderQueue = (int)RenderQueue.Transparent,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (contactShadowMaterial.HasProperty("_BaseMap"))
                contactShadowMaterial.SetTexture("_BaseMap", contactShadowTexture);
            if (contactShadowMaterial.HasProperty("_BaseColor"))
                contactShadowMaterial.SetColor("_BaseColor", Color.white);
            if (contactShadowMaterial.HasProperty("_Surface"))
                contactShadowMaterial.SetFloat("_Surface", 1f);
            if (contactShadowMaterial.HasProperty("_Blend"))
                contactShadowMaterial.SetFloat("_Blend", 0f);
            if (contactShadowMaterial.HasProperty("_SrcBlend"))
                contactShadowMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (contactShadowMaterial.HasProperty("_DstBlend"))
                contactShadowMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (contactShadowMaterial.HasProperty("_ZWrite"))
                contactShadowMaterial.SetFloat("_ZWrite", 0f);
            contactShadowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            contactShadowMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            var shadow = GameObject.CreatePrimitive(PrimitiveType.Plane);
            shadow.name = "Soft tabletop contact shadow";
            shadow.transform.SetParent(caseShell.transform, false);
            shadow.transform.localPosition = new Vector3(0.24f, -0.40f, -0.50f);
            shadow.transform.localScale = new Vector3(shadowSize / 10f, 1f,
                shadowSize / 10f);
            shadow.layer = 29;
            var renderer = shadow.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = contactShadowMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var collider = shadow.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
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
                ? ConstructionCasePaper.LeftInteriorPrint : ConstructionCasePaper.InteriorPrint);
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

            var print = new GameObject(cityHalf ? "Printed city inside lid" :
                "Printed oak inside lid", typeof(MeshFilter), typeof(MeshRenderer));
            print.transform.SetParent(hinge, false);
            // The face is on the underside while shut. After the hinge's
            // half-turn, local +X points toward screen left on both leaves.
            // Reverse U here so the printed page has the same orientation as
            // the screen-space panel that fades over it after the camera move.
            print.transform.localPosition = new Vector3(centre, -0.122f, 0f);
            print.layer = 29;
            var halfWidth = width * 0.5f;
            var halfDepth = depth * 0.5f;
            var mesh = new Mesh { name = "Printed case interior page" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, halfDepth),
                new Vector3(-halfWidth, 0f, halfDepth)
            };
            mesh.uv = new[]
            {
                new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            print.GetComponent<MeshFilter>().sharedMesh = mesh;
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
            if (contactShadowMaterial != null) Destroy(contactShadowMaterial);
            if (contactShadowTexture != null) Destroy(contactShadowTexture);
            if (lidPrintMaterial != null) Destroy(lidPrintMaterial);
            foreach (var material in interiorPrintMaterials)
                if (material != null) Destroy(material);
        }

    }

    public static class ConstructionLaunchContext
    {
        private static ConstructionGameMode? pending;
        private static ConstructionRunModel preparedRun;
        private static ConstructionRunStore preparedStore;
        public static void Select(ConstructionGameMode mode)
        {
            pending = mode;
            preparedRun = null;
            preparedStore = null;
        }
        public static void PrepareRestart(ConstructionRunModel run,
            ConstructionRunStore store)
        {
            pending = run.Mode;
            preparedRun = run;
            preparedStore = store;
        }
        public static bool TakePreparedRestart(out ConstructionRunModel run,
            out ConstructionRunStore store)
        {
            run = preparedRun;
            store = preparedStore;
            preparedRun = null;
            preparedStore = null;
            return run != null;
        }
        public static ConstructionGameMode Take()
        {
            var mode = pending ?? ConstructionGameMode.Fill;
            pending = null;
            return mode;
        }
    }

    // Lives on the Main scene root, not in the legacy HUD implementation.
    // The two modes keep their own scenes and their own save files.
    public sealed partial class ConstructionMenuEntry : MonoBehaviour
    {
        private const string ConstructionScene = "ConstructionPrototype";
        private GameRuntimeController runtime;
        private Transform menu;
        private Button entrance;
        private Button boardGameEntrance;
        private Text caption;
        private Text modeCaption;
        private Text boardGameCaption;
        private Text boardGameModeCaption;
        private InterfaceLanguage shownLanguage;
        private bool entering;
        private bool saveStateOverriddenForTesting;
        private ConstructionSavedRunStatus fillSaveStatus;
        private ConstructionSavedRunStatus endlessSaveStatus;
        private ConstructionSavedRunStatus boardGameSaveStatus;
        private GameObject saveWarningOverlay;
        private Text saveWarningTitle;
        private Text saveWarningBody;
        private Text saveWarningDismissLabel;
        private Text saveWarningArchiveLabel;
        private Button saveWarningArchive;
        private ConstructionGameMode warningMode;
        private ConstructionSavedRunStatus warningStatus;
        private bool warningArchiveFailed;

        public ConstructionSavedRunStatus SavedStatus(ConstructionGameMode mode) =>
            mode switch
            {
                ConstructionGameMode.Endless => endlessSaveStatus,
                ConstructionGameMode.BoardGame => boardGameSaveStatus,
                _ => fillSaveStatus
            };

        public void UseSaveStatesForTesting(ConstructionSavedRunStatus fill,
            ConstructionSavedRunStatus endless)
            => UseSaveStatesForTesting(fill, endless, ConstructionSavedRunStatus.None);

        public void UseSaveStatesForTesting(ConstructionSavedRunStatus fill,
            ConstructionSavedRunStatus endless,
            ConstructionSavedRunStatus boardGame)
        {
            saveStateOverriddenForTesting = true;
            fillSaveStatus = fill;
            endlessSaveStatus = endless;
            boardGameSaveStatus = boardGame;
            if (caption != null) UpdateCaption();
        }

        private void RefreshSaveStates()
        {
            if (!saveStateOverriddenForTesting)
            {
                fillSaveStatus = new ConstructionRunStore(ConstructionRunStore.PathFor(
                    ConstructionGameMode.Fill)).StatusFor(ConstructionGameMode.Fill);
                endlessSaveStatus = new ConstructionRunStore(ConstructionRunStore.PathFor(
                    ConstructionGameMode.Endless)).StatusFor(ConstructionGameMode.Endless);
                boardGameSaveStatus = new ConstructionRunStore(ConstructionRunStore.PathFor(
                    ConstructionGameMode.BoardGame)).StatusFor(ConstructionGameMode.BoardGame);
            }
            if (caption != null) UpdateCaption();
        }

        private void LateUpdate()
        {
            // A player build can contain a stale editor preview, which the
            // bootstrap destroys before creating its live HUD. Bind only to
            // an active desktop menu, and rebind if that hierarchy is replaced.
            if (!EnsureDesktopMenu()) return;
            SymbiosisAudioDirector.Ensure();
            if (shownLanguage != runtime.Language) UpdateCaption();
            var endlessSetup = menu.Find("Endless Difficulty Overlay");
            var researchSetup = menu.Find("Research Setup Overlay");
            var visible = runtime.AtDesktop && !runtime.SettingsOpen &&
                menu.gameObject.activeInHierarchy &&
                (saveWarningOverlay == null || !saveWarningOverlay.activeSelf) &&
                (endlessSetup == null || !endlessSetup.gameObject.activeSelf) &&
                (researchSetup == null || !researchSetup.gameObject.activeSelf);
            if (entrance.gameObject.activeSelf != visible)
                entrance.gameObject.SetActive(visible);
            // The physical-board detector launches a Windows command script.
            // Keep this mode out of macOS builds until the detector is ported.
            var boardGameVisible = visible &&
                Application.platform != RuntimePlatform.OSXPlayer;
            if (boardGameEntrance.gameObject.activeSelf != boardGameVisible)
                boardGameEntrance.gameObject.SetActive(boardGameVisible);
            if (localRankingsEntrance != null &&
                localRankingsEntrance.gameObject.activeSelf != visible)
                localRankingsEntrance.gameObject.SetActive(visible);
        }

        private bool EnsureDesktopMenu()
        {
            if (entrance != null && boardGameEntrance != null &&
                localRankingsEntrance != null &&
                runtime != null && menu != null) return true;
            var liveRuntime = GetComponentInChildren<GameRuntimeController>(true);
            var hud = GetComponentInChildren<UrbanWildlifeHud>(true);
            var liveMenu = hud?.transform.Find("Main Menu");
            if (liveRuntime == null || liveMenu == null ||
                !liveRuntime.AtDesktop || !liveMenu.gameObject.activeInHierarchy)
                return false;
            runtime = liveRuntime;
            menu = liveMenu;
            CreateEntrance();
            return entrance != null && boardGameEntrance != null;
        }

        private void CreateEntrance()
        {
            saveWarningOverlay = null;
            var root = new GameObject("Construction Mode Entrance",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RoundedPanelGraphic), typeof(Button));
            root.transform.SetParent(menu, false);
            var rect = root.GetComponent<RectTransform>();
            ConstructionMainMenuLayout.Place(rect,
                ConstructionMainMenuLayout.FillModeForPlatform);

            var backing = root.GetComponent<RoundedPanelGraphic>();
            backing.CornerRadius = 10f;
            backing.BorderWidth = 0f;
            entrance = root.GetComponent<Button>();
            entrance.targetGraphic = backing;
            entrance.transition = Selectable.Transition.None;
            entrance.onClick.AddListener(EnterConstruction);
            ConstructionMainMenuLayout.AddPlaqueShadow(root.transform);
            var plaque = ConstructionMainMenuLayout.AddPlaqueArtwork(root.transform);
            root.AddComponent<MainMenuModeCardFeedback>().Initialize(backing, plaque);

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
            iconRect.anchoredPosition = new Vector2(11f, 0f);
            iconRect.sizeDelta = new Vector2(44f, 44f);

            var labelObject = new GameObject("Construction Entry Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(root.transform, false);
            caption = labelObject.GetComponent<Text>();
            caption.alignment = TextAnchor.MiddleLeft;
            caption.color = new Color(0.98f, 0.94f, 0.84f);
            caption.raycastTarget = false;
            var labelRect = caption.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(62f, 36f);
            labelRect.offsetMax = new Vector2(-8f, -10f);

            var modeObject = new GameObject("Construction Mode Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            modeObject.transform.SetParent(root.transform, false);
            modeCaption = modeObject.GetComponent<Text>();
            modeCaption.alignment = TextAnchor.MiddleLeft;
            modeCaption.color = new Color(0.98f, 0.94f, 0.84f, 0.90f);
            modeCaption.raycastTarget = false;
            var modeRect = modeCaption.rectTransform;
            modeRect.anchorMin = Vector2.zero;
            modeRect.anchorMax = Vector2.one;
            modeRect.offsetMin = new Vector2(62f, 12f);
            modeRect.offsetMax = new Vector2(-8f, -39f);

            // Both construction variants share the same plaque, icon scale,
            // typography and alignment. The board-game card has its own save.
            var boardRoot = Instantiate(root, menu);
            boardRoot.name = "Board Game Mode Entrance";
            ConstructionMainMenuLayout.Place(
                boardRoot.GetComponent<RectTransform>(),
                ConstructionMainMenuLayout.BoardGameMode);
            boardRoot.GetComponent<MainMenuModeCardFeedback>().Initialize(
                boardRoot.GetComponent<RoundedPanelGraphic>(),
                boardRoot.transform.Find("Mode plaque artwork")
                    .GetComponent<Image>());
            boardGameEntrance = boardRoot.GetComponent<Button>();
            boardGameEntrance.onClick.RemoveAllListeners();
            boardGameEntrance.onClick.AddListener(EnterBoardGameConstruction);
            var boardIcon = boardRoot.transform.Find("Construction pictogram")
                ?.GetComponent<Image>();
            if (boardIcon != null)
                boardIcon.sprite = PauseMenuVisualCatalog.GetSprite(
                    PauseMenuVisual.CameraRecalibrateIcon);
            var boardAction = boardRoot.transform.Find("Construction Entry Label");
            boardGameCaption = boardAction.GetComponent<Text>();
            boardAction.name = "Board Game Entry Label";
            var boardSubtitle = boardRoot.transform.Find("Construction Mode Label");
            boardGameModeCaption = boardSubtitle.GetComponent<Text>();
            boardSubtitle.name = "Board Game Mode Label";

            // Keep both new entries below existing setup overlays in raycast order.
            var setup = menu.Find("Endless Difficulty Overlay");
            if (setup != null)
            {
                root.transform.SetSiblingIndex(setup.GetSiblingIndex());
                boardRoot.transform.SetSiblingIndex(setup.GetSiblingIndex());
            }
            CreateLocalRankingsEntrance();
            RefreshSaveStates();
            LegacyTextTmpBridge.Attach(caption);
            LegacyTextTmpBridge.Attach(modeCaption);
            LegacyTextTmpBridge.Attach(boardGameCaption);
            LegacyTextTmpBridge.Attach(boardGameModeCaption);
        }

        private void UpdateCaption()
        {
            shownLanguage = runtime.Language;
            var chinese = shownLanguage == InterfaceLanguage.Chinese;
            ConstructionMainMenuLayout.ApplyModeFont(caption, chinese, 19);
            ConstructionMainMenuLayout.ApplyModeFont(modeCaption, chinese, 15);
            ConstructionMainMenuLayout.ApplyModeFont(boardGameCaption,
                chinese, 19);
            ConstructionMainMenuLayout.ApplyModeFont(boardGameModeCaption,
                chinese, 15);
            caption.text = ConstructionMainMenuLayout.ActionLabel(fillSaveStatus,
                chinese);
            modeCaption.text = ConstructionMainMenuLayout.ModeLabel(
                ConstructionGameMode.Fill, chinese);
            boardGameCaption.text = ConstructionMainMenuLayout.ActionLabel(
                boardGameSaveStatus, chinese);
            boardGameModeCaption.text = ConstructionMainMenuLayout.ModeLabel(
                ConstructionGameMode.BoardGame, chinese);
            UpdateLocalRankingsLanguage();
            UpdateSaveWarningCopy();
        }

        public void EnterConstruction()
        {
            EnterMode(ConstructionGameMode.Fill);
        }

        public void EnterEndlessConstruction()
        {
            EnterMode(ConstructionGameMode.Endless);
        }

        public void EnterBoardGameConstruction()
        {
            EnterMode(ConstructionGameMode.BoardGame);
        }

        private void EnterMode(ConstructionGameMode mode)
        {
            if (entering || ConstructionSceneTransition.IsActive) return;
            if (mode == ConstructionGameMode.BoardGame &&
                Application.platform == RuntimePlatform.OSXPlayer) return;
            // The Endless card is created by the HUD and can receive a click
            // before this component's first LateUpdate has built the Fill card.
            if (!EnsureDesktopMenu()) return;
            RefreshSaveStates();
            var status = SavedStatus(mode);
            if (status == ConstructionSavedRunStatus.Corrupt ||
                status == ConstructionSavedRunStatus.Unavailable)
            {
                ShowSaveWarning(mode, status);
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(ConstructionScene))
            {
                Debug.LogError("The construction scene is not in build settings.", this);
                return;
            }
            ConstructionLaunchContext.Select(mode);
            entering = true;
            entrance.interactable = false;
            boardGameEntrance.interactable = false;
            if (!ConstructionSceneTransition.Enter(menu,
                    GetComponentInChildren<BoardCameraController>(true),
                    ConstructionScene))
            {
                entering = false;
                entrance.interactable = true;
                boardGameEntrance.interactable = true;
            }
            else if (mode == ConstructionGameMode.Endless ||
                     mode == ConstructionGameMode.BoardGame)
                FindFirstObjectByType<PhysicalBoardCameraController>()?
                    .SetRecognitionSuspended(true);
        }

        private void ShowSaveWarning(ConstructionGameMode mode,
            ConstructionSavedRunStatus status)
        {
            warningMode = mode;
            warningStatus = status;
            warningArchiveFailed = false;
            if (saveWarningOverlay == null)
                CreateSaveWarning();
            saveWarningOverlay.transform.SetAsLastSibling();
            saveWarningOverlay.SetActive(true);
            UpdateSaveWarningCopy();
        }

        private void CreateSaveWarning()
        {
            saveWarningOverlay = new GameObject("Construction Save Warning",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            saveWarningOverlay.transform.SetParent(menu, false);
            var shade = saveWarningOverlay.GetComponent<Image>();
            shade.color = new Color(0.02f, 0.04f, 0.06f, 0.78f);
            shade.raycastTarget = true;
            var shadeRect = shade.rectTransform;
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;

            var border = new GameObject("Gold border", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            border.transform.SetParent(saveWarningOverlay.transform, false);
            var borderImage = border.GetComponent<Image>();
            borderImage.color = new Color(0.78f, 0.61f, 0.34f);
            var borderRect = borderImage.rectTransform;
            borderRect.anchorMin = borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.sizeDelta = new Vector2(670f, 308f);
            borderRect.anchoredPosition = Vector2.zero;

            var panel = new GameObject("Parchment", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(border.transform, false);
            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.96f, 0.91f, 0.79f);
            var panelRect = panelImage.rectTransform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(5f, 5f);
            panelRect.offsetMax = new Vector2(-5f, -5f);

            var header = new GameObject("Navy heading", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            header.transform.SetParent(panel.transform, false);
            var headerImage = header.GetComponent<Image>();
            headerImage.color = new Color(0.11f, 0.18f, 0.23f);
            var headerRect = headerImage.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 63f);
            headerRect.anchoredPosition = Vector2.zero;
            saveWarningTitle = CreateSaveWarningText(header.transform,
                "Save warning title", new Vector2(18f, 6f),
                new Vector2(-18f, -6f), TextAnchor.MiddleCenter,
                new Color(0.98f, 0.94f, 0.84f), 26);

            saveWarningBody = CreateSaveWarningText(panel.transform,
                "Save warning explanation", new Vector2(34f, 90f),
                new Vector2(-34f, -81f), TextAnchor.UpperLeft,
                new Color(0.11f, 0.18f, 0.23f), 22);
            saveWarningBody.lineSpacing = 1.15f;

            saveWarningDismissLabel = CreateSaveWarningButton(panel.transform,
                "Dismiss save warning", new Vector2(-118f, 30f),
                () => saveWarningOverlay.SetActive(false), out _);
            saveWarningArchiveLabel = CreateSaveWarningButton(panel.transform,
                "Archive save and start", new Vector2(118f, 30f),
                ArchiveInvalidSaveAndStart, out saveWarningArchive);
        }

        private static Text CreateSaveWarningText(Transform parent, string name,
            Vector2 insetMin, Vector2 insetMax, TextAnchor alignment,
            Color color, int fontSize)
        {
            var item = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            var label = item.GetComponent<Text>();
            label.alignment = alignment;
            label.color = color;
            label.fontSize = fontSize;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = insetMin;
            rect.offsetMax = insetMax;
            return label;
        }

        private static Text CreateSaveWarningButton(Transform parent, string name,
            Vector2 position, UnityEngine.Events.UnityAction onClick,
            out Button button)
        {
            var item = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(205f, 49f);
            rect.anchoredPosition = position;
            var image = item.GetComponent<Image>();
            image.color = new Color(0.11f, 0.18f, 0.23f);
            button = item.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            return CreateSaveWarningText(item.transform, "Button label",
                new Vector2(8f, 0f), new Vector2(-8f, 0f),
                TextAnchor.MiddleCenter, new Color(0.98f, 0.94f, 0.84f), 19);
        }

        private void UpdateSaveWarningCopy()
        {
            if (saveWarningOverlay == null) return;
            var chinese = shownLanguage == InterfaceLanguage.Chinese;
            ConstructionMainMenuLayout.ApplyModeFont(saveWarningTitle, chinese, 26);
            ConstructionMainMenuLayout.ApplyModeFont(saveWarningBody, chinese, 22);
            ConstructionMainMenuLayout.ApplyModeFont(saveWarningDismissLabel,
                chinese, 19);
            ConstructionMainMenuLayout.ApplyModeFont(saveWarningArchiveLabel,
                chinese, 19);
            saveWarningBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            saveWarningBody.verticalOverflow = VerticalWrapMode.Truncate;
            var corrupt = warningStatus == ConstructionSavedRunStatus.Corrupt;
            saveWarningTitle.text = chinese
                ? corrupt ? "存档异常" : "暂时无法读取存档"
                : corrupt ? "Save cannot be loaded" : "Save unavailable";
            saveWarningBody.text = warningArchiveFailed
                ? chinese
                    ? "归档失败，原存档没有更改。请检查文件权限后重试。"
                    : "Archiving failed. The original save is unchanged. Check file access and try again."
                : corrupt
                    ? chinese
                        ? "此模式的存档无法载入。选择“归档并重开”会将原文件保存在同一文件夹，再开始新游戏。"
                        : "This mode's save cannot be loaded. Archive the original file beside the save, then start a new game."
                    : chinese
                        ? "目前无法读取此模式的存档。文件没有更改。请检查文件权限后重试。"
                        : "This mode's save cannot be read now. The file is unchanged. Check file access and try again.";
            saveWarningDismissLabel.text = chinese ? "返回" : "Back";
            saveWarningArchiveLabel.text = chinese
                ? "归档并重开" : "Archive & Start";
            saveWarningArchive.gameObject.SetActive(corrupt);
            var dismissRect = saveWarningDismissLabel.transform.parent
                .GetComponent<RectTransform>();
            dismissRect.anchoredPosition = corrupt
                ? new Vector2(-118f, 30f) : new Vector2(0f, 30f);
            LegacyTextTmpBridge.Attach(saveWarningTitle);
            LegacyTextTmpBridge.Attach(saveWarningBody);
            LegacyTextTmpBridge.Attach(saveWarningDismissLabel);
            LegacyTextTmpBridge.Attach(saveWarningArchiveLabel);
        }

        private void ArchiveInvalidSaveAndStart()
        {
            var store = new ConstructionRunStore(
                ConstructionRunStore.PathFor(warningMode));
            if (store.TryArchiveInvalid(warningMode, out _))
            {
                saveWarningOverlay.SetActive(false);
                RefreshSaveStates();
                EnterMode(warningMode);
                return;
            }
            // If the file was repaired externally while this dialog was open,
            // show the updated menu status and let the player decide again.
            RefreshSaveStates();
            var currentStatus = SavedStatus(warningMode);
            if (currentStatus != ConstructionSavedRunStatus.Corrupt &&
                currentStatus != ConstructionSavedRunStatus.Unavailable)
            {
                saveWarningOverlay.SetActive(false);
                return;
            }
            warningStatus = currentStatus;
            warningArchiveFailed = true;
            UpdateSaveWarningCopy();
        }

        internal void RestoreEntranceAfterReturn()
        {
            entering = false;
            if (entrance != null) entrance.interactable = true;
            if (boardGameEntrance != null) boardGameEntrance.interactable = true;
            RefreshSaveStates();
        }
    }

    // The construction UI loads additively over the Main scene. The real board,
    // camera and tabletop remain alive; only the menu/gameplay UI changes.
    internal sealed class ConstructionSceneTransition : MonoBehaviour
    {
        private const float CameraDuration = 1.8f;
        private const float MenuFadeDuration = 0.85f;
        private const float TabletopFadeDuration = 0.32f;
        private static ConstructionSceneTransition active;
        private bool entering;
        private bool reloading;
        private bool returnQueued;
        private bool reloadQueued;
        public static bool IsActive => active != null;

        private CanvasGroup tabletop;
        private readonly List<CanvasGroup> menuGroups = new();
        private readonly List<float> originalAlphas = new();
        private readonly List<bool> originalRaycastBlocks = new();

        public static bool Enter(Transform menu, BoardCameraController camera,
            string scene)
        {
            if (active != null) return false;
            var bridge = Create();
            bridge.entering = true;
            bridge.StartCoroutine(bridge.EnterRoutine(menu, camera, scene));
            return true;
        }

        public static bool ReturnToMenu()
        {
            if (active != null)
            {
                if (active.entering || active.reloading)
                    active.returnQueued = true;
                return true;
            }
            var bridge = Create();
            bridge.StartCoroutine(bridge.ReturnRoutine());
            return true;
        }

        public static bool ReloadPreparedRun(ConstructionRunModel run,
            ConstructionRunStore store)
        {
            if (run == null) return false;
            ConstructionLaunchContext.PrepareRestart(run, store);
            if (active != null)
            {
                active.reloadQueued = true;
                return true;
            }
            var bridge = Create();
            bridge.reloading = true;
            bridge.StartCoroutine(bridge.ReloadPreparedRoutine());
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
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
            originalRaycastBlocks.Clear();
            foreach (Transform child in menu)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                var group = child.GetComponent<CanvasGroup>();
                if (group == null)
                    group = child.gameObject.AddComponent<CanvasGroup>();
                menuGroups.Add(group);
                originalAlphas.Add(group.alpha);
                originalRaycastBlocks.Add(group.blocksRaycasts);
            }
        }

        private void SetMenuVisibility(float fraction)
        {
            for (var i = 0; i < menuGroups.Count; i++)
                if (menuGroups[i] != null)
                {
                    menuGroups[i].alpha = originalAlphas[i] * fraction;
                    // A fading menu can already be visible while another
                    // scene transition owns the input. Do not expose a button
                    // that appears clickable but silently ignores its click.
                    menuGroups[i].blocksRaycasts = fraction >= 0.999f &&
                        originalRaycastBlocks[i];
                }
        }

        private IEnumerator EnterRoutine(Transform menu,
            BoardCameraController camera, string scene)
        {
            SymbiosisAudioDirector.SetTransitionDucking(true);
            SymbiosisAudioDirector.Play(SymbiosisSound.CaseOpen);
            var sharedBoard = FindFirstObjectByType<ConstructionOpeningBoardPreview>();
            sharedBoard?.PrepareConstruction();
            if (sharedBoard != null && sharedBoard.IsPreviewing)
            {
                sharedBoard.BeginOpening();
                CaptureMenu(menu);
                // Read the next scene while the physical lids are moving.
                // Start its camera move after the menu fades, while the lids
                // are still unfolding around the board.
                var load = SceneManager.LoadSceneAsync(scene,
                    LoadSceneMode.Additive);
                if (load != null) load.allowSceneActivation = false;
                var elapsedShared = 0f;
                while (sharedBoard != null &&
                       elapsedShared < MenuFadeDuration)
                {
                    // A scene reload can destroy the old menu while this
                    // transition object survives. Stop before using stale UI.
                    if (menu == null || sharedBoard == null)
                    {
                        if (load != null) load.allowSceneActivation = true;
                        Finish();
                        yield break;
                    }
                    elapsedShared += Mathf.Min(Time.unscaledDeltaTime,
                        1f / 30f);
                    SetMenuVisibility(1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(elapsedShared / MenuFadeDuration)));
                    yield return null;
                }
                if (menu == null || sharedBoard == null)
                {
                    if (load != null) load.allowSceneActivation = true;
                    Finish();
                    yield break;
                }
                // Keep the actual board and tabletop visible on both sides of
                // the additive load. A full-screen tabletop image here used to
                // interrupt the camera move before the case leaves opened.
                // Restore stored menu alphas before disabling its HUD. The
                // return transition captures these values for its fade-in;
                // leaving them at zero makes the menu stay invisible.
                SetMenuVisibility(1f);
                sharedBoard.EnterConstruction();
                var menuHud = menu.GetComponentInParent<UrbanWildlifeHud>();
                if (menuHud != null) menuHud.gameObject.SetActive(false);
                if (load == null)
                    load = SceneManager.LoadSceneAsync(scene,
                        LoadSceneMode.Additive);
                else load.allowSceneActivation = true;
                while (!load.isDone) yield return null;
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
                // The interactive paper leaves take over only after both the
                // physical lids and camera reach their final poses.
                var world = FindFirstObjectByType<ConstructionWorldView>();
                while ((world != null && world.UsesOpeningBoard &&
                        world.IsEntranceCameraMoving) ||
                       (sharedBoard != null && !sharedBoard.IsCaseOpen))
                    yield return null;
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

        private IEnumerator ReloadPreparedRoutine()
        {
            var main = SceneManager.GetSceneByName("Main");
            var construction = SceneManager.GetSceneByName("ConstructionPrototype");
            if (main.isLoaded && construction.isLoaded)
            {
                SceneManager.SetActiveScene(main);
                var unload = SceneManager.UnloadSceneAsync(construction);
                while (!unload.isDone) yield return null;
                var load = SceneManager.LoadSceneAsync("ConstructionPrototype",
                    LoadSceneMode.Additive);
                while (!load.isDone) yield return null;
                SceneManager.SetActiveScene(
                    SceneManager.GetSceneByName("ConstructionPrototype"));
            }
            else
            {
                var load = SceneManager.LoadSceneAsync("ConstructionPrototype",
                    LoadSceneMode.Single);
                while (!load.isDone) yield return null;
            }
            Finish();
        }

        private IEnumerator ReturnRoutine()
        {
            SymbiosisAudioDirector.SetTransitionDucking(true);
            SymbiosisAudioDirector.Play(SymbiosisSound.CaseClose);
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
                if (sharedBoard == null)
                {
                    Finish();
                    yield break;
                }
                sharedBoard.ExitConstruction();
                // The Main runtime survives the additive construction scene.
                // Read the language chosen there before its menu becomes visible.
                FindFirstObjectByType<GameRuntimeController>(FindObjectsInactive.Include)?
                    .RefreshLanguageFromPreference();
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
                    if (sharedBoard == null)
                    {
                        Finish();
                        yield break;
                    }
                    elapsedShared += Time.unscaledDeltaTime;
                    SetMenuVisibility(Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(elapsedShared / MenuFadeDuration)));
                    yield return null;
                }
                SetMenuVisibility(1f);
                if (sharedBoard != null)
                    sharedBoard.GetComponentInParent<ConstructionMenuEntry>()?
                        .RestoreEntranceAfterReturn();
                FindFirstObjectByType<PhysicalBoardCameraController>()?
                    .SetRecognitionSuspended(false);
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
            var constructionLoaded =
                SceneManager.GetSceneByName("ConstructionPrototype").isLoaded;
            var returnAfterOpening = (entering || reloading) &&
                returnQueued && constructionLoaded;
            var reloadAfterOpening = reloadQueued && constructionLoaded &&
                !returnAfterOpening;
            SymbiosisAudioDirector.SetTransitionDucking(false);
            active = null;
            Destroy(gameObject);
            if (returnAfterOpening) ReturnToMenu();
            else if (reloadAfterOpening)
            {
                var bridge = Create();
                bridge.reloading = true;
                bridge.StartCoroutine(bridge.ReloadPreparedRoutine());
            }
        }

        private void OnDestroy()
        {
            if (active == this) active = null;
        }
    }
}
