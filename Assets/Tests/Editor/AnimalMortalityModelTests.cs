using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class AnimalMortalityModelTests
    {
        [Test]
        public void ThirdAnimalDeathReachesDefaultLimitRegardlessOfSpecies()
        {
            var model = new AnimalMortalityModel();

            Assert.That(model.RecordDeath(WildlifeSpecies.Pigeon, AnimalDeathCause.Traffic), Is.False);
            Assert.That(model.RecordDeath(WildlifeSpecies.Squirrel, AnimalDeathCause.Other), Is.False);
            Assert.That(model.RecordDeath(WildlifeSpecies.Hedgehog, AnimalDeathCause.Starvation), Is.True);

            Assert.That(model.TotalDeaths, Is.EqualTo(3));
            Assert.That(model.PigeonDeaths, Is.EqualTo(1));
            Assert.That(model.TrafficDeaths, Is.EqualTo(1));
            Assert.That(model.StarvationDeaths, Is.EqualTo(1));
        }

        [Test]
        public void RestoreClampsNegativeCountsAndResetReturnsToDefaultLimit()
        {
            var model = new AnimalMortalityModel();
            model.Restore(-1, 2, 1, 0, 1, 1, 9);

            Assert.That(model.TotalDeaths, Is.EqualTo(3));
            Assert.That(model.DeathLimit, Is.EqualTo(9));

            model.Reset();
            Assert.That(model.TotalDeaths, Is.Zero);
            Assert.That(model.DeathLimit, Is.EqualTo(3));
        }

        [Test]
        public void ResearchDeathLimitCanBeConfiguredBeforeDeaths()
        {
            var model = new AnimalMortalityModel();
            model.SetDeathLimit(2);

            Assert.That(model.RecordDeath(WildlifeSpecies.Pigeon, AnimalDeathCause.Other), Is.False);
            Assert.That(model.RecordDeath(WildlifeSpecies.Fox, AnimalDeathCause.Other), Is.True);
        }

        [Test]
        public void CountsEachCauseForTheCorrectSpeciesAndKeepsExportIndependent()
        {
            var model = new AnimalMortalityModel();
            model.SetDeathLimit(10);
            model.RecordDeath(WildlifeSpecies.Pigeon, AnimalDeathCause.Starvation);
            model.RecordDeath(WildlifeSpecies.Pigeon, AnimalDeathCause.Predation);
            model.RecordDeath(WildlifeSpecies.Squirrel, AnimalDeathCause.Traffic);
            model.RecordDeath(WildlifeSpecies.Fox, AnimalDeathCause.Starvation);

            Assert.That(model.BreakdownOf(WildlifeSpecies.Pigeon).starvation, Is.EqualTo(1));
            Assert.That(model.BreakdownOf(WildlifeSpecies.Pigeon).predation, Is.EqualTo(1));
            Assert.That(model.BreakdownOf(WildlifeSpecies.Squirrel).traffic, Is.EqualTo(1));
            Assert.That(model.BreakdownOf(WildlifeSpecies.Fox).starvation, Is.EqualTo(1));
            Assert.That(model.StarvationDeaths, Is.EqualTo(2));
            Assert.That(model.TrafficDeaths, Is.EqualTo(1));
            Assert.That(model.PredationDeaths, Is.EqualTo(1));

            var exported = model.ExportBreakdown();
            exported[0].starvation = 99;
            Assert.That(model.BreakdownOf(WildlifeSpecies.Pigeon).starvation, Is.EqualTo(1));
        }

        [Test]
        public void RestoringLegacyTotalsMarksPerSpeciesCausesAsUnrecorded()
        {
            var model = new AnimalMortalityModel();
            model.Restore(2, 1, 0, 0, 2, 1);

            Assert.That(model.BreakdownOf(WildlifeSpecies.Pigeon).unrecorded, Is.EqualTo(2));
            Assert.That(model.BreakdownOf(WildlifeSpecies.Squirrel).unrecorded, Is.EqualTo(1));
            Assert.That(model.BreakdownOf(WildlifeSpecies.Pigeon).starvation, Is.Zero);
            Assert.That(model.StarvationDeaths, Is.EqualTo(2));
            Assert.That(model.TrafficDeaths, Is.EqualTo(1));
        }

        [Test]
        public void RestoredBreakdownAndNewDeathsRemainConsistent()
        {
            var model = new AnimalMortalityModel();
            model.Restore(2, 1, 0, 0, 1, 0, 6,
                new[]
                {
                    new AnimalDeathBreakdownData
                    {
                        species = WildlifeSpecies.Pigeon,
                        starvation = 1,
                        predation = 1
                    }
                });

            Assert.That(model.BreakdownOf(WildlifeSpecies.Pigeon).Total, Is.EqualTo(2));
            Assert.That(model.PredationDeaths, Is.EqualTo(1));
            Assert.That(model.BreakdownOf(WildlifeSpecies.Squirrel).unrecorded, Is.EqualTo(1));
            model.RecordDeath(WildlifeSpecies.Squirrel, AnimalDeathCause.Traffic);
            Assert.That(model.BreakdownOf(WildlifeSpecies.Squirrel).Total, Is.EqualTo(2));
            Assert.That(model.BreakdownOf(WildlifeSpecies.Squirrel).traffic, Is.EqualTo(1));
            Assert.That(model.TotalDeaths, Is.EqualTo(4));
        }

        [Test]
        public void SpeciesCauseBreakdownSurvivesSessionJsonRoundTrip()
        {
            var original = new AnimalMortalityModel();
            original.RecordDeath(WildlifeSpecies.Pigeon, AnimalDeathCause.Predation);
            var save = new SessionSaveData
            {
                pigeonDeaths = original.PigeonDeaths,
                animalDeathBreakdown = original.ExportBreakdown()
            };

            var loaded = JsonUtility.FromJson<SessionSaveData>(JsonUtility.ToJson(save));
            var restored = new AnimalMortalityModel();
            restored.Restore(loaded.pigeonDeaths, loaded.squirrelDeaths,
                loaded.hedgehogDeaths, loaded.foxDeaths, loaded.starvationDeaths,
                loaded.trafficDeaths, savedBreakdown: loaded.animalDeathBreakdown);

            Assert.That(restored.BreakdownOf(WildlifeSpecies.Pigeon).predation, Is.EqualTo(1));
            Assert.That(restored.BreakdownOf(WildlifeSpecies.Pigeon).unrecorded, Is.Zero);
        }
    }
}
