using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionLeaderboardPayloadTests
    {
        [Test]
        public void RunIdSurvivesSaveAndRestore()
        {
            var original = new ConstructionRunModel();
            Assert.That(ConstructionRunModel.TryRestore(original.Export(),
                out var restored), Is.True);
            Assert.That(restored.RunId, Is.EqualTo(original.RunId));
            Assert.That(new ConstructionRunModel().RunId,
                Is.Not.EqualTo(original.RunId));
        }

        [Test]
        public void LegacySaveGetsIdButCannotSubmitBeforeFinishing()
        {
            var oldSave = new ConstructionRunModel().Export();
            oldSave.runId = null;
            Assert.That(ConstructionRunModel.TryRestore(oldSave,
                out var restored), Is.True);
            Assert.That(System.Guid.TryParse(restored.RunId, out _), Is.True);
            Assert.That(ConstructionLeaderboardPayload.TryCreate(restored,
                "Player", out _), Is.False);
        }

        [Test]
        public void FinishedEndlessRunSerializesScoredResult()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.ConfigureEndlessLossRules(20, 1), Is.True);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 4, 3, ConstructionCategory.Residence);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 5, 3, ConstructionCategory.Workshop);
            Assert.That(run.TrySimulateDay(out _), Is.True);
            Build(run, 3, 4, ConstructionCategory.Residence);
            for (var day = 0; day < 3; day++)
                Assert.That(run.TrySimulateDay(out _), Is.True);

            Assert.That(run.IsFinished, Is.True);
            Assert.That(run.EndReason,
                Is.EqualTo(ConstructionEndReason.ResidentsUnemployed));
            Assert.That(ConstructionLeaderboardPayload.TryCreate(run,
                "Tester", out var payload), Is.True);
            var serialized = JsonUtility.FromJson<ConstructionLeaderboardPayload>(
                payload.ToJson());
            Assert.That(serialized.runId, Is.EqualTo(run.RunId));
            Assert.That(serialized.nickname, Is.EqualTo("Tester"));
            Assert.That(serialized.mode, Is.EqualTo("Endless"));
            Assert.That(serialized.endReason,
                Is.EqualTo("ResidentsUnemployed"));
            Assert.That(serialized.scoringVersion,
                Is.EqualTo(ConstructionScoreLedger.CurrentScoringVersion));
            Assert.That(serialized.daysPlayed,
                Is.EqualTo(run.FinalResult.DaysPlayed));
            Assert.That(serialized.linkageTrackedDays,
                Is.EqualTo(serialized.daysPlayed));
            Assert.That(serialized.totalPoints,
                Is.EqualTo(serialized.animalPoints +
                    serialized.residentPoints + serialized.buildPoints +
                    serialized.goalPoints));
        }

        private static void Build(ConstructionRunModel run, int column,
            int row, ConstructionCategory category)
        {
            Assert.That(run.TryBuild(column, row, category,
                GreenPlanting.None, out _, out var failure,
                out var placement), Is.True,
                $"{category} at ({column}, {row}): {failure} / {placement}");
        }
    }
}
