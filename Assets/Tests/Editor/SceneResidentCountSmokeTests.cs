using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.People;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class SceneResidentCountSmokeTests
    {
        [Test]
        public void MainSceneVisiblePeopleMatchResidentModel()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<UrbanWildlifeBootstrap>(true))
                    .Single();
                bootstrap.RebuildPreview();
                var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                var model = generated.GetComponentInChildren<ResidentPopulationController>(true).Model;
                var people = generated.GetComponentsInChildren<CitizenDemoAgent>(true);
                Assert.That(model.ResidentCount, Is.EqualTo(4));
                Assert.That(people.Length, Is.EqualTo(model.ResidentCount));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void WorkforceCounterDoesNotOverlapGameplayActions()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<UrbanWildlifeBootstrap>(true))
                    .Single();
                bootstrap.RebuildPreview();
                var generated = bootstrap.transform.Find(UrbanWildlifeBootstrap.GeneratedRootName);
                var rects = generated.GetComponentsInChildren<RectTransform>(true);
                var detail = rects.Single(rect => rect.name == "Workforce Counter");
                var detailCopy = detail.GetComponentInChildren<Text>(true).text;
                Assert.That(detailCopy.Contains("通勤") ||
                            detailCopy.Contains("Commute"), Is.True);
                Assert.That(detailCopy, Does.Contain("/4"));
                Canvas.ForceUpdateCanvases();
                var detailLabel = detail.GetComponentInChildren<Text>(true);
                Assert.That(detailLabel.preferredHeight,
                    Is.LessThanOrEqualTo(detailLabel.rectTransform.rect.height + 2f));
                var detailBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    detail.parent, detail);

                foreach (var actionName in new[]
                {
                    "Enter Layout Editing",
                    "Activate Feeding Mode",
                    "Feeding Mode Instruction"
                })
                {
                    var action = rects.Single(rect => rect.name == actionName);
                    var actionBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                        detail.parent, action);
                    Assert.That(detailBounds.Intersects(actionBounds), Is.False, actionName);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
