using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.People;

namespace UrbanWildlifeRooms.Presentation
{
    // An observation overlay, not a movement order. Active animal missions use
    // their remaining waypoints; otherwise the line is explicitly a food-access
    // possibility calculated from food in stock and the live doorway graph.
    public sealed class SelectedRouteOverlay : MonoBehaviour
    {
        private static readonly Color WorkColor = new(0.34f, 0.82f, 0.94f, 0.98f);
        private static readonly Color MealColor = new(0.97f, 0.76f, 0.35f, 0.98f);
        private static readonly Color HomeColor = new(0.48f, 0.87f, 0.59f, 0.98f);
        private static readonly Color WildlifeColor = new(0.35f, 0.95f, 0.82f, 0.98f);
        private static readonly Color PreviewColor = new(0.38f, 0.78f, 0.72f, 0.80f);

        private readonly LineRenderer[] lines = new LineRenderer[3];
        private readonly HashSet<string> garageRooms = new(StringComparer.Ordinal);
        private GameRuntimeController runtime;
        private BoardCameraController cameraController;
        private CitizenPopulationPresenter citizens;
        private PigeonDemoDirector wildlifeDirector;
        private AnimalNavigationCoordinator navigation;
        private NaturalFoodController naturalFood;
        private PlayerFeedingController placedFood;
        private FoxPredationController foxPredation;
        private Transform mapRoot;
        private IReadOnlyDictionary<IWildlifeLayoutAgent, string> animalHomes;
        private Material routeMaterial;
        private GameObject statusCard;
        private Text statusText;
        private string selectedResidentId;
        private IWildlifeLayoutAgent selectedAnimal;
        private float nextRefresh;

        public void Initialize(GameRuntimeController gameRuntime,
            BoardCameraController boardCamera,
            CitizenPopulationPresenter residentPresenter,
            PigeonDemoDirector animalDirector,
            AnimalNavigationCoordinator animalNavigation,
            NaturalFoodController naturalFoodController,
            PlayerFeedingController feedingController,
            FoxPredationController predationController,
            Transform boardRoot,
            IReadOnlyDictionary<IWildlifeLayoutAgent, string> homes,
            UrbanWildlifeRooms.UI.UrbanWildlifeHud hud,
            HideFlags hideFlags)
        {
            runtime = gameRuntime;
            cameraController = boardCamera;
            citizens = residentPresenter;
            wildlifeDirector = animalDirector;
            navigation = animalNavigation;
            naturalFood = naturalFoodController;
            placedFood = feedingController;
            foxPredation = predationController;
            mapRoot = boardRoot;
            animalHomes = homes;
            foreach (var room in RoomLayoutData.All.Where(room => room.Type == RoomType.Garage))
                garageRooms.Add(room.Id);

            routeMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Selected Route Overlay",
                hideFlags = HideFlags.HideAndDontSave
            };
            for (var index = 0; index < lines.Length; index++)
            {
                var child = new GameObject($"Route Leg {index + 1}") { hideFlags = hideFlags };
                child.transform.SetParent(transform, false);
                var line = child.AddComponent<LineRenderer>();
                line.sharedMaterial = routeMaterial;
                line.useWorldSpace = true;
                line.startWidth = line.endWidth = 0.11f;
                line.numCornerVertices = line.numCapVertices = 4;
                line.sortingOrder = 35;
                line.enabled = false;
                lines[index] = line;
            }

            BuildStatusCard(hud.transform, hideFlags);
            citizens.ResidentClicked += HandleResidentClicked;
            wildlifeDirector.AnimalSelected += HandleAnimalSelected;
            wildlifeDirector.SelectionCleared += HandleAnimalSelectionCleared;
            runtime.RestartStarting += Clear;
            cameraController.OverviewRestored += Clear;
        }

