using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UnityEngine;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionCoexistenceIntegrationTests
    {
        [Test]
        public void FedWildlifeNeighbourhoodScoresForResidentAndSurvivesSaveReplay()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var meadow = Build(run, 4, 3, ConstructionCategory.Green);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var home = Build(run, 4, 2, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 5, 2, ConstructionCategory.Restaurant);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True,
                "The meadow can host pigeons only after its habitat matures.");

            var ecologyPreview = run.PreviewEcologyDay();
            var humanPreview = run.PreviewHumanDay();
            var person = humanPreview.residents.Single(resident =>
                resident.homeTileId == home.id);
            Assert.That(ecologyPreview.squirrelsFed, Is.GreaterThan(0));
            Assert.That(ecologyPreview.pigeonsFed, Is.GreaterThan(0));
            Assert.That(ecologyPreview.pigeonHabitatTileId,
                Is.EqualTo(meadow.id));
            Assert.That(person.completedCycle, Is.True);
            Assert.That(person.biodiversityBonus, Is.EqualTo(2));

            var before = run.HumanScore;
            Assert.That(run.TrySimulateConstructionDay(out var settled), Is.True);
            Assert.That(settled.pigeonHabitatTileId, Is.EqualTo(meadow.id));
            Assert.That(run.LastHumanDay.residents.Single().biodiversityBonus,
                Is.EqualTo(2));
            Assert.That(run.LastScoreDay.biodiversityHomes, Is.EqualTo(1));
            Assert.That(run.HumanScore - before,
                Is.EqualTo(humanPreview.SatisfactionPoints));
            Assert.That(run.LastScoreDay.linkagePoints,
                Is.EqualTo(run.LastHumanDay.LinkagePoints));

            var json = JsonUtility.ToJson(run.Export());
            Assert.That(ConstructionRunModel.TryRestore(
                JsonUtility.FromJson<ConstructionRunSaveData>(json),
                out var restored), Is.True);
            Assert.That(restored.LastScoreDay.biodiversityHomes,
                Is.EqualTo(1));
            Assert.That(restored.HumanScore, Is.EqualTo(run.HumanScore));
        }

        [Test]
        public void VisitedPlazaProtectsOnlyNeighbouringStreetInTomorrowPreview()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Square);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 4, 2, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            var street = Build(run, 3, 2, ConstructionCategory.Street);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.True);
            Build(run, 3, 1, ConstructionCategory.Restaurant);

            var human = run.PreviewHumanDay();
            Assert.That(human.residents.Single().visitedSquare, Is.True);
            Assert.That(run.IsStreetProtectedTomorrow(street.id), Is.True);
            Assert.That(ConstructionRoomIssueModel.ForTile(run, street,
                    human, run.PreviewEcologyDay(),
                    run.PreviewWasteAvailability()),
                Does.Contain(ConstructionRoomIssue.TrafficRisk),
                "A square halves the collision chance but does not remove it.");
            var forecast = run.PreviewEcologyDay();
            Assert.That(run.TrySimulateConstructionDay(out var settled), Is.True);
            Assert.That(settled.trafficSpecies, Is.EqualTo(forecast.trafficSpecies));
            Assert.That(settled.trafficStreetTileId,
                Is.EqualTo(forecast.trafficStreetTileId));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(), out _),
                Is.True);
        }

        [Test]
        public void ExistingVersionSevenRunKeepsItsOriginalWildlifeAndResidentRules()
        {
            var fresh = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(fresh.TrySimulateConstructionDay(out _), Is.True);
            var saved = fresh.Export();
            saved.score.scoringVersion = 7;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var oldRun),
                Is.True);
            Assert.That(oldRun.ScoringVersion, Is.EqualTo(7));

            Build(oldRun, 4, 3, ConstructionCategory.Green);
            Assert.That(oldRun.TrySimulateConstructionDay(out _), Is.True);
            Build(oldRun, 4, 2, ConstructionCategory.Residence);
            Assert.That(oldRun.TrySimulateConstructionDay(out _), Is.True);
            Build(oldRun, 5, 2, ConstructionCategory.Restaurant);
            var person = oldRun.PreviewHumanDay().residents.Single();
            Assert.That(person.completedCycle, Is.True);
            Assert.That(person.biodiversityBonus, Is.Zero);
            Assert.That(oldRun.TrySimulateConstructionDay(out _), Is.True);
            Assert.That(oldRun.LastScoreDay.biodiversityHomes, Is.Zero);
            Assert.That(ConstructionRunModel.TryRestore(oldRun.Export(),
                out var restored), Is.True);
            Assert.That(restored.ScoringVersion, Is.EqualTo(7));
        }

        private static ConstructionTileData Build(ConstructionRunModel run,
            int column, int row, ConstructionCategory category)
        {
            Assert.That(run.TryBuild(column, row, category, GreenPlanting.None,
                out var tile, out var failure, out var placement), Is.True,
                $"{category} at {column},{row}: {failure} / {placement}");
            return tile;
        }
    }
}
