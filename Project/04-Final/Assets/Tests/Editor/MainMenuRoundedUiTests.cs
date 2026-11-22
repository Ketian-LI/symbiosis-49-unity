using System.Linq;
using NUnit.Framework;
using UnityEditor;
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
                Assert.That(wordmark.GetComponent<Image>().sprite, Is.Not.Null,
                    "The title is a separate high-resolution artwork component.");
                Assert.That(wordmark.GetComponent<Image>().raycastTarget, Is.False);

                foreach (var name in new[] { "Sandbox Mode Entrance", "Research Mode Entrance" })
                {
                    var card = rects.Single(rect => rect.name == name);
                    var plaque = card.GetComponentsInChildren<Image>(true)
                        .Single(image => image.name == "Mode plaque artwork");
                    Assert.That(plaque.sprite, Is.Not.Null, name);
                    Assert.That(plaque.raycastTarget, Is.False, name);
                }

                foreach (var name in new[] { "Desktop Settings", "Desktop Language", "Desktop Exit" })
                {
                    var control = rects.Single(rect => rect.name == name);
                    var baseImage = control.Find("Paper Button Backing").GetComponent<Image>();
                    Assert.That(baseImage.sprite, Is.Not.Null, name);
                    Assert.That(baseImage.transform.Find("Control Pictogram")
                        .GetComponent<Image>().sprite, Is.Not.Null, name);
                }

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

        [Test]
        public void ReferenceArtworkImportsWithoutDestructiveScalingOrCompression()
        {
            foreach (var path in new[]
            {
                "Assets/Resources/UI/ConstructionMainMenu/menu-title-lockup-v01.png",
                "Assets/Resources/UI/ConstructionMainMenu/menu-mode-plaque-v02.png",
                "Assets/Resources/UI/GameplayHud/gameplay-tabletop-backdrop-v01.png",
                "Assets/Resources/UI/ConstructionCase/lid-city-oak-v01.png"
            })
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                Assert.That(importer.textureCompression,
                    Is.EqualTo(TextureImporterCompression.Uncompressed), path);
            }
        }
    }
}
