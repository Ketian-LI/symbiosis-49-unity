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

        [Test]
        public void DayNightIllustrationLoadsSeparatelyFromTheDynamicDial()
        {
            Assert.That(Resources.Load<Texture2D>("UI/GameplayHud/day-night-dial-v02"), Is.Not.Null);
            Assert.That(GameplayHudVisualCatalog.GetDayNightDialSprite(), Is.Not.Null);
        }

        [Test]
        public void GameplayTabletopBackgroundResourceLoads()
        {
            Assert.That(Resources.Load<Texture2D>("UI/GameplayHud/gameplay-tabletop-backdrop-v01"), Is.Not.Null);
        }

        [Test]
        public void MealRingTracksActualFedAnimalsRatherThanMapStock()
        {
            var noneFed = new AnimalMealProgress(12, 12);
            Assert.That(noneFed.Badge, Is.EqualTo("0/12"));
            Assert.That(noneFed.Fraction, Is.Zero);

            var partlyFed = new AnimalMealProgress(12, 5);
            Assert.That(partlyFed.Badge, Is.EqualTo("7/12"));
            Assert.That(partlyFed.Fraction, Is.EqualTo(7f / 12f).Within(0.001f));

            var noLivingAnimals = new AnimalMealProgress(0, 4);
            Assert.That(noLivingAnimals.Badge, Is.EqualTo("0/0"));
            Assert.That(noLivingAnimals.Fraction, Is.Zero);
        }

        [Test]
        public void PopulationFrameReflectsLivingShareWithoutBakingInTheCount()
        {
            var badge = new GameObject("Population Badge Test", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(AnimalPopulationBadgeGraphic));
            try
            {
                var graphic = badge.GetComponent<AnimalPopulationBadgeGraphic>();
                graphic.SetPopulation(Color.blue, 3, 4);
                Assert.That(graphic.LivingFraction, Is.EqualTo(0.75f));
                graphic.SetPopulation(Color.blue, 0, 4);
                Assert.That(graphic.LivingFraction, Is.Zero);
                graphic.SetPopulation(Color.cyan, 0, 4, 0.5f);
                Assert.That(graphic.ArcFraction, Is.EqualTo(0.4f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(badge);
            }
        }
    }
}
