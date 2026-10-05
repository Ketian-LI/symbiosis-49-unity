using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
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
            Assert.That(seed.MapBadgeText.text,
                Is.EqualTo(food.Model.PortionsIn("central-park", NaturalFoodKind.Seed).ToString()));
            Assert.That(seed.MapBadgeText.transform.parent.GetComponentsInChildren<Collider>(true),
                Is.Empty, "Map badges must not intercept room clicks and drags.");
            AssertFacilityLabels(rooms, residents.Model.PreviewFacilityUse());

            Assert.That(food.TryConsume("central-park", NaturalFoodKind.Seed), Is.True);
            yield return null; // Destroy of the old badge completes at frame end.
            Assert.That(park.VisualRoot.GetComponentsInChildren<NaturalFoodVisual>(true)
                .Any(visual => visual.name == "Natural Food Seed"), Is.False);

            var dishPosition = park.VisualRoot.TransformPoint(new Vector3(0f, 0.30f, 0f));
            Assert.That(feeding.TryPlaceFood("central-park", dishPosition), Is.True);
            var placed = generated.GetComponentsInChildren<PlayerFoodSourceVisual>(true).Single();
            Assert.That(placed.MapBadgeText.text, Is.EqualTo("5"));
            placed.SetPortions(2);
            Assert.That(placed.MapBadgeText.text, Is.EqualTo("2"));

            var changed = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(changed.TrySwap("residence-d", "pigeon-d"), Is.True);
            Assert.That(editor.RestoreLayout(changed.ExportData()), Is.True);
            AssertFacilityLabels(rooms, residents.Model.PreviewFacilityUse());
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
