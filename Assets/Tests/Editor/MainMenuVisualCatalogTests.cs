using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class MainMenuVisualCatalogTests
    {
        [Test]
        public void EveryApprovedMainMenuVisualLoadsFromResources()
        {
            var paths = new HashSet<string>();
            foreach (MainMenuVisual visual in Enum.GetValues(typeof(MainMenuVisual)))
            {
                var path = MainMenuVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty);
                Assert.That(paths.Add(path), Is.True);
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null);
                Assert.That(MainMenuVisualCatalog.GetSprite(visual), Is.Not.Null);
            }
        }

        [Test]
        public void TitleWordmarkTrimsGenerationMarginsWithoutChangingUiPlacement()
        {
            var texture = Resources.Load<Texture2D>(
                MainMenuVisualCatalog.ResourcePath(MainMenuVisual.TitleWordmark));
            var sprite = MainMenuVisualCatalog.GetSprite(MainMenuVisual.TitleWordmark);

            Assert.That(texture, Is.Not.Null);
            TestContext.WriteLine($"Imported title texture: {texture.width}×{texture.height}");
            Assert.That((float)texture.width / texture.height,
                Is.EqualTo(2169f / 725f).Within(0.02f),
                "Unity must preserve the title texture's original aspect ratio.");
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.rect.width / sprite.texture.width,
                Is.EqualTo(2111f / 2169f).Within(0.001f));
            Assert.That(sprite.rect.height / sprite.texture.height,
                Is.EqualTo(360f / 725f).Within(0.001f));
        }
    }
}
