using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UrbanWildlifeRooms.Data;

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
                    new() { roomId = "pigeon-a", kind = "Seed", portions = 1 }
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
                }
            };

            var restored = JsonUtility.FromJson<SessionSaveData>(JsonUtility.ToJson(original));
            Assert.That(restored.schemaVersion, Is.EqualTo(1));
            Assert.That(restored.mode, Is.EqualTo("Sandbox"));
            Assert.That(restored.elapsedSimulationSeconds, Is.EqualTo(123.5d));
            Assert.That(restored.speedMultiplier, Is.EqualTo(4));
            Assert.That(restored.rooms, Has.Count.EqualTo(35));
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
            Assert.That(restored.squirrelDemoCachePortions, Is.EqualTo(2));
            Assert.That(restored.squirrelCachePortions, Is.EqualTo(new[] { 2, 1, 0, 3 }));
            Assert.That(restored.roomMovements, Is.EqualTo(7));
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
        }
    }
}
