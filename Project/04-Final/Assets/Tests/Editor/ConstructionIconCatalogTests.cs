using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionIconCatalogTests
    {
        [Test]
        public void EveryConstructionIconLoadsWithTransparentMarginTrimmed()
        {
            var paths = new HashSet<string>();
            foreach (ConstructionCategory category in Enum.GetValues(
                typeof(ConstructionCategory)))
            {
                if (category == ConstructionCategory.Green)
                {
                    foreach (var planting in new[] { GreenPlanting.Oak,
                                 GreenPlanting.Meadow, GreenPlanting.Shrub })
                        Check(ConstructionIconCatalog.BuildResourcePath(category,
                            planting), ConstructionIconCatalog.GetBuild(category,
                            planting), paths);
                }
                else
                    Check(ConstructionIconCatalog.BuildResourcePath(category),
                        ConstructionIconCatalog.GetBuild(category), paths);
            }
            foreach (ConstructionCharacterIcon icon in Enum.GetValues(
                typeof(ConstructionCharacterIcon)))
                Check(ConstructionIconCatalog.CharacterResourcePath(icon),
                    ConstructionIconCatalog.GetCharacter(icon), paths);
            foreach (ConstructionStatusIcon icon in Enum.GetValues(
                typeof(ConstructionStatusIcon)))
                Check(ConstructionIconCatalog.StatusResourcePath(icon),
                    ConstructionIconCatalog.GetStatus(icon), paths);
            foreach (ConstructionControlIcon icon in Enum.GetValues(
                typeof(ConstructionControlIcon)))
                Check(ConstructionIconCatalog.ControlResourcePath(icon),
                    ConstructionIconCatalog.GetControl(icon), paths);
            Assert.That(paths.Count, Is.EqualTo(27));
        }

        private static void Check(string path, Sprite sprite, HashSet<string> paths)
        {
            Assert.That(paths.Add(path), Is.True, $"Duplicate icon: {path}");
            var texture = Resources.Load<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, $"Missing icon texture: {path}");
            Assert.That(sprite, Is.Not.Null, $"Missing icon sprite: {path}");
            Assert.That(sprite.texture, Is.SameAs(texture));
            Assert.That(sprite.rect.width, Is.LessThan(texture.width));
            Assert.That(sprite.rect.height, Is.LessThan(texture.height));
        }
    }
}