        private void BuildStatusCard(Transform canvasRoot, HideFlags hideFlags)
        {
            statusCard = new GameObject("Selected Route Status", typeof(RectTransform),
                typeof(Image)) { hideFlags = hideFlags };
            statusCard.transform.SetParent(canvasRoot, false);
            var rect = (RectTransform)statusCard.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 82f);
            rect.sizeDelta = new Vector2(630f, 58f);
            var backing = statusCard.GetComponent<Image>();
            backing.color = new Color(0.10f, 0.14f, 0.18f, 0.88f);
            backing.raycastTarget = false;

            var textObject = new GameObject("Selected Route Label", typeof(RectTransform),
                typeof(Text)) { hideFlags = hideFlags };
            textObject.transform.SetParent(statusCard.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 3f);
            textRect.offsetMax = new Vector2(-14f, -3f);
            statusText = textObject.GetComponent<Text>();
            statusText.font = UrbanFontResolver.GetFont(FontStyle.Bold);
            statusText.fontSize = 19;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = new Color(0.98f, 0.95f, 0.83f);
            statusText.raycastTarget = false;
            statusCard.SetActive(false);
        }

        private void HandleResidentClicked(string residentId, CitizenDemoAgent agent)
        {
            if (!CanInspect() || agent == null) return;
            cameraController.ReturnToOverviewIfNeeded();
            selectedAnimal = null;
            selectedResidentId = residentId;
            RefreshRoute();
        }

        private void HandleAnimalSelected(IWildlifeLayoutAgent animal)
        {
            if (!CanInspect() || animal == null) return;
            selectedResidentId = null;
            selectedAnimal = animal;
            RefreshRoute();
        }

        private void HandleAnimalSelectionCleared()
        {
            // Middle-mouse panning stops camera follow, but the player may still
            // need to pan across a longer route. Right-clicking back to the
            // overview (or selecting something else) clears it as usual.
            if (selectedAnimal != null && cameraController != null &&
                cameraController.IsFocused && !cameraController.IsFollowing)
                return;
            Clear();
        }

        private bool CanInspect() => runtime != null && runtime.HasActiveRun &&
            !runtime.AtDesktop && !runtime.OnboardingOpen && !runtime.PauseMenuOpen &&
            !runtime.SettingsOpen && !runtime.LayoutEditing && !runtime.ResultsOpen;

        private void Update()
        {
            if (selectedResidentId == null && selectedAnimal == null) return;
            if (!CanInspect())
            {
                HideVisuals();
                return;
            }
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.35f;
            RefreshRoute();
        }

        private void RefreshRoute()
        {
            HideVisuals();
            if (!CanInspect()) return;
            var chinese = runtime.Language == InterfaceLanguage.Chinese;
            if (!string.IsNullOrEmpty(selectedResidentId))
            {
                if (citizens.TryBuildActivityRouteLegs(selectedResidentId,
                        out var work, out var meal, out var home))
                {
                    ShowLine(0, work, WorkColor);
                    ShowLine(1, meal, MealColor);
                    ShowLine(2, home, HomeColor);
                    ShowStatus(chinese
                        ? "居民通勤：蓝色上班 → 黄色吃饭 → 绿色回家"
                        : "Resident commute: blue work → amber meal → green home");
                }
                else
                    ShowStatus(chinese
                        ? "当前无法完成上班→吃饭→回家；检查道路与工作／餐饮容量"
                        : "Full commute blocked; check roads and work / meal capacity");
                return;
            }

            if (selectedAnimal is not Component component || component == null ||
                selectedAnimal.AgentTransform == null || !IsAlive(selectedAnimal))
            {
                Clear();
                return;
            }
            if (TryGetActiveMission(selectedAnimal, out var mission, out var returning))
            {
                ShowLine(0, mission, WildlifeColor);
                ShowStatus(returning
                    ? chinese ? "取食后返程 · 绿色为剩余路线"
                        : "Returning after foraging · teal shows the remaining route"
                    : chinese ? "正在取食／狩猎 · 绿色为当前剩余路线"
                        : "Foraging / hunting now · teal shows the remaining route");
                return;
            }
            if (TryBuildFoodOpportunity(selectedAnimal, out var preview))
            {
                ShowLine(0, preview, PreviewColor);
                ShowStatus(chinese
                    ? "可达食物路线预览；尚未出发，也不保证食物仍有份额"
                    : "Reachable food route preview; not a dispatched trip or guaranteed meal");
                return;
            }
            ShowStatus(selectedAnimal.Species == WildlifeSpecies.Fox
                ? chinese ? "当前没有可达残食；狐狸尚未锁定可捕猎目标"
                    : "No reachable scraps; the fox has not locked onto prey"
                : chinese ? "当前没有可达且适合该动物的食物"
                    : "No compatible food is reachable right now");
        }

        private static bool TryGetActiveMission(IWildlifeLayoutAgent animal,
            out IReadOnlyList<Vector3> points, out bool returning)
        {
            returning = false;
            switch (animal)
            {
                case PigeonDemoAgent pigeon:
                    returning = pigeon.IsReturningFromFood;
                    return pigeon.TryGetRemainingFoodRoute(out points);
                case SquirrelDemoAgent squirrel:
                    return squirrel.TryGetRemainingFoodRoute(out points, out returning);
                case HedgehogDemoAgent hedgehog:
                    return hedgehog.TryGetRemainingFoodRoute(out points);
                case FoxDemoAgent fox:
                    return fox.TryGetRemainingFoodRoute(out points);
                default:
                    points = Array.Empty<Vector3>();
                    return false;
            }
        }

        private bool TryBuildFoodOpportunity(IWildlifeLayoutAgent animal,
            out IReadOnlyList<Vector3> points)
        {
            points = Array.Empty<Vector3>();
            var routeMap = navigation?.NavigationMapFor(animal);
            if (routeMap == null || mapRoot == null || animal.AgentTransform == null)
                return false;
            var start = animal.AgentTransform.position;
            var local = mapRoot.InverseTransformPoint(start);
            if (!routeMap.TryFindRoomContaining(new Vector2(local.x, local.z), out var startRoom))
                return false;

            var candidates = new List<(string room, Vector3 position, bool placed)>();
            if (naturalFood?.Model != null)
                foreach (var source in naturalFood.Model.Sources.Values)
                    if (source != null && source.portions > 0 &&
                        CanEatNatural(animal.Species, source.kind) &&
                        naturalFood.TryGetWorldPosition(source.roomId, source.kind, out var position))
                        candidates.Add((source.roomId, position, false));
            if (animal.Species is WildlifeSpecies.Pigeon or WildlifeSpecies.Squirrel &&
                placedFood?.Model != null)
                foreach (var source in placedFood.Model.Sources.Values)
                    if (source != null && source.portions > 0 && source.remainingLifetime > 0f)
                        candidates.Add((source.roomId, source.worldPosition, true));

            var bestDistance = int.MaxValue;
            foreach (var candidate in candidates)
            {
                if (!candidate.placed && !HabitatFoodNetworkModel.CanReachSource(
                        routeMap, startRoom, animal.Species, candidate.room, out _))
                    continue;
                var excluded = animal.Species is WildlifeSpecies.Squirrel or WildlifeSpecies.Hedgehog
                    ? garageRooms : null;
                if (!routeMap.TryFindRoute(startRoom, candidate.room, excluded, out var rooms))
                    continue;
                if (animal.Species == WildlifeSpecies.Squirrel &&
                    (!animalHomes.TryGetValue(animal, out var home) ||
                     !HabitatFoodNetworkModel.SquirrelRouteStaysNearHome(routeMap, home, rooms)))
                    continue;
                if (rooms.Count >= bestDistance) continue;
                if (!TryBuildAnimalWaypoints(routeMap, rooms, start, candidate.position,
                        out var candidatePoints))
                    continue;
                bestDistance = rooms.Count;
                points = candidatePoints;
            }
            if (points.Count < 2 && animal is FoxDemoAgent fox &&
                foxPredation != null &&
                foxPredation.TryGetPotentialPrey(fox, out var preyPosition))
            {
                var preyLocal = mapRoot.InverseTransformPoint(preyPosition);
                if (routeMap.TryFindRoomContaining(
                        new Vector2(preyLocal.x, preyLocal.z), out var preyRoom) &&
                    routeMap.TryFindRoute(startRoom, preyRoom, out var huntRooms) &&
                    TryBuildAnimalWaypoints(routeMap, huntRooms, start,
                        preyPosition, out var huntPoints))
                    points = huntPoints;
            }
            return points.Count >= 2;
        }

        private bool TryBuildAnimalWaypoints(RoomNavigationMap routeMap,
            IReadOnlyList<string> rooms, Vector3 start, Vector3 destination,
            out IReadOnlyList<Vector3> points)
        {
            points = Array.Empty<Vector3>();
            var result = new List<Vector3> { start };
            var local = mapRoot.InverseTransformPoint(start);
            for (var index = 0; index < rooms.Count - 1; index++)
            {
                if (!routeMap.TryGetConnectionPoint(rooms[index], rooms[index + 1],
                        out var door)) return false;
                var waypoint = mapRoot.TransformPoint(new Vector3(door.x, local.y, door.y));
                waypoint.y = start.y;
                result.Add(waypoint);
            }
            result.Add(new Vector3(destination.x, start.y, destination.z));
            points = result;
            return true;
        }

        private static bool CanEatNatural(WildlifeSpecies species, NaturalFoodKind kind) =>
            species switch
            {
                WildlifeSpecies.Pigeon => kind is NaturalFoodKind.Seed or NaturalFoodKind.DiscardedFood,
                WildlifeSpecies.Squirrel => kind == NaturalFoodKind.Nut,
                WildlifeSpecies.Hedgehog => kind == NaturalFoodKind.Insect,
                WildlifeSpecies.Fox => kind == NaturalFoodKind.DiscardedFood,
                _ => false
            };

        private static bool IsAlive(IWildlifeLayoutAgent animal) => animal switch
        {
            PigeonDemoAgent pigeon => pigeon.IsAlive,
            SquirrelDemoAgent squirrel => squirrel.IsAlive,
            HedgehogDemoAgent hedgehog => hedgehog.IsAlive,
            FoxDemoAgent fox => fox.IsAlive,
            _ => false
        };

        private void ShowLine(int index, IReadOnlyList<Vector3> points, Color color)
        {
            if (points == null || points.Count < 2) return;
            var line = lines[index];
            line.startColor = line.endColor = color;
            line.positionCount = points.Count;
            for (var point = 0; point < points.Count; point++)
                line.SetPosition(point, points[point] + Vector3.up * 0.11f);
            line.enabled = true;
        }

        private void ShowStatus(string message)
        {
            statusText.text = message;
            statusCard.SetActive(true);
        }

        private void HideVisuals()
        {
            foreach (var line in lines)
                if (line != null) line.enabled = false;
            if (statusCard != null) statusCard.SetActive(false);
        }

        public void Clear()
        {
            selectedResidentId = null;
            selectedAnimal = null;
            HideVisuals();
        }

        private void OnDestroy()
        {
            if (citizens != null) citizens.ResidentClicked -= HandleResidentClicked;
            if (wildlifeDirector != null)
            {
                wildlifeDirector.AnimalSelected -= HandleAnimalSelected;
                wildlifeDirector.SelectionCleared -= HandleAnimalSelectionCleared;
            }
            if (runtime != null) runtime.RestartStarting -= Clear;
            if (cameraController != null) cameraController.OverviewRestored -= Clear;
            if (routeMaterial != null)
            {
                if (Application.isPlaying) Destroy(routeMaterial);
                else DestroyImmediate(routeMaterial);
            }
        }
    }
}
