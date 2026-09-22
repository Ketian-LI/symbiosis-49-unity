using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class AnimalSelectionVisualCatalogTests
    {
        [Test]
        public void EveryAnimalSelectionLayerLoadsFromResources()
        {
            var paths = new HashSet<string>();
            foreach (AnimalSelectionVisual visual in Enum.GetValues(typeof(AnimalSelectionVisual)))
            {
                var path = AnimalSelectionVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty);
                Assert.That(paths.Add(path), Is.True);
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null);
                Assert.That(AnimalSelectionVisualCatalog.GetSprite(visual), Is.Not.Null);
            }
        }

        [Test]
        public void FollowExitUsesAQuickFootprintFade()
        {
            Assert.That(PigeonDemoAgent.FootprintLifetimeSeconds, Is.EqualTo(6f));
            Assert.That(PigeonDemoAgent.FootprintExitFadeSeconds, Is.LessThan(0.5f));
        }
    }
}
