using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class AnimalAmbientMotionFlowTests
    {
        [UnityTest]
        public IEnumerator MiddleDragStopsAnimalTrackingButKeepsCloseView()
        {
            var cameraObject = new GameObject("Tracking Camera Test");
            var animal = new GameObject("Tracked Animal Test");
            try
            {
                cameraObject.transform.SetPositionAndRotation(
                    new Vector3(0f, 32f, 0f), Quaternion.Euler(90f, 0f, 0f));
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 12.9f;
                var controller = cameraObject.AddComponent<BoardCameraController>();
                controller.Initialize(camera);
                controller.Follow(animal.transform, 1.45f);
                animal.transform.position = new Vector3(3f, 0f, 2f);
                yield return null;

                Assert.That(controller.IsFollowing, Is.True);
                Assert.That(controller.TargetPosition.x, Is.EqualTo(3f).Within(0.001f));
                Assert.That(controller.TargetPosition.z, Is.EqualTo(2f).Within(0.001f));

                camera.orthographicSize = 1.45f;
                Assert.That(controller.PanByPixels(new Vector2(80f, 0f), 1000f), Is.True);
                var pannedTarget = controller.TargetPosition;
                Assert.That(controller.TargetSize, Is.EqualTo(1.45f).Within(0.001f));
                animal.transform.position = new Vector3(6f, 0f, 4f);
                yield return null;

                Assert.That(controller.IsFollowing, Is.False);
                Assert.That(Vector3.Distance(controller.TargetPosition, pannedTarget), Is.LessThan(0.001f));
                Assert.That(controller.IsFocused, Is.True);
            }
            finally
            {
                Object.Destroy(cameraObject);
                Object.Destroy(animal);
            }
        }

        [UnityTest]
        public IEnumerator ActiveSpeciesCoverVisibleGroundDuringTheirOwnPhase()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
            Assert.That(persistence, Is.Not.Null);
            Object.Destroy(persistence); // Test never saves over the player's session.
            yield return null;

            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.SetSpeed(1);
            var pigeon = generated.GetComponentsInChildren<PigeonDemoAgent>(true).First();
            var squirrel = generated.GetComponentsInChildren<SquirrelDemoAgent>(true).First();
            var hedgehog = generated.GetComponentsInChildren<HedgehogDemoAgent>(true).First();
            var fox = generated.GetComponentsInChildren<FoxDemoAgent>(true).First();

            runtime.Clock.Restore(40d); // Day: birds and squirrels are active.
            yield return null;
            var dayTravel = new float[2];
            yield return MeasureTravel(new[] { pigeon.transform, squirrel.transform },
                dayTravel, 6f);

            runtime.Clock.Restore(220d); // Night: hedgehogs and foxes are active.
            yield return null;
            var nightTravel = new float[2];
            yield return MeasureTravel(new[] { hedgehog.transform, fox.transform },
                nightTravel, 6f);

            TestContext.WriteLine($"Six-second travel (m): pigeon {dayTravel[0]:F2}, " +
                                  $"squirrel {dayTravel[1]:F2}, hedgehog {nightTravel[0]:F2}, " +
                                  $"fox {nightTravel[1]:F2}.");
            Assert.That(dayTravel.Concat(nightTravel)
                .All(distance => !float.IsNaN(distance) && !float.IsInfinity(distance)), Is.True);
            Assert.That(dayTravel[0], Is.GreaterThan(0.5f), "Pigeons should visibly travel by day.");
            Assert.That(dayTravel[1], Is.GreaterThan(0.5f), "Squirrels should visibly travel by day.");
            Assert.That(nightTravel[0], Is.GreaterThan(0.5f), "Hedgehogs should visibly travel at night.");
            Assert.That(nightTravel[1], Is.GreaterThan(0.7f), "The fox should visibly patrol at night.");
        }

        [UnityTest]
        public IEnumerator FoxPatrolStaysInTheRoomReachedAfterAChase()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            var generated = Object.FindFirstObjectByType<UrbanWildlifeBootstrap>()
                .transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
            Object.Destroy(generated.GetComponentInChildren<SessionPersistenceController>(true));
            yield return null;
            var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
            runtime.StartNewRun(GameMode.Sandbox);
            runtime.SetOnboardingOpen(false);
            runtime.SetSpeed(1);
            generated.GetComponentInChildren<FoxPredationController>(true).enabled = false;
            runtime.Clock.Restore(220d);
            // A restored layout can still be completing a short room-follow
            // relocation; test patrol only after that animation has settled.
            yield return new WaitForSeconds(0.7f);

            var fox = generated.GetComponentInChildren<FoxDemoAgent>(true);
            var destination = generated.GetComponentsInChildren<RoomView>(true)
                .Single(room => room.Spec.Id == "oak-d").VisualRoot.position;
            fox.transform.position = destination + Vector3.up * 0.30f;
            fox.SetActivityEnabled(false);
            fox.SetActivityEnabled(true);
            var distances = new float[1];
            yield return MeasureTravel(new[] { fox.transform }, distances, 4f);

            Assert.That(distances[0], Is.GreaterThan(0.5f));
            Assert.That(Mathf.Abs(fox.transform.position.x - destination.x), Is.LessThan(1.4f));
            Assert.That(Mathf.Abs(fox.transform.position.z - destination.z), Is.LessThan(1.4f));
        }

        private static IEnumerator MeasureTravel(Transform[] actors, float[] distances,
            float seconds)
        {
            var previous = actors.Select(actor => actor.position).ToArray();
            var deadline = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                for (var index = 0; index < actors.Length; index++)
                {
                    var current = actors[index].position;
                    distances[index] += Vector2.Distance(
                        new Vector2(previous[index].x, previous[index].z),
                        new Vector2(current.x, current.z));
                    previous[index] = current;
                }
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
