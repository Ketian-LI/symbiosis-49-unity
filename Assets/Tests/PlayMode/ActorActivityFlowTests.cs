using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.People;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ActorActivityFlowTests
    {
        [UnityTest]
        public IEnumerator EveryLivingAnimalCanOpenAndLeaveTheFollowView()
        {
            yield return LoadRun();
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var camera = generated.GetComponentInChildren<BoardCameraController>(true);
            var director = generated.GetComponentInChildren<PigeonDemoDirector>(true);
            var needs = generated.GetComponentInChildren<AnimalNeedsController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            camera.SetGameplayViewImmediate();

            // This checks the world click path, without a HUD element covering the pointer.
            if (EventSystem.current != null)
            {
                EventSystem.current.gameObject.SetActive(false);
            }

            var animals = new MonoBehaviour[]
            {
                generated.GetComponentInChildren<PigeonDemoAgent>(true),
                generated.GetComponentInChildren<SquirrelDemoAgent>(true),
                generated.GetComponentInChildren<HedgehogDemoAgent>(true),
                generated.GetComponentInChildren<FoxDemoAgent>(true)
            };
            Assert.That(director, Is.Not.Null);
            Assert.That(needs, Is.Not.Null);
            foreach (var animal in animals)
            {
                Assert.That(animal, Is.Not.Null);
                var clickCollider = animal.GetComponent<BoxCollider>();
                Assert.That(clickCollider, Is.Not.Null);
                var clickRay = new Ray(clickCollider.bounds.center + Vector3.up * 5f, Vector3.down);
                Assert.That(Physics.Raycast(clickRay, out var hit, 7f), Is.True);
                Assert.That(hit.collider, Is.EqualTo(clickCollider),
                    $"{animal.GetType().Name}'s click collider is obscured from the board camera.");
                animal.gameObject.SendMessage("OnMouseDown");
                Assert.That(director.SelectedAnimal, Is.EqualTo(animal.transform),
                    $"{animal.GetType().Name} did not enter the shared follow view.");
                Assert.That(camera.IsFocused, Is.True);
                if (animal is not PigeonDemoAgent)
                {
                    var selection = animal.GetComponent<AnimalFollowSelection>();
                    Assert.That(selection, Is.Not.Null,
                        $"{animal.GetType().Name} is missing its observer selection ring.");
                    Assert.That(selection.IsSelected, Is.True);
                    var species = ((IWildlifeLayoutAgent)animal).Species;
                    needs.Model.Animals.Values.First(state => state.species == species).hungerDays = 1;
                    needs.RestoreSession(needs.Model.Export());
                    Assert.That(animal.GetComponent<AnimalNeedIndicator>().VisibleCount, Is.EqualTo(1),
                        $"{animal.GetType().Name} does not reveal its hunger warning when followed.");
                }
                Assert.That(camera.ReturnToOverviewIfNeeded(), Is.True);
                Assert.That(director.SelectedAnimal, Is.Null);
                if (animal is not PigeonDemoAgent)
                {
                    Assert.That(animal.GetComponent<AnimalFollowSelection>().IsSelected, Is.False);
                    Assert.That(animal.GetComponent<AnimalNeedIndicator>().IsVisible, Is.False);
                }
            }

            var hedgehog = (HedgehogDemoAgent)animals[2];
            hedgehog.gameObject.SendMessage("OnMouseDown");
            Assert.That(hedgehog.Vitality.Kill(AnimalDeathCause.Other), Is.True);
            yield return null;
            Assert.That(camera.IsFocused, Is.False,
                "An animal's death must not leave the camera following an invisible target.");
        }

        [UnityTest]
        public IEnumerator TutorialKeepsPeopleAndDaytimeAnimalsMovingWithoutAdvancingTheClock()
        {
            yield return LoadRun();
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(true);

            Assert.That(runtime.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            var initialClock = runtime.Clock.TotalSeconds;
            var citizens = generated.GetComponentsInChildren<CitizenDemoAgent>(true);
            var pigeons = generated.GetComponentsInChildren<PigeonDemoAgent>(true);
            Assert.That(citizens.Length, Is.GreaterThan(0));
            Assert.That(pigeons.Length, Is.GreaterThan(0));

            var citizenMoved = false;
            var pigeonMoved = false;
            var citizenOrigin = citizens[0].transform.position;
            var pigeonOrigin = pigeons[0].transform.position;
            var deadline = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < deadline && (!citizenMoved || !pigeonMoved))
            {
                yield return null;
                citizenMoved |= Vector3.Distance(citizenOrigin, citizens[0].transform.position) > 0.04f;
                pigeonMoved |= Vector3.Distance(pigeonOrigin, pigeons[0].transform.position) > 0.04f;
            }

            Assert.That(citizenMoved, Is.True, "Residents should visibly move while the guide is open.");
            Assert.That(pigeonMoved, Is.True, "Daytime animals should visibly move while the guide is open.");
            Assert.That(runtime.Clock.TotalSeconds, Is.EqualTo(initialClock).Within(0.001d));

            runtime.SetSpeed(0);
            var pausedCitizenPosition = citizens[0].transform.position;
            var pausedPigeonPosition = pigeons[0].transform.position;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Vector3.Distance(pausedCitizenPosition, citizens[0].transform.position),
                Is.LessThan(0.001f), "An explicit player pause must still freeze residents.");
            Assert.That(Vector3.Distance(pausedPigeonPosition, pigeons[0].transform.position),
                Is.LessThan(0.001f), "An explicit player pause must still freeze animals.");
        }

        [UnityTest]
        public IEnumerator AnimalsResumeRoamingWhenTheirDailyActivityWindowReturns()
        {
            yield return LoadRun();
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);

            var hedgehog = generated.GetComponentInChildren<HedgehogDemoAgent>(true);
            var fox = generated.GetComponentInChildren<FoxDemoAgent>(true);
            var pigeon = generated.GetComponentInChildren<PigeonDemoAgent>(true);
            var squirrel = generated.GetComponentInChildren<SquirrelDemoAgent>(true);
            Assert.That(hedgehog, Is.Not.Null);
            Assert.That(fox, Is.Not.Null);
            Assert.That(pigeon, Is.Not.Null);
            Assert.That(squirrel, Is.Not.Null);

            runtime.Clock.Restore(210d); // Night: wake hedgehogs and foxes.
            yield return null;
            var hedgehogOrigin = hedgehog.transform.position;
            var foxOrigin = fox.transform.position;
            var hedgehogMoved = false;
            var foxMoved = false;
            var deadline = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < deadline && (!hedgehogMoved || !foxMoved))
            {
                yield return null;
                hedgehogMoved |= Vector3.Distance(hedgehogOrigin, hedgehog.transform.position) > 0.04f;
                foxMoved |= Vector3.Distance(foxOrigin, fox.transform.position) > 0.04f;
            }
            Assert.That(hedgehogMoved, Is.True, "The hedgehog must leave daytime sleep at night.");
            Assert.That(foxMoved, Is.True, "The fox must leave daytime sleep at night.");

            runtime.Clock.Restore(360d); // New dawn: wake pigeons and squirrels again.
            yield return null;
            var pigeonOrigin = pigeon.transform.position;
            var squirrelOrigin = squirrel.transform.position;
            var pigeonMoved = false;
            var squirrelMoved = false;
            deadline = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < deadline && (!pigeonMoved || !squirrelMoved))
            {
                yield return null;
                pigeonMoved |= Vector3.Distance(pigeonOrigin, pigeon.transform.position) > 0.04f;
                squirrelMoved |= Vector3.Distance(squirrelOrigin, squirrel.transform.position) > 0.04f;
            }
            Assert.That(pigeonMoved, Is.True, "Pigeons must leave nighttime sleep at dawn.");
            Assert.That(squirrelMoved, Is.True, "Squirrels must leave nighttime sleep at dawn.");
        }

        [UnityTest]
        public IEnumerator ResidentsTravelToOfficeFoodShopAndBackHome()
        {
            yield return LoadRun();
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            var residents = generated.GetComponentInChildren<ResidentPopulationController>(true);
            var presenter = generated.GetComponentInChildren<CitizenPopulationPresenter>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.SetSpeed(4);

            var first = residents.Model.Residents[0];
            Assert.That(residents.Model.TryGetRoute(first.id, out var homeId,
                out var officeId, out var shopId), Is.True);
            var agent = presenter.PrimaryAgent;
            var rooms = generated.GetComponentsInChildren<RoomView>(true);
            Assert.That(agent, Is.Not.Null);
            Assert.That(presenter.TryBuildActivityRoute(first.id, out var visibleRoute), Is.True);
            Assert.That(visibleRoute.Count, Is.GreaterThan(3),
                "The tutorial route should include doorway waypoints, not only room centres.");

            runtime.Clock.Restore(20d);
            yield return WaitForResident(agent, officeId,
                rooms.First(room => room.Spec.Id == officeId).VisualRoot.position, 9f);

            runtime.Clock.Restore(115d);
            yield return WaitForResident(agent, shopId,
                rooms.First(room => room.Spec.Id == shopId).VisualRoot.position, 9f);

            runtime.Clock.Restore(190d);
            yield return WaitForResident(agent, homeId,
                rooms.First(room => room.Spec.Id == homeId).VisualRoot.position, 9f);
        }

        private static IEnumerator WaitForResident(CitizenDemoAgent agent,
            string destinationId, Vector3 roomCenter, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline &&
                   (agent.CurrentDestinationRoomId != destinationId ||
                    Vector3.Distance(agent.transform.position, roomCenter) > 1.1f))
            {
                yield return null;
            }
            Assert.That(agent.CurrentDestinationRoomId, Is.EqualTo(destinationId));
            Assert.That(Vector3.Distance(agent.transform.position, roomCenter),
                Is.LessThanOrEqualTo(1.1f), $"Resident did not reach {destinationId} via the room route.");
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        private static IEnumerator LoadRun()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Assert.That(generated, Is.Not.Null);
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Assert.That(persistence, Is.Not.Null);
            Object.Destroy(persistence); // Never write the player's save from a PlayMode test.
            yield return null;
        }
    }
}
