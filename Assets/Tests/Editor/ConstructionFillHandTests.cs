using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ConstructionFillHandTests
    {
        private static readonly (ConstructionCategory category, int column,
            int row)[] Opening =
        {
            (ConstructionCategory.Green, 3, 3),
            (ConstructionCategory.Residence, 4, 3),
            (ConstructionCategory.Restaurant, 5, 3),
            (ConstructionCategory.Workshop, 4, 4),
            (ConstructionCategory.Street, 4, 2),
            (ConstructionCategory.Waste, 6, 3),
            (ConstructionCategory.Supermarket, 3, 2),
            (ConstructionCategory.Square, 4, 1)
        };

        [Test]
        public void OpeningShowsThreeThreeTwoAndOnlyNextCardCanBeBuilt()
        {
            var run = NewHandRun();
            Assert.That(run.BuiltCount, Is.Zero,
                "The player must place the first Central Oak card.");
            Assert.That(run.At(3, 3), Is.Null);
            Assert.That(run.BalanceScore, Is.Zero);
            Assert.That(run.CurrentDay, Is.EqualTo(1));
            Assert.That(run.TrySimulateFillDay(out _), Is.False,
                "The supplied oak must not award a placement settlement.");

            for (var index = 0; index < Opening.Length; index++)
            {
                var start = index < 3 ? 0 : index < 6 ? 3 : 6;
                var size = index < 6 ? 3 : 2;
                Assert.That(run.FillHandCards,
                    Is.EqualTo(Opening.Skip(start).Take(size)
                        .Select(item => item.category)),
                    $"Visible tutorial batch at card {index + 1}");
                Assert.That(run.FillHandActiveIndex, Is.EqualTo(index - start));
                Assert.That(run.IsFillHandCardPlayable(Opening[index].category),
                    Is.True);
                foreach (var other in run.FillHandCards.Where(category =>
                             category != Opening[index].category))
                    Assert.That(run.IsFillHandCardPlayable(other), Is.False,
                        "A future card can be seen but not played early.");

                var (category, column, row) = Opening[index];
                var planting = index == 0 ? GreenPlanting.Oak : GreenPlanting.None;
                Assert.That(run.CheckPlacement(column, row, category, planting),
                    Is.EqualTo(ConstructionPlacementFailure.None));
                Assert.That(run.TryBuild(column, row, category,
                    planting, out var built, out var failure,
                    out var placement), Is.True,
                    $"Card {index + 1}: {category} {failure}/{placement}");
                if (index == 0)
                    Assert.That(built.id,
                        Is.EqualTo(ConstructionBoardModel.StarterOakId));
                Assert.That(run.TrySimulateFillDay(out _), Is.True,
                    "Every confirmed placement must settle once.");
                Assert.That(run.CurrentDay, Is.EqualTo(index + 2));
                Assert.That(run.BuiltCount, Is.EqualTo(index + 1));
            }

            Assert.That(run.IsIntroPhase, Is.False);
            Assert.That(run.FillHandActiveIndex, Is.EqualTo(-1));
            Assert.That(run.FillHandCards.Count, Is.EqualTo(run.FillHandSize));
            Assert.That(run.FillHandCards.Distinct().Count(),
                Is.EqualTo(run.FillHandSize));
        }

        [Test]
        public void InvalidTutorialBuildDoesNotConsumeTheCurrentCard()
        {
            var run = NewHandRun();
            var before = run.FillHandCards.ToArray();
            Assert.That(run.TryBuild(3, 3, ConstructionCategory.Residence,
                GreenPlanting.None, out _, out _, out _), Is.False);
            Assert.That(run.FillHandCards, Is.EqualTo(before));
            Assert.That(run.FillHandActiveIndex, Is.Zero);
            Assert.That(run.BuiltCount, Is.Zero);
            Assert.That(run.CurrentDay, Is.EqualTo(1));
        }

        [Test]
        public void FirstOakCardMustBePlacedAtTheCenter()
        {
            var run = NewHandRun();
            Assert.That(run.CheckPlacement(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak), Is.Not.EqualTo(
                    ConstructionPlacementFailure.None));
            Assert.That(run.TryBuild(4, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.False);
            Assert.That(run.BuiltCount, Is.Zero);
            Assert.That(run.FillHandActiveIndex, Is.Zero);
        }

        [Test]
        public void EmptyOpeningBoardSurvivesSaveAndRestore()
        {
            var run = NewHandRun();
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.UsesFillHand, Is.True);
            Assert.That(restored.BuiltCount, Is.Zero);
            Assert.That(restored.At(3, 3), Is.Null);
            Assert.That(restored.FillHandCards,
                Is.EqualTo(new[]
                {
                    ConstructionCategory.Green,
                    ConstructionCategory.Residence,
                    ConstructionCategory.Restaurant
                }));
            Assert.That(restored.CheckPlacement(3, 3,
                ConstructionCategory.Green, GreenPlanting.Oak),
                Is.EqualTo(ConstructionPlacementFailure.None));
            Assert.That(restored.TryBuild(3, 3,
                ConstructionCategory.Green, GreenPlanting.Oak,
                out var oak, out var failure, out var reason), Is.True,
                $"{failure}/{reason}");
            Assert.That(oak.id,
                Is.EqualTo(ConstructionBoardModel.StarterOakId));
        }

        [Test]
        public void RandomHandPersistsAndRefillsDeterministically()
        {
            var run = CompleteOpening();
            var cards = run.FillHandCards.ToArray();
            Assert.That(run.UsesFiniteFillDeck, Is.True);
            Assert.That(run.FillDeckCardsRemaining, Is.EqualTo(89),
                "Eight tutorial plots and three held cards leave 89 in the shared deck.");
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.UsesFillHand, Is.True);
            Assert.That(restored.FillHandCards, Is.EqualTo(cards));
            Assert.That(restored.FillHandActiveIndex, Is.EqualTo(-1));
            Assert.That(restored.CategoryLimit(ConstructionCategory.Green),
                Is.EqualTo(30));

            var placement = FindPlayablePlacement(run);
            Assert.That(placement.HasValue, Is.True,
                "At least one of the three random cards should be buildable.");
            var (category, column, row) = placement.Value;
            foreach (var instance in new[] { run, restored })
            {
                Assert.That(instance.TryBuild(column, row, category,
                    GreenPlanting.None, out _, out var failure,
                    out var reason), Is.True,
                    $"{category} {failure}/{reason}");
                Assert.That(instance.TrySimulateFillDay(out _), Is.True);
            }

            Assert.That(run.FillHandCards, Is.EqualTo(restored.FillHandCards),
                "Restore must preserve the next random draw, not only the visible hand.");
            Assert.That(run.FillHandCards.Count, Is.EqualTo(run.FillHandSize));
            Assert.That(run.FillHandCards.Distinct().Count(),
                Is.EqualTo(run.FillHandSize));
            Assert.That(run.CurrentDay, Is.EqualTo(10));
            Assert.That(run.FillDeckCardsRemaining,
                Is.EqualTo(100 - run.BuiltTiles.Count -
                    run.FillHandCards.Count));
            Assert.That(restored.FillDeckCardsRemaining,
                Is.EqualTo(run.FillDeckCardsRemaining));
        }

        [Test]
        public void DigitalSupplyHasOneHundredCardsAndOldHandSavesStayUnlimited()
        {
            var categories = Enum.GetValues(typeof(ConstructionCategory))
                .Cast<ConstructionCategory>().ToArray();
            Assert.That(categories.Sum(ConstructionRunModel.DigitalFillCardSupply),
                Is.EqualTo(100));
            Assert.That(ConstructionRunModel.DigitalFillCardSupply(
                ConstructionCategory.Green), Is.EqualTo(30));
            Assert.That(ConstructionRunModel.DigitalFillCardSupply(
                ConstructionCategory.Residence), Is.EqualTo(18));
            var saved = NewHandRun().Export();
            saved.fillHandDeckVersion = 0;
            saved.greenPatchRulesVersion = 0;
            saved.fillHandWitnessedLinkage = false;
            saved.fillHandExpanded = false;
            saved.fillHandFedSpeciesMask = 0;
            saved.fillHandRerollsSpent = 0;
            saved.score.scoringVersion = 9;
            saved.specialTasks = null;
            saved.placementScore = null;
            Assert.That(ConstructionRunModel.TryRestore(saved, out var oldRun),
                Is.True);
            Assert.That(oldRun.UsesFiniteFillDeck, Is.False);
            Assert.That(oldRun.CategoryLimit(ConstructionCategory.Green),
                Is.EqualTo(int.MaxValue));
            Assert.That(oldRun.FillDeckCardsRemaining, Is.EqualTo(-1));
            Assert.That(oldRun.FillHandCards,
                Is.EqualTo(new[] { ConstructionCategory.Green,
                    ConstructionCategory.Residence,
                    ConstructionCategory.Restaurant }));
        }

        [Test]
        public void FreshHandRunScoresObservedOakMealAndRestoresItsBreakdown()
        {
            var run = NewHandRun();
            Assert.That(run.HasFedAnimalScore, Is.True);
            Assert.That(run.TryBuild(3, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            Assert.That(run.PreviewEcologyDay().animalPoints, Is.EqualTo(5));
            Assert.That(run.TrySimulateFillDay(out var ecology), Is.True);
            Assert.That(ecology.AnimalMeals, Is.EqualTo(1));
            Assert.That(run.AnimalScore, Is.EqualTo(5));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.AnimalScore, Is.EqualTo(5));

            var tampered = run.Export();
            tampered.lastEcologyDay.animalPoints = 8;
            Assert.That(ConstructionRunModel.TryRestore(tampered, out _),
                Is.False, "A saved score must match observed feeding.");
        }

        [Test]
        public void BlankPreviousHandSaveKeepsItsOriginalScoringRules()
        {
            var saved = NewHandRun().Export();
            saved.score.scoringVersion = 9;
            saved.fillHandDeckVersion = 1;
            saved.specialTasks = null;
            saved.placementScore = null;
            Assert.That(saved.score.days, Is.Empty);
            Assert.That(ConstructionRunModel.TryRestore(saved, out var run),
                Is.True);
            Assert.That(run.HasFedAnimalScore, Is.False);
            Assert.That(run.TryBuild(3, 3, ConstructionCategory.Green,
                GreenPlanting.Oak, out _, out _, out _), Is.True);
            var expected = run.PreviewEcologyDay().animalPoints;
            Assert.That(run.TrySimulateFillDay(out _), Is.True);
            Assert.That(run.AnimalScore, Is.EqualTo(expected));
        }

        [Test]
        public void FirstAnimalMealsGrantFiniteSwapsWithoutChangingDeckSize()
        {
            var run = NewHandRun();
            Assert.That(run.FillHandRerollsRemaining, Is.EqualTo(2));
            Assert.That(run.TryRerollFillHandCard(ConstructionCategory.Green,
                out _), Is.False, "The fixed opening cannot be rerolled.");
            var (category, column, row) = Opening[0];
            Assert.That(run.TryBuild(column, row, category, GreenPlanting.Oak,
                out _, out _, out _), Is.True);
            Assert.That(run.TrySimulateFillDay(out var ecology), Is.True);
            Assert.That(ecology.squirrelsFed, Is.GreaterThan(0));
            Assert.That(run.FillHandRerollsRemaining, Is.EqualTo(3));

            foreach (var next in Opening.Skip(1))
            {
                Assert.That(run.TryBuild(next.column, next.row, next.category,
                    GreenPlanting.None, out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
            }
            // Each species' first meal adds one banked opportunity. Repeating
            // the same meals must not silently spend or duplicate it.
            var hook = typeof(ConstructionRunModel).GetMethod(
                "AwardFillHandProgress", BindingFlags.Instance |
                BindingFlags.NonPublic);
            Assert.That(hook, Is.Not.Null);
            var meals = new ConstructionEcologyDayResult
                { pigeonsFed = 1, hedgehogFed = true };
            var people = new ConstructionHumanDayResult();
            hook.Invoke(run, new object[] { meals, people });
            hook.Invoke(run, new object[] { meals, people });
            Assert.That(run.FillHandRerollsRemaining, Is.EqualTo(5));
            var before = run.FillHandCards.ToArray();
            var target = before.First(run.CanRerollFillHandCard);
            var deck = run.FillDeckCardsRemaining;
            Assert.That(run.TryRerollFillHandCard(target, out var replacement),
                Is.True);
            Assert.That(replacement, Is.Not.EqualTo(target));
            Assert.That(run.FillHandCards.Count, Is.EqualTo(3));
            Assert.That(run.FillHandCards.Distinct().Count(), Is.EqualTo(3));
            Assert.That(run.FillDeckCardsRemaining, Is.EqualTo(deck));
            Assert.That(run.FillHandRerollsRemaining, Is.EqualTo(4));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.FillHandCards, Is.EqualTo(run.FillHandCards));
            Assert.That(restored.FillHandRerollsRemaining, Is.EqualTo(4));
            for (var remaining = 3; remaining >= 0; remaining--)
            {
                var chosen = restored.FillHandCards.First(
                    restored.CanRerollFillHandCard);
                Assert.That(restored.TryRerollFillHandCard(chosen, out _),
                    Is.True);
                Assert.That(restored.FillHandRerollsRemaining,
                    Is.EqualTo(remaining));
                Assert.That(restored.FillDeckCardsRemaining, Is.EqualTo(deck));
            }
            Assert.That(restored.TryRerollFillHandCard(
                restored.FillHandCards[0], out _), Is.False);
        }

        [Test]
        public void ThreeAndSixSquaresUnlockFourthAndFifthCards()
        {
            var run = CompleteOpening();
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Square),
                Is.EqualTo(1));
            Assert.That(run.FillHandSize, Is.EqualTo(3));
            run = BuildSquaresUntil(run, 2);
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Square),
                Is.EqualTo(2));
            Assert.That(run.FillHandSize, Is.EqualTo(3));
            run = BuildSquaresUntil(run, 3);
            Assert.That(run.FillHandSize, Is.EqualTo(4));
            Assert.That(run.FillHandCards.Count, Is.EqualTo(4));
            Assert.That(run.FillHandCards.Distinct().Count(), Is.EqualTo(4));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out var restored), Is.True);
            Assert.That(restored.FillHandSize, Is.EqualTo(4));
            Assert.That(restored.FillHandCards,
                Is.EqualTo(run.FillHandCards));
            run = BuildSquaresUntil(run, 5);
            Assert.That(run.FillHandSize, Is.EqualTo(4));
            run = BuildSquaresUntil(run, 6);
            Assert.That(run.CategoryBuiltCount(ConstructionCategory.Square),
                Is.EqualTo(6));
            Assert.That(run.FillHandSize, Is.EqualTo(5));
            Assert.That(run.FillHandCards.Count, Is.EqualTo(5));
            Assert.That(run.FillHandCards.Distinct().Count(), Is.EqualTo(5));
            Assert.That(ConstructionRunModel.TryRestore(run.Export(),
                out restored), Is.True);
            Assert.That(restored.FillHandCards, Is.EqualTo(run.FillHandCards));
        }

        [Test]
        public void OldTwoAndFourSquareSavesKeepLoadingAtNewHandSizes()
        {
            var run = BuildSquaresUntil(CompleteOpening(), 2);
            var oldFourCardSave = AddLegacyExtraCard(run, fifth: false);
            Assert.That(ConstructionRunModel.TryRestore(oldFourCardSave,
                out var migrated), Is.True);
            Assert.That(migrated.FillHandSize, Is.EqualTo(3));
            Assert.That(migrated.FillHandCards,
                Is.EqualTo(run.FillHandCards));

            run = BuildSquaresUntil(run, 4);
            var oldFiveCardSave = AddLegacyExtraCard(run, fifth: true);
            Assert.That(ConstructionRunModel.TryRestore(oldFiveCardSave,
                out migrated), Is.True);
            Assert.That(migrated.FillHandSize, Is.EqualTo(4));
            Assert.That(migrated.FillHandCards,
                Is.EqualTo(run.FillHandCards));
        }

        [Test]
        public void FiniteDeckCanFillTheBoardAcrossDeterministicDraws()
        {
            var started = CompleteOpening();
            Assert.That(started.TryConfirmSpecialTasks(), Is.True);
            var opening = started.Export();
            var categories = Enum.GetValues(typeof(ConstructionCategory))
                .Cast<ConstructionCategory>().ToArray();
            for (var seed = 1; seed <= 12; seed++)
            {
                opening.fillHandRandomState = seed;
                Assert.That(ConstructionRunModel.TryRestore(opening,
                    out var run), Is.True);
                while (!run.IsFinished)
                {
                    var next = FindPlayablePlacement(run);
                    Assert.That(next.HasValue, Is.True,
                        $"Seed {seed} stalled at {run.BuiltCount}/49.");
                    var (category, column, row) = next.Value;
                    Assert.That(run.TryBuild(column, row, category,
                        GreenPlanting.None, out _, out _, out _), Is.True);
                    Assert.That(run.TrySimulateFillDay(out _), Is.True);
                    foreach (var kind in categories)
                        Assert.That(run.CategoryBuiltCount(kind) +
                            run.FillHandCards.Count(card => card == kind),
                            Is.LessThanOrEqualTo(
                                ConstructionRunModel.DigitalFillCardSupply(kind)));
                }
                Assert.That(run.BuiltCount, Is.EqualTo(49));
                var exported = run.Export();
                var loadable = ConstructionRunModel.TryRestore(exported, out var completed);
                if (!loadable)
                {
                    var debugPath = Environment.GetEnvironmentVariable("SYMBIOSIS_SAVE_DIAGNOSTIC_PATH");
                    if (!string.IsNullOrEmpty(debugPath))
                        File.WriteAllText(debugPath, UnityEngine.JsonUtility.ToJson(exported, true));
                    exported.awardRulesVersion = 0;
                    TestContext.WriteLine("Same save without award metadata loads: " +
                        ConstructionRunModel.TryRestore(exported, out _));
                }
                Assert.That(loadable, Is.True,
                    $"Seed {seed}: a completed board must remain loadable.");
                Assert.That(completed.OverallScore, Is.EqualTo(run.OverallScore));
            }
        }

        [Test]
        public void OtherModesDoNotReceiveTheDigitalFillHand()
        {
            Assert.That(new ConstructionRunModel(ConstructionGameMode.Endless)
                .UsesFillHand, Is.False);
            Assert.That(new ConstructionRunModel(ConstructionGameMode.BoardGame)
                .UsesFillHand, Is.False);
            Assert.That(new ConstructionRunModel(ConstructionGameMode.Fill)
                .UsesFillHand, Is.False,
                "Existing Fill saves must keep their prior rules until migrated.");
        }

        private static ConstructionRunModel NewHandRun() =>
            new(ConstructionGameMode.Fill, useFillHand: true);

        private static ConstructionRunModel CompleteOpening()
        {
            var run = NewHandRun();
            foreach (var (category, column, row) in Opening)
            {
                var planting = category == ConstructionCategory.Green
                    ? GreenPlanting.Oak : GreenPlanting.None;
                Assert.That(run.TryBuild(column, row, category,
                    planting, out _, out var failure,
                    out var reason), Is.True,
                    $"{category} {failure}/{reason}");
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
            }
            return run;
        }

        private static ConstructionRunModel BuildSquaresUntil(
            ConstructionRunModel run, int target)
        {
            while (run.CategoryBuiltCount(ConstructionCategory.Square) < target)
            {
                if (!FindPlacement(run, ConstructionCategory.Square).HasValue)
                {
                    run = PutInHand(run, ConstructionCategory.Street);
                    var street = FindPlacement(run, ConstructionCategory.Street);
                    Assert.That(street.HasValue, Is.True);
                    Assert.That(run.TryBuild(street.Value.column,
                        street.Value.row, ConstructionCategory.Street,
                        GreenPlanting.None, out _, out _, out _), Is.True);
                    Assert.That(run.TrySimulateFillDay(out _), Is.True);
                }
                run = PutInHand(run, ConstructionCategory.Square);
                var square = FindPlacement(run, ConstructionCategory.Square);
                Assert.That(square.HasValue, Is.True);
                Assert.That(run.TryBuild(square.Value.column,
                    square.Value.row, ConstructionCategory.Square,
                    GreenPlanting.None, out _, out _, out _), Is.True);
                Assert.That(run.TrySimulateFillDay(out _), Is.True);
            }
            return run;
        }

        private static ConstructionRunModel PutInHand(
            ConstructionRunModel run, ConstructionCategory category)
        {
            if (run.FillHandCards.Contains(category)) return run;
            var saved = run.Export();
            saved.fillHandCards[0] = category;
            Assert.That(ConstructionRunModel.TryRestore(saved,
                out var withCard), Is.True);
            return withCard;
        }

        private static ConstructionRunSaveData AddLegacyExtraCard(
            ConstructionRunModel run, bool fifth)
        {
            var saved = run.Export();
            saved.fillHandDeckVersion = 1;
            var extra = System.Enum.GetValues(typeof(ConstructionCategory))
                .Cast<ConstructionCategory>()
                .First(category => !saved.fillHandCards.Contains(category) &&
                    run.CategoryBuiltCount(category) <
                    ConstructionRunModel.DigitalFillCardSupply(category));
            saved.fillHandCards.Add(extra);
            saved.fillHandExpanded = true;
            saved.fillHandExpandedToFive = fifth;
            return saved;
        }

        private static (ConstructionCategory category, int column, int row)?
            FindPlayablePlacement(ConstructionRunModel run)
        {
            foreach (var category in run.FillHandCards)
            {
                var place = FindPlacement(run, category);
                if (place.HasValue) return place;
            }
            if (run.UsesBasicFillResources)
                foreach (var category in new[]
                {
                    ConstructionCategory.Green, ConstructionCategory.Street,
                    ConstructionCategory.Waste
                })
                {
                    var place = FindPlacement(run, category);
                    if (place.HasValue) return place;
                }
            return null;
        }

        private static (ConstructionCategory category, int column, int row)?
            FindPlacement(ConstructionRunModel run,
                ConstructionCategory category)
        {
            for (var row = 0; row < 7; row++)
            for (var column = 0; column < 7; column++)
                if (run.CheckPlacement(column, row, category) ==
                    ConstructionPlacementFailure.None)
                    return (category, column, row);
            return null;
        }
    }
}
