using System.Collections.Generic;
using NUnit.Framework;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class EndlessBalanceModelTests
    {
        [Test]
        public void LegacyCommunitySettingsDoNotChangeActiveDifficulty()
        {
            var settings = EndlessDifficultySettings.Preset(EndlessDifficulty.Gentle);
            settings.startingCommunity = 85;
            settings.commuteTargetPercent = 100;
            Assert.That(settings.IsCustom, Is.False);
            Assert.That(EndlessDifficultyRules.For(settings).Label(false), Is.EqualTo("Gentle"));
        }

        [Test]
        public void PresetIsEditableForWildlifeThresholdAndGrace()
        {
            var settings = EndlessDifficultySettings.Preset(EndlessDifficulty.Gentle);
            settings.wildlifeFloor = 6;
            settings.wildlifeGraceDays = 4;
            var rules = EndlessDifficultyRules.For(settings);
            var model = new EndlessBalanceModel(settings);
            Assert.That(settings.IsCustom, Is.True);
            Assert.That(rules.Label(false), Is.EqualTo("Custom"));
            Assert.That(model.CurrentWildlifeFloor, Is.EqualTo(6));
            Assert.That(model.WildlifeGraceDays, Is.EqualTo(4));
        }

        [Test]
        public void EditableLimitsAreClampedAndModelKeepsFourDayGrace()
        {
            var settings = new EndlessDifficultySettings
            {
                wildlifeFloor = 30, wildlifeGraceDays = 0
            }.Normalized();
            Assert.That(settings.wildlifeFloor, Is.EqualTo(18));
            Assert.That(settings.wildlifeGraceDays, Is.EqualTo(1));

            var custom = EndlessDifficultySettings.Preset(EndlessDifficulty.Standard);
            custom.wildlifeFloor = 8;
            custom.wildlifeGraceDays = 4;
            var model = new EndlessBalanceModel(custom);
            for (var day = 1; day <= 3; day++)
            {
                model.SettleDay(day, 7);
                Assert.That(model.WildlifeCollapse, Is.False);
            }
            model.SettleDay(4, 7);
            Assert.That(model.WildlifeCollapse, Is.True);
            model.Reset();
            Assert.That(model.CriticalWildlifeDays, Is.Zero);
            Assert.That(model.CurrentWildlifeFloor, Is.EqualTo(8));
        }

        [Test]
        public void SameDayCannotSettleTwice_AndWildlifeHasGraceDay()
        {
            var model = new EndlessBalanceModel();
            model.SettleDay(1, 9);
            model.SettleDay(1, 0);
            Assert.That(model.CriticalWildlifeDays, Is.EqualTo(1));
            Assert.That(model.LastWildlifeCount, Is.EqualTo(9));
            Assert.That(model.WildlifeCollapse, Is.False);
            model.SettleDay(2, 9);
            Assert.That(model.WildlifeCollapse, Is.True);
            model.SettleDay(3, 10);
            Assert.That(model.WildlifeCollapse, Is.False);
        }

        [Test]
        public void RecoveryRequiresThreeEligibleDays_ThenOnlyOneArrival()
        {
            var model = new EndlessBalanceModel();
            var eligible = new Dictionary<WildlifeSpecies, bool>
            {
                [WildlifeSpecies.Fox] = true,
                [WildlifeSpecies.Hedgehog] = true
            };
            var living = new Dictionary<WildlifeSpecies, int>
            {
                [WildlifeSpecies.Fox] = 0,
                [WildlifeSpecies.Hedgehog] = 1
            };
            var slots = new Dictionary<WildlifeSpecies, int>
            {
                [WildlifeSpecies.Fox] = 1,
                [WildlifeSpecies.Hedgehog] = 1
            };
            Assert.That(model.SelectArrival(eligible, living, slots), Is.Null);
            Assert.That(model.SelectArrival(eligible, living, slots), Is.Null);
            Assert.That(model.SelectArrival(eligible, living, slots), Is.EqualTo(WildlifeSpecies.Fox));
            Assert.That(model.SelectArrival(eligible, living, slots), Is.EqualTo(WildlifeSpecies.Hedgehog));
        }

        [Test]
        public void SaveRestoreKeepsPressureAndRecoveryProgress()
        {
            var model = new EndlessBalanceModel();
            model.SettleDay(3, 9);
            model.SelectArrival(
                new Dictionary<WildlifeSpecies, bool> { [WildlifeSpecies.Pigeon] = true },
                new Dictionary<WildlifeSpecies, int> { [WildlifeSpecies.Pigeon] = 11 },
                new Dictionary<WildlifeSpecies, int> { [WildlifeSpecies.Pigeon] = 1 });
            var restored = new EndlessBalanceModel();
            restored.Restore(model.Export());
            Assert.That(restored.LastSettledDay, Is.EqualTo(3));
            Assert.That(restored.LastWildlifeCount, Is.EqualTo(9));
            Assert.That(restored.CriticalWildlifeDays, Is.EqualTo(1));
            Assert.That(restored.RecoveryProgress(WildlifeSpecies.Pigeon), Is.EqualTo(1));
        }

        [Test]
        public void WildlifeEmergencyFloorDoesNotIncreaseWithElapsedDays()
        {
            Assert.That(EndlessBalanceModel.WildlifeFloorForDay(1), Is.EqualTo(10));
            Assert.That(EndlessBalanceModel.WildlifeFloorForDay(10), Is.EqualTo(10));
            Assert.That(EndlessBalanceModel.WildlifeFloorForDay(11), Is.EqualTo(10));
            Assert.That(EndlessBalanceModel.WildlifeFloorForDay(21), Is.EqualTo(10));
            Assert.That(EndlessBalanceModel.WildlifeFloorForDay(100), Is.EqualTo(10));
        }

        [Test]
        public void ArrivalCapacityTracksReachableHabitatAndNeverExceedsActorSlots()
        {
            var access = new AnimalFoodAccessSnapshot(2, 3, 1, 0);
            Assert.That(EndlessBalanceModel.HabitatCapacityFor(
                WildlifeSpecies.Pigeon, access, 12), Is.EqualTo(6));
            Assert.That(EndlessBalanceModel.HabitatCapacityFor(
                WildlifeSpecies.Squirrel, access, 4), Is.EqualTo(3));
            Assert.That(EndlessBalanceModel.HabitatCapacityFor(
                WildlifeSpecies.Hedgehog, access, 2), Is.EqualTo(1));
            Assert.That(EndlessBalanceModel.HabitatCapacityFor(
                WildlifeSpecies.Fox, access, 1), Is.Zero);
            Assert.That(EndlessBalanceModel.HabitatCapacityFor(
                WildlifeSpecies.Pigeon, new AnimalFoodAccessSnapshot(6, 0, 0, 0), 12),
                Is.EqualTo(12));
        }

    }
}
