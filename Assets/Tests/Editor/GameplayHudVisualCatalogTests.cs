using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class GameplayHudVisualCatalogTests
    {
        [Test]
        public void EveryPopulationPortraitLoadsFromItsOwnResource()
        {
            foreach (WildlifeSpecies species in System.Enum.GetValues(typeof(WildlifeSpecies)))
            {
                var path = GameplayHudVisualCatalog.PopulationResourcePath(species);
                Assert.That(path, Is.Not.Empty);
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null, $"Missing population portrait for {species}.");
                Assert.That(GameplayHudVisualCatalog.GetPopulationSprite(species), Is.Not.Null);
            }
        }

        [Test]
        public void EveryEcologicalMetricHasAPictogram()
        {
            foreach (EcologicalMetricKind kind in System.Enum.GetValues(typeof(EcologicalMetricKind)))
            {
                var path = GameplayHudVisualCatalog.MetricResourcePath(kind);
                Assert.That(path, Is.Not.Empty);
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null, $"Missing ecological pictogram for {kind}.");
                Assert.That(GameplayHudVisualCatalog.GetMetricSprite(kind), Is.Not.Null);
            }
        }
    }
}
