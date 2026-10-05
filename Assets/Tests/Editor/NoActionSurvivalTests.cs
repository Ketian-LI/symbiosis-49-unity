using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class NoActionSurvivalTests
    {
        [Test]
        public void NoDecisionCannotAdvanceTheModelAcrossDawn()
        {
            var result = SimulateDays(8);
            // In the playable mode, even a large simulated delta must stop
            // before the first day boundary until a room is actually changed.
            Assert.That(result.Active, Is.True);
            Assert.That(result.Results, Is.Null);
            Assert.That(result.Day, Is.EqualTo(1));
        }

        private static (bool Active, RunResultsData Results, int Deaths, int Day) SimulateDays(
            int maximumDays)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            // Model settlement recreates decorative scene visuals. Those use
            // play-mode Destroy, which Unity logs as an EditMode-only error.
            var previousLogPolicy = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                var bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<UrbanWildlifeBootstrap>(true))
                    .Single();
                bootstrap.RebuildPreview();
                var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                // Prevent this EditMode simulation from touching the user's
                // persistent save/profile when the run ends.
                var persistence = generated.GetComponentInChildren<SessionPersistenceController>(true);
                UnityEngine.Object.DestroyImmediate(persistence);
                var runtime = generated.GetComponentInChildren<GameRuntimeController>(true);
                var waste = generated.GetComponentInChildren<WasteManagementController>(true);
                var food = generated.GetComponentInChildren<NaturalFoodController>(true);
                var economy = generated.GetComponentInChildren<ResourceEconomyController>(true);
                var mortality = generated.GetComponentInChildren<AnimalMortalityController>(true);

                runtime.RestoreSession(0d, 1, GameMode.Sandbox);
                runtime.ResumeFromDesktop();
                for (var day = 1; day <= maximumDays && runtime.HasActiveRun; day++)
                {
                    runtime.AdvanceSimulation(SimulationClockModel.CycleSeconds);
                    waste.ProcessUntil(runtime.Clock.TotalSeconds);
                    food.ProcessUntil(runtime.Clock.TotalSeconds);
                    economy.ProcessUntil(runtime.Clock.TotalSeconds);
                }
                return (runtime.HasActiveRun, runtime.CurrentResults,
                    mortality.Model.TotalDeaths, runtime.Clock.DayNumber);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousLogPolicy;
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
