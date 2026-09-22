using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class AnimalNeedVisualCatalogTests
    {
        [Test]
        public void EveryAnimalNeedVisualLoadsFromResources()
        {
            var paths = new HashSet<string>();
            foreach (AnimalNeedVisual visual in Enum.GetValues(typeof(AnimalNeedVisual)))
            {
                var path = AnimalNeedVisualCatalog.ResourcePath(visual);
                Assert.That(path, Is.Not.Empty);
                Assert.That(paths.Add(path), Is.True);
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null);
                Assert.That(AnimalNeedVisualCatalog.GetSprite(visual), Is.Not.Null);
            }
        }

        [Test]
        public void IndicatorOnlyShowsActiveNeedsForSelectedLivingAnimal()
        {
            var animal = new GameObject("Animal Need Test");
            try
            {
                var indicator = animal.AddComponent<AnimalNeedIndicator>();
                indicator.Initialize();

                Assert.That(indicator.IsVisible, Is.False);
                indicator.SetSelected(true);
                Assert.That(indicator.IsVisible, Is.False);

                indicator.SetNeeds(true, false, true);
                Assert.That(indicator.IsVisible, Is.True);
                Assert.That(indicator.VisibleCount, Is.EqualTo(2));

                indicator.SetAlive(false);
                Assert.That(indicator.IsVisible, Is.False);
                indicator.SetAlive(true);
                Assert.That(indicator.IsVisible, Is.True);

                indicator.SetNeeds(false, false, false);
                Assert.That(indicator.IsVisible, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(animal);
            }
        }
    }
}
