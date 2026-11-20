using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionGameplayRevisionTests
    {
        [Test]
        public void SkippingFillTutorialUnlocksFreePlayAndSurvivesSave()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            Assert.That(run.IsIntroPhase, Is.True);
            Assert.That(run.SkipFillTutorial(), Is.True);
            Assert.That(run.IsIntroPhase, Is.False);
            Assert.That(run.FillTutorialSkipBuiltCount, Is.Zero);
            Assert.That(run.FillHandCards.Count, Is.EqualTo(3));
            Assert.That(run.FillHandCards,
                Does.Contain(ConstructionCategory.Green));
            Assert.That(run.UsesBasicFillResources, Is.False);
            Assert.That(run.IsFillHandCardPlayable(ConstructionCategory.Green),
                Is.True);
            Assert.That(run.CanRerollFillHandCard(ConstructionCategory.Green),
                Is.False, "The only valid opening oak cannot be swapped away.");
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.FillTutorialSkipped, Is.True);
            Assert.That(restored.FillHandCards, Is.EqualTo(run.FillHandCards));
        }

        [Test]
        public void GreenUsesTheSharedHandAndDeck()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            Assert.That(run.SkipFillTutorial(), Is.True);
            Assert.That(run.FillHandCards,
                Does.Contain(ConstructionCategory.Green));
            var deckBefore = run.FillDeckCardsRemaining;
            Assert.That(deckBefore, Is.EqualTo(97));
            Assert.That(run.FillHandStartingRerolls, Is.EqualTo(2));
            Assert.That(run.TryBuild(3, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.FillDeckCardsRemaining, Is.EqualTo(deckBefore - 1));
            Assert.That(run.FillHandCards.Count, Is.EqualTo(run.FillHandSize));
            Assert.That(run.IsFillHandCardPlayable(ConstructionCategory.Green),
                Is.EqualTo(run.FillHandCards.Contains(ConstructionCategory.Green)));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.FillDeckCardsRemaining,
                Is.EqualTo(run.FillDeckCardsRemaining));
            Assert.That(restored.FillHandCards, Is.EqualTo(run.FillHandCards));
        }

        [TestCase(ConstructionCategory.Green)]
        [TestCase(ConstructionCategory.Street)]
        [TestCase(ConstructionCategory.Waste)]
        public void FormerResourceTypesCanBePlayedAsHandCards(
            ConstructionCategory category)
        {
            var saved = CompletedOpening().Export();
            saved.fillHandCards = new List<ConstructionCategory>
                { ConstructionCategory.Green, ConstructionCategory.Street,
                    ConstructionCategory.Waste };
            Assert.That(ConstructionRunModel.TryRestore(saved, out var run),
                Is.True);
            Assert.That(run.UsesBasicFillResources, Is.False);
            Assert.That(run.IsFillHandCardPlayable(category), Is.True);
            Assert.That(run.IsFillHandCardPlayable(ConstructionCategory.Residence),
                Is.False, "There must be no off-hand shortcut.");
            var plot = Enumerable.Range(0, 7).SelectMany(row =>
                Enumerable.Range(0, 7).Select(column => (column, row)))
                .First(point => run.CheckPlacement(point.column, point.row,
                    category) == ConstructionPlacementFailure.None);
            var deckBefore = run.FillDeckCardsRemaining;
            Assert.That(run.TryBuild(plot.column, plot.row, category,
                GreenPlanting.None, out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.FillDeckCardsRemaining, Is.EqualTo(deckBefore - 1));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.FillHandCards, Is.EqualTo(run.FillHandCards));
        }

        [Test]
        public void ResourceButtonSaveJoinsSharedDeckWithoutResettingProgress()
        {
            var saved = CompletedOpening().Export();
            saved.fillHandDeckVersion = 2;
            saved.fillHandCards = new List<ConstructionCategory>
                { ConstructionCategory.Residence, ConstructionCategory.Restaurant,
                    ConstructionCategory.Workshop };
            saved.fillHandRerollsSpent = 1;
            var original = JsonUtility.ToJson(saved);
            Assert.That(ConstructionRunModel.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(restored.UsesBasicFillResources, Is.False);
            Assert.That(restored.Export().fillHandDeckVersion, Is.EqualTo(3));
            Assert.That(restored.RunId, Is.EqualTo(saved.runId));
            Assert.That(restored.BuiltCount, Is.EqualTo(8));
            Assert.That(restored.CurrentDay, Is.EqualTo(saved.currentDay));
            Assert.That(restored.FillHandCards, Is.EqualTo(saved.fillHandCards));
            Assert.That(restored.FillDeckCardsRemaining, Is.EqualTo(89));
            Assert.That(restored.Export().fillHandRerollsSpent, Is.EqualTo(1));
            Assert.That(restored.Export().fillHandFedSpeciesMask,
                Is.EqualTo(saved.fillHandFedSpeciesMask));
            Assert.That(JsonUtility.ToJson(restored.Export().score),
                Is.EqualTo(JsonUtility.ToJson(saved.score)));
            Assert.That(JsonUtility.ToJson(saved), Is.EqualTo(original),
                "Migration must not modify the source save used by previews.");
            Assert.That(ConstructionRunModel.TryRestore(restored.Export(),
                out var reloaded), Is.True);
            Assert.That(reloaded.FillHandCards, Is.EqualTo(restored.FillHandCards));
        }

        [Test]
        public void SkippedEmptyResourceSaveReceivesAnOpeningOakInTheHand()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            Assert.That(run.SkipFillTutorial(), Is.True);
            var saved = run.Export();
            saved.fillHandDeckVersion = 2;
            saved.fillHandCards = new List<ConstructionCategory>
                { ConstructionCategory.Residence, ConstructionCategory.Restaurant,
                    ConstructionCategory.Workshop };
            Assert.That(ConstructionRunModel.TryRestore(saved, out var restored),
                Is.True);
            Assert.That(restored.FillHandCards,
                Does.Contain(ConstructionCategory.Green));
            Assert.That(restored.TryBuild(3, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(restored.TrySimulateFillDay(out _), Is.True);
        }

        private static ConstructionRunModel CompletedOpening()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Fill,
                useFillHand: true);
            foreach (var card in new (ConstructionCategory category, int x, int y)[]
                     {
                         (ConstructionCategory.Green, 3, 3),
                         (ConstructionCategory.Residence, 4, 3),
                         (ConstructionCategory.Restaurant, 5, 3),
                         (ConstructionCategory.Workshop, 4, 4),
                         (ConstructionCategory.Street, 4, 2),
                         (ConstructionCategory.Waste, 6, 3),
                         (ConstructionCategory.Supermarket, 3, 2),
                         (ConstructionCategory.Square, 4, 1)
                     })
            {
                Assert.That(run.TryBuild(card.x, card.y, card.category,
                    card.category == ConstructionCategory.Green
                        ? GreenPlanting.Oak : GreenPlanting.None,
                    out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
            }
            return run;
        }

        [Test]
        public void EndlessDayRequiresAPlacement()
        {
            var run = new ConstructionRunModel(ConstructionGameMode.Endless);
            Assert.That(run.CanAdvanceConstructionDay, Is.False);
            Assert.That(run.TrySimulateConstructionDay(out _), Is.False);
            Assert.That(run.TryBuild(3, 4, ConstructionCategory.Green,
                GreenPlanting.Meadow, out _, out _, out _), Is.True);
            Assert.That(run.CanAdvanceConstructionDay, Is.True);
        }
    }
}
