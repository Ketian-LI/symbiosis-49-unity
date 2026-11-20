using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Tests.PlayMode
{
    public sealed class ConstructionLocalLeaderboardPresentationTests
    {
        [UnityTest]
        public IEnumerator FinishedRunIsRecordedAndRankingsOpenInsideTheGame()
        {
            var directory = Path.Combine(Application.temporaryCachePath,
                "symbiosis49-rank-presentation-" + Guid.NewGuid().ToString("N"));
            var save = new ConstructionRunStore(Path.Combine(directory, "run.json"));
            var ranks = new ConstructionLocalLeaderboardStore(
                Path.Combine(directory, "ranks.json"));
            var run = FinishedEndlessRun();
            var root = new GameObject("Local ranking presentation test");
            var controller = root.AddComponent<ConstructionPrototypeController>();
            controller.UseRunForTesting(run, save, ranks);
            yield return null;

            Assert.That(ranks.RankingsFor(ConstructionGameMode.Endless,
                out var entries), Is.EqualTo(ConstructionLocalLeaderboardStatus.Loaded));
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].score.runId, Is.EqualTo(run.RunId));
            Assert.That(entries[0].score.totalPoints,
                Is.EqualTo(run.OverallScore));
            var result = root.transform.Find(
                "Construction UI/Construction final result/Result card");
            Assert.That(result.Find("Leaderboard status").GetComponent<Text>().text,
                Does.Contain("Saved on this device"));
            result.Find("Open local rankings").GetComponent<Button>()
                .onClick.Invoke();
            var overlay = root.transform.Find(
                "Construction UI/Local rankings overlay");
            Assert.That(overlay.gameObject.activeSelf, Is.True);
            var card = overlay.Find("Local rankings card");
            Assert.That(card.Find("Rank 1 column 1").GetComponent<Text>().text,
                Is.EqualTo("Player"));
            Assert.That(card.Find("Rank 1 column 2").GetComponent<Text>().text,
                Is.EqualTo(run.OverallScore.ToString()));
            Assert.That(card.Find("Rank 2 column 1").gameObject.activeSelf,
                Is.False);
            card.Find("Close local rankings").GetComponent<Button>()
                .onClick.Invoke();
            Assert.That(overlay.gameObject.activeSelf, Is.False);

            UnityEngine.Object.Destroy(root);
            yield return null;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        private static ConstructionRunModel FinishedEndlessRun()
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
            return run;
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
