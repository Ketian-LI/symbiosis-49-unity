using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class WasteManagementModelTests
    {
        [Test]
        public void DailyBaselineProducesTenUnits()
        {
            var model = BuildInitialModel();

            model.ProduceDailyWaste(4, 2, true);

            Assert.That(model.TotalWasteUnits, Is.EqualTo(10));
        }

        [Test]
        public void WasteUsesNearestReachableRoom()
        {
            var specs = LinearSpecs();
            var model = new WasteManagementModel(BuildNavigation(specs, specs), specs);

            Assert.That(model.RouteWaste("source", 2), Is.True);
            Assert.That(model.WasteRooms["trash-near"].Units, Is.EqualTo(2));
            Assert.That(model.WasteRooms["trash-far"].Units, Is.Zero);
        }

        [Test]
        public void BlockedWasteReroutesAfterConnectivityReturns()
        {
            var specs = LinearSpecs();
            var isolatedSource = new[] { specs.Single(room => room.Id == "source") };
            var model = new WasteManagementModel(BuildNavigation(isolatedSource, specs), specs);

            Assert.That(model.RouteWaste("source", 3), Is.False);
            Assert.That(model.BlockedWaste["source"], Is.EqualTo(3));

            model.UpdateNavigation(BuildNavigation(specs, specs));

            Assert.That(model.BlockedWaste, Is.Empty);
            Assert.That(model.WasteRooms["trash-near"].Units, Is.EqualTo(3));
        }

        [Test]
        public void PenaltyStacksPerAffectedRoomAndCapsAtTwenty()
        {
            var model = BuildInitialModel();
            var states = model.WasteRooms.Keys
                .Select(id => new WasteRoomSaveData { id = id, units = 9 })
                .ToList();
            model.Restore(
                states,
                new[] { new BlockedWasteSaveData { producerRoomId = "residence-a", units = 1 } });

            Assert.That(model.AffectedWasteRoomCount, Is.EqualTo(5));
            Assert.That(model.HumanFunctionPenalty, Is.EqualTo(20));
        }

        [Test]
        public void EmergencyCollectionRequiresOverflowAndTwoResources()
        {
            var model = BuildInitialModel();
            model.WasteRooms["trash-a"].Restore(9);

            Assert.That(model.TryEmergencyCollect("trash-a", 1, out var rejectedCost), Is.False);
            Assert.That(rejectedCost, Is.Zero);
            Assert.That(model.TryEmergencyCollect("trash-a", 2, out var acceptedCost), Is.True);
            Assert.That(acceptedCost, Is.EqualTo(2));
            Assert.That(model.WasteRooms["trash-a"].Units, Is.Zero);
        }

        [Test]
        public void MunicipalCollectionEmptiesAllWasteRooms()
        {
            var model = BuildInitialModel();
            model.ProduceDailyWaste(8, 2, true);

            model.MunicipalCollectAll();

            Assert.That(model.WasteRooms.Values.All(room => room.Units == 0), Is.True);
        }

        [Test]
        public void MarketHotspotCreatesARealButMitigableLayoutChoice()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(layout.TrySwap("pigeon-d", "residence-d"), Is.True);
            Assert.That(layout.TrySwap("residence-e", "trash-d"), Is.True);

            var before = new WasteManagementModel(
                new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f),
                RoomLayoutData.All);
            before.ProduceDailyWaste(8, 2, true);
            Assert.That(before.AffectedWasteRoomCount, Is.Zero);
            before.RouteWaste(NeighborhoodMarketSchedule.ForDay(7).Value.RoomId,
                NeighborhoodMarketSchedule.ExtraWaste);
            Assert.That(before.AffectedWasteRoomCount, Is.EqualTo(1));

            Assert.That(layout.TrySwap("residence-g", "trash-a"), Is.True);
            var after = new WasteManagementModel(
                new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f),
                RoomLayoutData.All);
            after.ProduceDailyWaste(8, 2, true);
            after.RouteWaste(NeighborhoodMarketSchedule.ForDay(7).Value.RoomId,
                NeighborhoodMarketSchedule.ExtraWaste);
            Assert.That(after.AffectedWasteRoomCount, Is.Zero,
                "Moving a trash room should offer a spatial response, not an unavoidable day penalty.");
            foreach (var day in new[] { 7, 11, 15 })
            {
                var later = new WasteManagementModel(
                    new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f),
                    RoomLayoutData.All);
                later.ProduceDailyWaste(8, 2, true);
                var source = NeighborhoodMarketSchedule.ForDay(day).Value.RoomId;
                later.RouteWaste(source, NeighborhoodMarketSchedule.ExtraWaste);
                var loads = string.Join(", ", later.WasteRooms.Select(pair => $"{pair.Key}={pair.Value.Units}"));
                TestContext.WriteLine($"Market day {day} at {source}: {later.AffectedWasteRoomCount} waste problems after first fix; {loads}.");
                Assert.That(later.AffectedWasteRoomCount, Is.EqualTo(day == 7 ? 0 : 1),
                    $"The first hotspot fix should not permanently solve all later locations ({source}).");
            }
        }

        [Test]
        public void DailyShopRotationChangesSpatialWastePressure()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(layout.TrySwap("pigeon-d", "residence-d"), Is.True);
            Assert.That(layout.TrySwap("residence-e", "trash-d"), Is.True);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);
            var affectedByDay = new Dictionary<int, int>();
            foreach (var day in new[] { 4, 5, 6 })
            {
                var activity = NeighborhoodMarketSchedule.ForDay(day).Value;
                var waste = new WasteManagementModel(navigation, RoomLayoutData.All);
                waste.ProduceDailyWaste(8, 2, true);
                waste.RouteWaste(activity.RoomId, activity.ExtraWaste);
                affectedByDay[day] = waste.AffectedWasteRoomCount;
                TestContext.WriteLine($"Day {day} {activity.RoomId}: " +
                    $"affected={waste.AffectedWasteRoomCount}; " +
                    string.Join(", ", waste.WasteRooms.Select(pair => $"{pair.Key}={pair.Value.Units}")));
            }
            Assert.That(affectedByDay[4], Is.EqualTo(1));
            Assert.That(affectedByDay[5], Is.Zero);
            Assert.That(affectedByDay[6], Is.Zero);
            Assert.That(layout.TrySwap("residence-g", "trash-a"), Is.True);
            var mitigated = new WasteManagementModel(
                new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f),
                RoomLayoutData.All);
            mitigated.ProduceDailyWaste(8, 2, true);
            var dayFour = NeighborhoodMarketSchedule.ForDay(4).Value;
            mitigated.RouteWaste(dayFour.RoomId, dayFour.ExtraWaste);
            Assert.That(mitigated.AffectedWasteRoomCount, Is.Zero,
                "The daily hotspot should have a legal spatial response.");
        }

        [Test]
        public void NextMarketHotspotHasAReachableSpatialResponse()
        {
            var prepared = new RoomLayoutModel(RoomLayoutData.All);
            var singles = RoomLayoutData.All.Where(room => room.Movable && room.CellCount == 1).ToArray();
            var viable = new List<string>();
            var solvesAllThree = new List<string>();
            var stableCandidates = 0;
            var lowestAffected = int.MaxValue;
            for (var first = 0; first < singles.Length; first++)
            {
                for (var second = first + 1; second < singles.Length; second++)
                {
                    var candidate = new RoomLayoutModel(RoomLayoutData.All);
                    Assert.That(candidate.TryRestore(prepared.ExportData()), Is.True);
                    if (!candidate.TrySwap(singles[first].Id, singles[second].Id))
                    {
                        continue;
                    }
                    var navigation = new RoomNavigationMap(candidate.ExportData(), RoomLayoutData.All, 3.1f);
                    var animalNavigation = new RoomNavigationMap(
                        candidate.ExportData(), RoomLayoutData.All, 3.1f, true, 11);
                    if (!GreenNetworkModel.IsStable(animalNavigation))
                    {
                        continue;
                    }
                    stableCandidates++;
                    var waste = new WasteManagementModel(navigation, RoomLayoutData.All);
                    waste.ProduceDailyWaste(8, 2, true);
                    waste.RouteWaste(NeighborhoodMarketSchedule.ForDay(11).Value.RoomId,
                        NeighborhoodMarketSchedule.ExtraWaste);
                    lowestAffected = System.Math.Min(lowestAffected, waste.AffectedWasteRoomCount);
                    if (waste.AffectedWasteRoomCount == 0)
                    {
                        var move = $"{singles[first].Id}/{singles[second].Id}";
                        viable.Add(move);
                        var allSafe = new[] { 7, 15 }.All(day =>
                        {
                            var later = new WasteManagementModel(navigation, RoomLayoutData.All);
                            later.ProduceDailyWaste(8, 2, true);
                            later.RouteWaste(NeighborhoodMarketSchedule.ForDay(day).Value.RoomId,
                                NeighborhoodMarketSchedule.ExtraWaste);
                            return later.AffectedWasteRoomCount == 0;
                        });
                        if (allSafe)
                        {
                            solvesAllThree.Add(move);
                        }
                    }
                }
            }
            TestContext.WriteLine($"Day-11 viable one-swap responses preserving linked greenery: {string.Join(", ", viable.Take(20))}");
            TestContext.WriteLine($"Stable candidates={stableCandidates}; lowest affected waste rooms={lowestAffected}");
            TestContext.WriteLine($"Those also safe on days 7 and 15: {string.Join(", ", solvesAllThree.Take(20))}");
            Assert.That(viable, Is.Not.Empty,
                "The stronger market must remain solvable with a legal room exchange.");
            Assert.That(solvesAllThree.Count, Is.LessThan(viable.Count),
                "Most hotspot fixes should still carry a future spatial trade-off, even if a resilient arrangement is possible.");
        }

        private static WasteManagementModel BuildInitialModel()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(layout.ExportData(), RoomLayoutData.All, 3.1f);
            return new WasteManagementModel(navigation, RoomLayoutData.All);
        }

        private static RoomSpec[] LinearSpecs()
        {
            return new[]
            {
                new RoomSpec("source", "Source", RoomType.Residence, 0, 0, 1, 1, true, ""),
                new RoomSpec("trash-near", "Near", RoomType.Trash, 1, 0, 1, 1, true, ""),
                new RoomSpec("middle", "Middle", RoomType.Office, 2, 0, 1, 1, true, ""),
                new RoomSpec("trash-far", "Far", RoomType.Trash, 3, 0, 1, 1, true, "")
            };
        }

        private static RoomNavigationMap BuildNavigation(
            IEnumerable<RoomSpec> placedRooms,
            IEnumerable<RoomSpec> allSpecs)
        {
            var placements = placedRooms.Select(room => new RoomPlacementData
            {
                id = room.Id,
                column = room.Column,
                row = room.Row,
                quarterTurns = 0
            });
            return new RoomNavigationMap(placements, allSpecs, 3.1f);
        }
    }
}
