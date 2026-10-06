using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class SessionSaveDataTests
    {
        [Test]
        public void JsonRoundTripKeepsTimeSpeedModeAndAllRooms()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var original = new SessionSaveData
            {
                savedAtUtc = "2026-09-18T23:00:00Z",
                mode = "Sandbox",
                sandboxDifficulty = "Gentle",
                sandboxSettings = new EndlessDifficultySettings
                {
                    template = EndlessDifficulty.Gentle,
                    startingCommunity = 85,
                    commuteTargetPercent = 80,
                    wildlifeFloor = 6,
                    wildlifeGraceDays = 4
                },
                elapsedSimulationSeconds = 123.5d,
                speedMultiplier = 4,
                rooms = new List<RoomPlacementData>(layout.ExportData()),
                residentCount = 6,
                operatingFoodShopCount = 2,
                supermarketOperating = true,
                resourceBalance = 11.5f,
                cumulativeResourceIncome = 18.5f,
                cumulativeResourceSpending = 15f,
                unstoredResourceSurplus = 2f,
                peakResourceBalance = 20f,
                residents = new List<ResidentSaveData>
                {
                    new()
                    {
                        id = "R001",
                        residenceId = "residence-a",
                        assignedOfficeId = "office-a",
                        assignedFoodShopId = "canteen-a",
                        dissatisfiedDays = 1,
                        lastEfficiency = 0.5f
                    }
                },
                prospectiveResidenceId = "residence-h",
                nextResidentNumber = 6,
                cumulativeResidentArrivals = 1,
                cumulativeResidentRelocations = 2,
                cumulativeResidentDepartures = 1,
                peakResidentCount = 6,
                oakTrees = new List<OakTreeSaveData>
                {
                    new() { roomId = "oak-a", stage = "Sapling", completeDaysSincePlanting = 0 }
                },
                treesFelled = 2,
                treesPlanted = 1,
                roomMovements = 7,
                lastRoomMovementDay = 4,
                lastLayoutPlanningDay = 5,
                lastManualFeedingDay = 3,
                shrubShelters = new List<ShrubShelterSaveData>
                {
                    new() { roomId = "shrub-a", readyDay = 6 }
                },
                dailyOutcome = new DailyOutcomeSaveData
                {
                    pendingDay = 5,
                    pendingMovedRooms = 2,
                    previousTotalDeaths = 3,
                    lastDay = 4,
                    lastMovedRooms = 1,
                    lastWasteIssues = 2
                },
                playerFoodSources = new List<PlayerFoodSourceSaveData>
                {
                    new()
                    {
                        id = "food-001",
                        roomId = "central-park",
                        x = 1f,
                        y = 0.3f,
                        z = 2f,
                        portions = 3,
                        remainingLifetime = 19f,
                        playerPlaced = true
                    }
                },
                squirrelDemoCachePortions = 2,
                squirrelCachePortions = new List<int> { 2, 1, 0, 3 },
                pigeonDeaths = 2,
                squirrelDeaths = 1,
                trafficDeaths = 2,
                naturalFoodSources = new List<NaturalFoodSaveData>
                {
                    new()
                    {
                        roomId = "pigeon-a", kind = "Seed", portions = 1,
                        addedToday = 1, dailyAdditionKnown = true, productionDayNumber = 2
                    }
                },
                animalNeeds = new List<AnimalNeedSaveData>
                {
                    new() { id = "pigeon-01", species = "Pigeon", hungerDays = 2, ateToday = false }
                },
                wasteRooms = new List<WasteRoomSaveData>
                {
                    new() { id = "trash-a", units = 7 }
                },
                blockedWaste = new List<BlockedWasteSaveData>
                {
                    new() { producerRoomId = "residence-a", units = 2 }
                },
                endlessBalance = new EndlessBalanceSaveData
                {
                    lastSettledDay = 4,
                    community = 52,
                    criticalWildlifeDays = 1,
                    pigeonRecoveryDays = 2
                },
                endlessDeadAnimalIds = new List<string> { "pigeon-03", "hedgehog-01" }
            };

            var restored = JsonUtility.FromJson<SessionSaveData>(JsonUtility.ToJson(original));
            Assert.That(restored.schemaVersion, Is.EqualTo(2));
            Assert.That(restored.mode, Is.EqualTo("Sandbox"));
            Assert.That(restored.sandboxDifficulty, Is.EqualTo("Gentle"));
            Assert.That(restored.sandboxSettings.startingCommunity, Is.EqualTo(85));
            Assert.That(restored.sandboxSettings.commuteTargetPercent, Is.EqualTo(80));
            Assert.That(restored.sandboxSettings.wildlifeFloor, Is.EqualTo(6));
            Assert.That(restored.sandboxSettings.wildlifeGraceDays, Is.EqualTo(4));
            Assert.That(restored.elapsedSimulationSeconds, Is.EqualTo(123.5d));
            Assert.That(restored.speedMultiplier, Is.EqualTo(4));
            Assert.That(restored.rooms, Has.Count.EqualTo(49));
            Assert.That(restored.residentCount, Is.EqualTo(6));
            Assert.That(restored.resourceBalance, Is.EqualTo(11.5f));
            Assert.That(restored.cumulativeResourceIncome, Is.EqualTo(18.5f));
            Assert.That(restored.cumulativeResourceSpending, Is.EqualTo(15f));
            Assert.That(restored.unstoredResourceSurplus, Is.EqualTo(2f));
            Assert.That(restored.peakResourceBalance, Is.EqualTo(20f));
            Assert.That(restored.residents, Has.Count.EqualTo(1));
            Assert.That(restored.residents[0].residenceId, Is.EqualTo("residence-a"));
            Assert.That(restored.residents[0].lastEfficiency, Is.EqualTo(0.5f));
            Assert.That(restored.prospectiveResidenceId, Is.EqualTo("residence-h"));
            Assert.That(restored.cumulativeResidentRelocations, Is.EqualTo(2));
            Assert.That(restored.oakTrees, Has.Count.EqualTo(1));
            Assert.That(restored.oakTrees[0].stage, Is.EqualTo("Sapling"));
            Assert.That(restored.treesFelled, Is.EqualTo(2));
            Assert.That(restored.playerFoodSources, Has.Count.EqualTo(1));
            Assert.That(restored.playerFoodSources[0].portions, Is.EqualTo(3));
            Assert.That(restored.playerFoodSources[0].playerPlaced, Is.True);
            Assert.That(restored.naturalFoodSources[0].addedToday, Is.EqualTo(1));
            Assert.That(restored.naturalFoodSources[0].dailyAdditionKnown, Is.True);
            Assert.That(restored.naturalFoodSources[0].productionDayNumber, Is.EqualTo(2));
            Assert.That(restored.squirrelDemoCachePortions, Is.EqualTo(2));
            Assert.That(restored.squirrelCachePortions, Is.EqualTo(new[] { 2, 1, 0, 3 }));
            Assert.That(restored.roomMovements, Is.EqualTo(7));
            Assert.That(restored.lastRoomMovementDay, Is.EqualTo(4));
            Assert.That(restored.lastLayoutPlanningDay, Is.EqualTo(5));
            Assert.That(restored.lastManualFeedingDay, Is.EqualTo(3));
            Assert.That(restored.shrubShelters, Has.Count.EqualTo(1));
            Assert.That(restored.shrubShelters[0].readyDay, Is.EqualTo(6));
            Assert.That(restored.dailyOutcome.pendingMovedRooms, Is.EqualTo(2));
            Assert.That(restored.dailyOutcome.previousTotalDeaths, Is.EqualTo(3));
            Assert.That(restored.dailyOutcome.lastWasteIssues, Is.EqualTo(2));
            Assert.That(restored.pigeonDeaths, Is.EqualTo(2));
            Assert.That(restored.squirrelDeaths, Is.EqualTo(1));
            Assert.That(restored.trafficDeaths, Is.EqualTo(2));
            Assert.That(restored.naturalFoodSources, Has.Count.EqualTo(1));
            Assert.That(restored.naturalFoodSources[0].kind, Is.EqualTo("Seed"));
            Assert.That(restored.animalNeeds, Has.Count.EqualTo(1));
            Assert.That(restored.animalNeeds[0].hungerDays, Is.EqualTo(2));
            Assert.That(restored.wasteRooms, Has.Count.EqualTo(1));
            Assert.That(restored.wasteRooms[0].units, Is.EqualTo(7));
            Assert.That(restored.blockedWaste[0].units, Is.EqualTo(2));
            Assert.That(restored.endlessBalance.community, Is.EqualTo(52));
            Assert.That(restored.endlessBalance.criticalWildlifeDays, Is.EqualTo(1));
            Assert.That(restored.endlessBalance.pigeonRecoveryDays, Is.EqualTo(2));
            Assert.That(restored.endlessDeadAnimalIds,
                Is.EqualTo(new[] { "pigeon-03", "hedgehog-01" }));
        }

        [Test]
        public void OlderV2SaveWithoutEndlessFieldsUsesBaselineOnRestore()
        {
            var legacy = JsonUtility.FromJson<SessionSaveData>(
                "{\"schemaVersion\":2,\"mode\":\"Sandbox\",\"elapsedSimulationSeconds\":120}");
            var model = new UrbanWildlifeRooms.Core.EndlessBalanceModel();
            model.Restore(legacy.endlessBalance);
            // JsonUtility may materialize field initializers for a missing field.
            // Null and a default instance must both preserve Standard rules.
            Assert.That(EndlessDifficultyRules.For(legacy.sandboxSettings).StartingCommunity,
                Is.EqualTo(60));
            Assert.That(model.Community,
                Is.EqualTo(UrbanWildlifeRooms.Core.EndlessBalanceModel.StartingCommunity));
            Assert.That(model.LastSettledDay, Is.Zero);
        }
    }
}
