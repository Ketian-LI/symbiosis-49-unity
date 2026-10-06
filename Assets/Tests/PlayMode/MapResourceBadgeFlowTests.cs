using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class MapResourceBadgeFlowTests
    {
        [UnityTest]
        public IEnumerator FoodAndFacilityBadgesFollowLiveModelsWithoutBlockingRoomInput()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var food = generated.GetComponentInChildren<NaturalFoodController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var feeding = generated.GetComponentInChildren<PlayerFeedingController>(true);
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.SetSpeed(0);
            yield return null; // Restarted visuals are destroyed at frame end.

            var park = rooms.Single(room => room.Spec.Id == "central-park");
            var seed = park.VisualRoot.GetComponentsInChildren<NaturalFoodVisual>(true)
                .Single(visual => visual.name == "Natural Food Seed");
            Assert.That(seed.MapBadgeText, Is.Not.Null);
            Assert.That(seed.Kind, Is.EqualTo(NaturalFoodKind.Seed));
            Assert.That(seed.AddedToday, Is.EqualTo(
                food.Model.Sources.Values.Single(source => source.roomId == "central-park" &&
                    source.kind == NaturalFoodKind.Seed).addedToday));
            Assert.That(seed.MapBadgeText.text,
                Is.EqualTo(food.Model.PortionsIn("central-park", NaturalFoodKind.Seed).ToString()));
            Assert.That(RoomMapBadgeVisual.DescribeFood(seed.Kind, 1, true),
                Does.Contain("种子 · 剩余 1 份"));
            Assert.That(RoomMapBadgeVisual.DescribeFood(seed.Kind, 1, true, 1),
                Does.Contain("今日实际新增 +1 份"));
            Assert.That(RoomMapBadgeVisual.DescribeFood(NaturalFoodKind.Nut, 2, true),
                Does.Contain("坚果 · 剩余 2 份"));
            Assert.That(RoomMapBadgeVisual.DescribeFood(NaturalFoodKind.Insect, 1, true),
                Does.Contain("昆虫 · 剩余 1 份"));
            Assert.That(RoomMapBadgeVisual.DescribeFood(NaturalFoodKind.DiscardedFood, 1, true),
                Does.Contain("残余食物 · 剩余 1 份"));
            Assert.That(RoomMapBadgeVisual.TryGetFoodScreenRect(
                seed.MapBadgeText, Camera.main, out var screenRect), Is.True);
            var cardCenter = Camera.main.WorldToScreenPoint(
                seed.MapBadgeText.transform.parent.TransformPoint(new Vector3(0f, 0.07f, 0f)));
            Assert.That(screenRect.Contains(cardCenter), Is.True);
            Assert.That(seed.MapBadgeText.transform.parent.GetComponentsInChildren<Collider>(true),
                Is.Empty, "Map badges must not intercept room clicks and drags.");
            AssertFacilityLabels(rooms, residents.Model.PreviewFacilityUse());

            Assert.That(food.TryConsume("central-park", NaturalFoodKind.Seed), Is.True);
            Assert.That(RoomMapBadgeVisual.TryGetFoodScreenRect(
                seed.MapBadgeText, Camera.main, out _), Is.False,
                "The old badge should stop responding to hover as soon as its source is rebuilt.");
            yield return null; // Destroy of the old badge completes at frame end.
            Assert.That(park.VisualRoot.GetComponentsInChildren<NaturalFoodVisual>(true)
                .Any(visual => visual.name == "Natural Food Seed"), Is.False);

            var dishPosition = park.VisualRoot.TransformPoint(new Vector3(0f, 0.30f, 0f));
            Assert.That(feeding.TryPlaceFood("central-park", dishPosition), Is.True);
            var placed = generated.GetComponentsInChildren<PlayerFoodSourceVisual>(true).Single();
            Assert.That(placed.MapBadgeText.text, Is.EqualTo("5"));
            placed.SetPortions(2);
            Assert.That(placed.MapBadgeText.text, Is.EqualTo("2"));
            Assert.That(RoomMapBadgeVisual.DescribeFood(null, 2, false),
                Does.Contain("Placed food · 2 portions left"));
            Assert.That(RoomMapBadgeVisual.DescribeFood(null, 2, true),
                Does.Contain("本次投放 5 份"));
            var tooltip = generated.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "Food Badge Tooltip");
            Assert.That(tooltip.GetComponent<Image>().raycastTarget, Is.False,
                "Hover explanations must not block room clicks or drags.");

            var changed = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(changed.TrySwap("residence-d", "pigeon-d"), Is.True);
            Assert.That(editor.RestoreLayout(changed.ExportData()), Is.True);
            AssertFacilityLabels(rooms, residents.Model.PreviewFacilityUse());
        }

        [UnityTest]
        public IEnumerator FoodIndicatorListsLiveRoomStockAndLocatesSelectedRoom()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var food = generated.GetComponentInChildren<NaturalFoodController>(true);
            var feeding = generated.GetComponentInChildren<PlayerFeedingController>(true);
            var editor = generated.GetComponentInChildren<RoomLayoutEditorController>(true);
            var camera = generated.GetComponentInChildren<BoardCameraController>(true);
            var park = generated.GetComponentsInChildren<RoomView>(true)
                .Single(room => room.Spec.Id == "central-park");
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.SetSpeed(0);
            food.RestoreSession(new[]
            {
                new NaturalFoodSaveData
                {
                    roomId = "central-park", kind = NaturalFoodKind.Seed.ToString(), portions = 3
                }
            });

            var indicator = generated.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "Animals Fed Today");
            var openButton = indicator.GetComponentsInChildren<Button>(true).Single();
            Assert.That(openButton.GetComponent<Image>().raycastTarget, Is.True);
            openButton.onClick.Invoke();
            var panel = generated.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "Food Locations");
            Assert.That(panel.gameObject.activeInHierarchy, Is.True);
            var title = panel.GetComponentsInChildren<Text>(true)
                .Single(item => item.name == "Food Locations Title");
            var firstRow = panel.GetComponentsInChildren<Button>(true)
                .Single(item => item.name == "Food Location 1");
            var count = firstRow.GetComponentsInChildren<Text>(true)
                .Single(item => item.name == "Food Portions");
            Assert.That(title.text, Does.Contain("3"));
            Assert.That(count.text, Does.Contain("3"));

            Assert.That(food.TryConsume("central-park", NaturalFoodKind.Seed), Is.True);
            Assert.That(count.text, Does.Contain("2"),
                "A consumed portion should disappear from the room list immediately.");
            var dishPosition = park.VisualRoot.TransformPoint(new Vector3(0f, 0.30f, 0f));
            Assert.That(feeding.TryPlaceFood("central-park", dishPosition), Is.True);
            Assert.That(count.text, Does.Contain("7"),
                "Natural and placed food in one room should be summed once.");
            firstRow.onClick.Invoke();
            Assert.That(panel.gameObject.activeSelf, Is.False);
            Assert.That(camera.IsFocused, Is.True);
            Assert.That(camera.TargetPosition.x,
                Is.EqualTo(park.transform.parent.position.x).Within(0.01f));
            Assert.That(camera.TargetPosition.z,
                Is.EqualTo(park.transform.parent.position.z).Within(0.01f));

            camera.ReturnToOverviewIfNeeded();
            var pigeonRoom = generated.GetComponentsInChildren<RoomView>(true)
                .Single(room => room.Spec.Id == "pigeon-a");
            var fixedDish = pigeonRoom.VisualRoot.TransformPoint(new Vector3(0f, 0.30f, 0f));
            feeding.RestoreSession(new[]
            {
                new PlayerFoodSourceSaveData
                {
                    id = "food-relocated", roomId = "pigeon-a",
                    x = fixedDish.x, y = fixedDish.y, z = fixedDish.z, portions = 5
                }
            });
            food.RestoreSession(System.Array.Empty<NaturalFoodSaveData>());
            var swapped = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(swapped.TrySwap("pigeon-a", "residence-a"), Is.True);
            Assert.That(editor.RestoreLayout(swapped.ExportData()), Is.True);
            Assert.That(feeding.CurrentRoomIdAt(fixedDish), Is.EqualTo("residence-a"),
                "A fixed dish must be listed under the room currently occupying its tile.");
            openButton.onClick.Invoke();
            var roomName = firstRow.GetComponentsInChildren<Text>(true)
                .Single(item => item.name == "Room Name");
            Assert.That(roomName.text, Is.EqualTo(runtime.Language == InterfaceLanguage.Chinese
                ? "住宅 A" : "Home A"));
            Assert.That(count.text, Does.Contain("5"));

            feeding.RestoreSession(System.Array.Empty<PlayerFoodSourceSaveData>());
            food.RestoreSession(System.Array.Empty<NaturalFoodSaveData>());
            Assert.That(panel.GetComponentsInChildren<Text>(true)
                .Single(item => item.name == "No Food Sources").gameObject.activeSelf, Is.True);
            Assert.That(firstRow.gameObject.activeSelf, Is.False);
        }

        private static void AssertFacilityLabels(RoomView[] rooms,
            ResidentFacilityUsePreview use)
        {
            foreach (var room in rooms.Where(room => room.Spec.Type is
                         RoomType.Office or RoomType.Canteen))
            {
                var badge = room.VisualRoot.GetComponentsInChildren<TextMesh>(true)
                    .Single(label => label.name == "Planned People / Capacity");
                var office = room.Spec.Type == RoomType.Office;
                var counts = office ? use.Offices : use.FoodShops;
                var capacity = office
                    ? ResidentPopulationModel.OfficeCapacity
                    : ResidentPopulationModel.FoodShopCapacity;
                Assert.That(badge.text, Is.EqualTo($"{counts[room.Spec.Id]}/{capacity}"),
                    room.Spec.Id);
                Assert.That(badge.transform.parent.GetComponentsInChildren<Collider>(true),
                    Is.Empty, room.Spec.Id);
            }
        }
    }
}
