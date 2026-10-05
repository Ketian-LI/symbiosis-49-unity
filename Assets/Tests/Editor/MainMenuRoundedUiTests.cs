using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class MainMenuRoundedUiTests
    {
        [Test]
        public void MainMenuCardsAndRecordUseRoundedGraphics()
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

                var wordmark = rects.Single(rect => rect.name == "SYMBIOSIS 49 Wordmark");
                Assert.That(wordmark.localScale, Is.EqualTo(Vector3.one),
                    "The title must not be compressed vertically.");
                Assert.That(wordmark.GetComponent<Image>().preserveAspect, Is.True);

                foreach (var name in new[] { "Sandbox Mode Entrance", "Research Mode Entrance" })
                {
                    var card = rects.Single(rect => rect.name == name);
                    var rounded = card.GetComponent<RoundedPanelGraphic>();
                    Assert.That(rounded, Is.Not.Null, name);
                    Assert.That(card.GetComponent<Button>().targetGraphic, Is.SameAs(rounded), name);
                    Assert.That(card.GetComponent<Image>(), Is.Null, name);
                    Assert.That(rounded.CornerRadius, Is.GreaterThan(0f), name);
                }

                var record = rects.Single(rect => rect.name == "Best Record Rounded Backing")
                    .GetComponent<RoundedPanelGraphic>();
                Assert.That(record, Is.Not.Null);
                Assert.That(record.CornerRadius, Is.GreaterThan(0f));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
