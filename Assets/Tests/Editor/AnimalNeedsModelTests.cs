using NUnit.Framework;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class AnimalNeedsModelTests
    {
        [Test]
        public void IndividualStarvesOnlyAfterThreeCompleteMissedDays()
        {
            var model = new AnimalNeedsModel();
            model.Register("fox-01", WildlifeSpecies.Fox);

            Assert.That(model.CompleteDay(new[] { "fox-01" }), Is.Empty);
            Assert.That(model.Animals["fox-01"].hungerDays, Is.EqualTo(1));
            Assert.That(model.CompleteDay(new[] { "fox-01" }), Is.Empty);
            Assert.That(model.CompleteDay(new[] { "fox-01" }), Is.EqualTo(new[] { "fox-01" }));
            Assert.That(model.Animals["fox-01"].hungerDays, Is.EqualTo(3));
        }

        [Test]
        public void AnyMealResetsHungerAndIsConsumedByDailySettlement()
        {
            var model = new AnimalNeedsModel();
            model.Register("pigeon-01", WildlifeSpecies.Pigeon);
            model.CompleteDay(new[] { "pigeon-01" });
            model.CompleteDay(new[] { "pigeon-01" });

            Assert.That(model.MarkMeal("pigeon-01"), Is.True);
            Assert.That(model.Animals["pigeon-01"].hungerDays, Is.Zero);
            Assert.That(model.CompleteDay(new[] { "pigeon-01" }), Is.Empty);
            Assert.That(model.Animals["pigeon-01"].hungerDays, Is.Zero);
            Assert.That(model.Animals["pigeon-01"].ateToday, Is.False);
        }

        [Test]
        public void DeadAnimalDoesNotAccumulateAdditionalHunger()
        {
            var model = new AnimalNeedsModel();
            model.Register("squirrel-01", WildlifeSpecies.Squirrel);
            model.CompleteDay(new[] { "squirrel-01" });

            model.CompleteDay(System.Array.Empty<string>());

            Assert.That(model.Animals["squirrel-01"].hungerDays, Is.EqualTo(1));
        }

        [Test]
        public void SaveRoundTripPreservesPartialDailyState()
        {
            var original = new AnimalNeedsModel();
            original.Register("hedgehog-01", WildlifeSpecies.Hedgehog);
            original.CompleteDay(new[] { "hedgehog-01" });
            original.MarkMeal("hedgehog-01");

            var restored = new AnimalNeedsModel();
            restored.Register("hedgehog-01", WildlifeSpecies.Hedgehog);
            restored.Restore(original.Export());

            Assert.That(restored.Animals["hedgehog-01"].hungerDays, Is.Zero);
            Assert.That(restored.Animals["hedgehog-01"].ateToday, Is.True);
        }

        [Test]
        public void FoodDiagnosisDistinguishesMissingStockFromAnUnfinishedTrip()
        {
            Assert.That(AnimalNeedsController.ClassifyFoodOutcome(false, 1, 0, 0,
                System.Array.Empty<AnimalFoodAttemptTrace>()),
                Is.EqualTo(AnimalFoodOutcomeReason.NoCompatibleFoodObserved));
            Assert.That(AnimalNeedsController.ClassifyFoodOutcome(false, 1, 2, 0,
                System.Array.Empty<AnimalFoodAttemptTrace>()),
                Is.EqualTo(AnimalFoodOutcomeReason.NoReachableFoodObserved));
            Assert.That(AnimalNeedsController.ClassifyFoodOutcome(false, 1, 2, 2,
                System.Array.Empty<AnimalFoodAttemptTrace>()),
                Is.EqualTo(AnimalFoodOutcomeReason.ReachableButNoTrip));
            Assert.That(AnimalNeedsController.ClassifyFoodOutcome(false, 1, 2, 2,
                new[] { new AnimalFoodAttemptTrace() }),
                Is.EqualTo(AnimalFoodOutcomeReason.TripNotCompleted));
            Assert.That(AnimalNeedsController.ClassifyFoodOutcome(false, 1, 2, 2,
                new[] { new AnimalFoodAttemptTrace(true, true) }),
                Is.EqualTo(AnimalFoodOutcomeReason.CollectedButNotEaten));
            Assert.That(AnimalNeedsController.ClassifyFoodOutcome(true, 0, 0, 0,
                System.Array.Empty<AnimalFoodAttemptTrace>()),
                Is.EqualTo(AnimalFoodOutcomeReason.Fed));
        }

        [Test]
        public void DailyFoodHintNamesTheObservedFailureWithoutClaimingFoodWasGone()
        {
            var blocked = UrbanWildlifeHud.FoodTraceHint(WildlifeSpecies.Pigeon,
                "pigeon-01", AnimalFoodOutcomeReason.NoReachableFoodObserved, true);
            var unfinished = UrbanWildlifeHud.FoodTraceHint(WildlifeSpecies.Pigeon,
                "pigeon-01", AnimalFoodOutcomeReason.TripNotCompleted, false);
            Assert.That(blocked, Does.Contain("动物通道到不了"));
            Assert.That(unfinished, Does.Contain("trip not completed"));
            Assert.That(unfinished, Does.Not.Contain("stock gone"));
        }
    }
}
