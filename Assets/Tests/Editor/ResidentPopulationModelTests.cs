using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Tests.Editor
{
    public sealed class ResidentPopulationModelTests
    {
        [Test]
        public void NewRunStartsWithFourResidentsInDifferentHomes()
        {
            var model = BuildInitialModel();

            Assert.That(model.ResidentCount, Is.EqualTo(4));
            Assert.That(model.Residents.Select(item => item.residenceId).Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void GeometricallyOpenLegsDoNotCountAsAnAssignedCycle()
        {
            var legs = new ResidentRouteLegs(true, true, true,
                assignmentBlocked: true);
            Assert.That(legs.Work && legs.Meal && legs.Home, Is.True);
            Assert.That(legs.Complete, Is.False);
        }

        [Test]
        public void ThreeFailedCommuteCyclesCauseDepartureButNotOnTheFirstTwoDays()
        {
            var disconnected = new RoomNavigationMap(
                System.Array.Empty<RoomPlacementData>(), RoomLayoutData.All, 3.1f,
                humanRoadsOnly: true);
            var model = new ResidentPopulationModel(disconnected, RoomLayoutData.All);
            Assert.That(model.PreviewRouteLegs(model.Residents[0].id).Complete, Is.False);
            model.CompleteDay(1, 0, false);
            model.CompleteDay(2, 0, false);
            Assert.That(model.ResidentCount, Is.EqualTo(4));
            Assert.That(model.Residents.All(item => item.WarningState ==
                ResidentWarningState.LeavingTomorrow), Is.True);
            var third = model.CompleteDay(3, 0, false);
            Assert.That(third.ResidentDeparted, Is.True);
            Assert.That(model.ResidentCount, Is.Zero);
            Assert.That(model.CumulativeDepartures, Is.EqualTo(4));
        }

        [Test]
        public void SuccessfulCycleClearsFailedDayStreak()
        {
            var model = BuildInitialModel();
            var connected = model.NavigationMap;
            var disconnected = new RoomNavigationMap(
                System.Array.Empty<RoomPlacementData>(), RoomLayoutData.All, 3.1f,
                humanRoadsOnly: true);
            model.UpdateNavigation(disconnected);
            model.CompleteDay(1, 0, false);
            model.UpdateNavigation(connected);
            model.CompleteDay(2, 0, false);
            Assert.That(model.Residents.All(item => item.dissatisfiedDays == 0), Is.True);
            model.UpdateNavigation(disconnected);
            model.CompleteDay(3, 0, false);
            model.CompleteDay(4, 0, false);
            Assert.That(model.ResidentCount, Is.EqualTo(4));
        }

        [Test]
        public void ResidentRouteCanBePreviewedBeforeFirstDaySettles()
        {
            var model = BuildInitialModel();
            var first = model.Residents[0];

            Assert.That(model.TryGetRoute(first.id, out var home,
                out var office, out var foodShop), Is.True);
            Assert.That(home, Is.EqualTo(first.residenceId));
            Assert.That(office, Is.Not.Empty);
            Assert.That(foodShop, Is.Not.Empty);
            Assert.That(model.TryGetRoute("missing", out _, out _, out _), Is.False);
        }

        [Test]
        public void PreviewRoutesRespectOfficeAndFoodShopCapacity()
        {
            var model = BuildInitialModel();
            var routes = model.Residents
                .Select(resident =>
                {
                    var assigned = model.TryGetRoute(resident.id, out _,
                        out var office, out var foodShop);
                    return new { assigned, office, foodShop };
                })
                .ToArray();

            Assert.That(routes.All(route => route.assigned), Is.True);
            Assert.That(routes.GroupBy(route => route.office)
                .All(group => group.Count() <= ResidentPopulationModel.OfficeCapacity), Is.True);
            Assert.That(routes.GroupBy(route => route.foodShop)
                .All(group => group.Count() <= ResidentPopulationModel.FoodShopCapacity), Is.True);
        }

        [Test]
        public void FacilityBadgesUseTheSamePlannedFullCycleAssignmentsAsCommute()
        {
            var model = BuildInitialModel();
            var preview = model.PreviewCommute(model.NavigationMap);
            var use = model.PreviewFacilityUse();

            Assert.That(use.Offices.Count, Is.EqualTo(4));
            Assert.That(use.FoodShops.Count, Is.EqualTo(2));
            Assert.That(use.Offices.Values.Sum(), Is.EqualTo(preview.WorkingResidents));
            Assert.That(use.FoodShops.Values.Sum(), Is.EqualTo(preview.WorkingResidents));
            Assert.That(use.Offices.Values.All(value =>
                value <= ResidentPopulationModel.OfficeCapacity), Is.True);
            Assert.That(use.FoodShops.Values.All(value =>
                value <= ResidentPopulationModel.FoodShopCapacity), Is.True);

            var disconnected = new RoomNavigationMap(
                System.Array.Empty<RoomPlacementData>(), RoomLayoutData.All, 3.1f,
                humanRoadsOnly: true);
            var disconnectedUse = model.PreviewFacilityUse(disconnected);
            Assert.That(disconnectedUse.Offices.Values.Sum(), Is.Zero);
            Assert.That(disconnectedUse.FoodShops.Values.Sum(), Is.Zero);
            Assert.That(model.ResidentCount, Is.EqualTo(4),
                "The map display must not mutate the resident population.");
        }

        [Test]
        public void TwoQualifiedDaysAnnounceThenAddOneResident()
        {
            var model = BuildInitialModel();

            var first = model.CompleteDay(1, 100, true);
            Assert.That(first.ResidentArrived, Is.False);
            Assert.That(model.ProspectiveResidenceId, Is.Not.Empty);

            var announcedHome = model.ProspectiveResidenceId;
            var second = model.CompleteDay(2, 100, true);
            Assert.That(second.ResidentArrived, Is.True);
            Assert.That(second.ArrivalResidenceId, Is.EqualTo(announcedHome));
            Assert.That(model.ResidentCount, Is.EqualTo(5));
        }

        [Test]
        public void StaticLayoutLimitsGrowthWhileKeepingTheEightResidentCap()
        {
            var model = BuildInitialModel();

            for (var day = 1; day <= 12; day++)
            {
                model.CompleteDay(day, 100, true);
            }

            Assert.That(model.ResidentCount, Is.EqualTo(7),
                "Without rearranging the 49 rooms, one home lacks a sustainable commute.");
            Assert.That(model.PeakResidents, Is.LessThanOrEqualTo(ResidentPopulationModel.MaximumResidents));
            Assert.That(model.CumulativeArrivals, Is.EqualTo(4));
            Assert.That(model.CumulativeDepartures, Is.EqualTo(1));
        }

        [Test]
        public void AfterBirdHabitatRescueAFreeSwapCanImproveTheResidentCommute()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(layout.TrySwap("pigeon-c", "shared-f"), Is.True);
            Assert.That(layout.TrySwap("pigeon-d", "shared-h"), Is.True);
            Assert.That(layout.TrySwap("pigeon-a", "shared-d"), Is.True);
            var model = BuildInitialModel();
            var baseline = model.PreviewCommute(new RoomNavigationMap(
                layout.ExportData(), RoomLayoutData.All, 3.1f));
            var movable = RoomLayoutData.All.Where(room => room.Movable).ToArray();
            var options = new List<(string move, float production)>();
            for (var first = 0; first < movable.Length; first++)
            {
                for (var second = first + 1; second < movable.Length; second++)
                {
                    var candidate = new RoomLayoutModel(RoomLayoutData.All);
                    Assert.That(candidate.TryRestore(layout.ExportData()), Is.True);
                    if (!candidate.TrySwap(movable[first].Id, movable[second].Id)) continue;
                    var forecast = model.PreviewCommute(new RoomNavigationMap(
                        candidate.ExportData(), RoomLayoutData.All, 3.1f));
                    options.Add(($"{movable[first].Id}/{movable[second].Id}", forecast.Production));
                }
            }
            var best = options.OrderByDescending(item => item.production).First();
            TestContext.WriteLine($"Post-rescue baseline production {baseline.Production}; best one-swap {best.move} gives {best.production}.");
            Assert.That(best.production, Is.GreaterThan(baseline.Production));

            Assert.That(layout.TrySwap("trash-b", "canteen-b"), Is.True);
            var improvedBaseline = model.PreviewCommute(new RoomNavigationMap(
                layout.ExportData(), RoomLayoutData.All, 3.1f));
            options.Clear();
            for (var first = 0; first < movable.Length; first++)
            {
                for (var second = first + 1; second < movable.Length; second++)
                {
                    var candidate = new RoomLayoutModel(RoomLayoutData.All);
                    Assert.That(candidate.TryRestore(layout.ExportData()), Is.True);
                    if (!candidate.TrySwap(movable[first].Id, movable[second].Id)) continue;
                    var forecast = model.PreviewCommute(new RoomNavigationMap(
                        candidate.ExportData(), RoomLayoutData.All, 3.1f));
                    options.Add(($"{movable[first].Id}/{movable[second].Id}", forecast.Production));
                }
            }
            var nextBest = options.OrderByDescending(item => item.production).First();
            TestContext.WriteLine($"After the first commute fix: {improvedBaseline.Production}; best next swap {nextBest.move} gives {nextBest.production}.");
        }

        [Test]
        public void BrokenHumanFunctionCancelsProspectiveArrival()
        {
            var model = BuildInitialModel();
            model.CompleteDay(1, 100, true);
            Assert.That(model.ProspectiveResidenceId, Is.Not.Empty);

            model.CompleteDay(2, 74, true);

            Assert.That(model.ProspectiveResidenceId, Is.Empty);
            Assert.That(model.ResidentCount, Is.EqualTo(4));
        }

        [Test]
        public void CommuteEfficiencyUsesDiscreteFullAndHalfStates()
        {
            var model = BuildInitialModel();
            var report = model.CompleteDay(1, 100, true);

            Assert.That(report.Production, Is.GreaterThanOrEqualTo(0f));
            Assert.That(model.Residents.All(item =>
                item.lastEfficiency == 0f || item.lastEfficiency == 0.5f || item.lastEfficiency == 1f), Is.True);
        }

        [Test]
        public void CommunitySquareImprovesViableLongCommuteEfficiency()
        {
            RoomSpec[] BuildRooms(RoomType squareType) => new[]
            {
                new RoomSpec("home", "Home", RoomType.Residence, 0, 0, 1, 1, true, ""),
                new RoomSpec("walk-a", "Walk A", RoomType.SharedSpace, 1, 0, 1, 1, true, ""),
                new RoomSpec("walk-b", "Walk B", RoomType.SharedSpace, 2, 0, 1, 1, true, ""),
                new RoomSpec("work", "Work", RoomType.Office, 3, 0, 1, 1, true, ""),
                new RoomSpec("square", "Square", squareType, 3, 1, 1, 1, true, ""),
                new RoomSpec("shop", "Shop", RoomType.Canteen, 4, 1, 1, 1, true, "")
            };

            float Production(RoomType squareType)
            {
                var rooms = BuildRooms(squareType);
                var placements = rooms.Select(room => new RoomPlacementData
                {
                    id = room.Id, column = room.Column, row = room.Row
                });
                var model = new ResidentPopulationModel(
                    new RoomNavigationMap(placements, rooms, 2f), rooms);
                var preview = model.PreviewCommute(model.NavigationMap);
                Assert.That(preview.WorkingResidents, Is.EqualTo(1));
                Assert.That(model.CompleteDay(1, 0, true).Production,
                    Is.EqualTo(preview.Production).Within(0.001f));
                return preview.Production;
            }

            Assert.That(Production(RoomType.SharedSpace), Is.EqualTo(0.5f));
            Assert.That(Production(RoomType.CommunitySquare), Is.EqualTo(0.75f));
        }

        [Test]
        public void SharedCommunitySquareMakesOneExtraWorkStepViableOnlyWhenBothDestinationsBorderIt()
        {
            int WorkingResidents(RoomType squareType, bool shopBordersSquare)
            {
                var rooms = new List<RoomSpec>
                {
                    new("home", "Home", RoomType.Residence, 0, 0, 1, 1, true, "")
                };
                for (var column = 1; column <= 4; column++)
                {
                    rooms.Add(new RoomSpec($"walk-{column}", "Walk", RoomType.SharedSpace,
                        column, 0, 1, 1, true, ""));
                }
                rooms.Add(new RoomSpec("work", "Work", RoomType.Office, 5, 0, 1, 1, true, ""));
                rooms.Add(new RoomSpec("square", "Square", squareType, 5, 1, 1, 1, true, ""));
                rooms.Add(new RoomSpec("shop", "Shop", RoomType.Canteen,
                    6, shopBordersSquare ? 1 : 0, 1, 1, true, ""));
                var placements = rooms.Select(room => new RoomPlacementData
                {
                    id = room.Id, column = room.Column, row = room.Row
                });
                var navigation = new RoomNavigationMap(placements, rooms, 2f);
                var population = new ResidentPopulationModel(navigation, rooms);
                return population.PreviewCommute(navigation).WorkingResidents;
            }

            Assert.That(WorkingResidents(RoomType.SharedSpace, true), Is.Zero);
            Assert.That(WorkingResidents(RoomType.CommunitySquare, true), Is.EqualTo(1));
            Assert.That(WorkingResidents(RoomType.CommunitySquare, false), Is.Zero);
        }

        [Test]
        public void MovingTheRealCommunitySquareCanOpenAWorkRouteForRemoteHome()
        {
            var population = BuildInitialModel();
            population.Restore(new[]
            {
                new ResidentSaveData { id = "R001", residenceId = "residence-g" }
            }, string.Empty, 2, 0, 0, 0, 1);
            var before = population.PreviewCommute(population.NavigationMap);
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            Assert.That(layout.TrySwap("shared-k", "canteen-b"), Is.True);
            var after = population.PreviewCommute(new RoomNavigationMap(
                layout.ExportData(), RoomLayoutData.All, 3.1f));

            Assert.That(before.WorkingResidents, Is.Zero);
            Assert.That(after.WorkingResidents, Is.EqualTo(1),
                "The community square should offer a real spatial way to recover workforce.");
        }

        [Test]
        public void CommutePreviewMatchesSettlementWithoutMutatingResidents()
        {
            var model = BuildInitialModel();
            var before = model.ExportResidents();
            var preview = model.PreviewCommute(model.NavigationMap);

            Assert.That(model.Residents.Select(item => item.assignedOfficeId),
                Is.EqualTo(before.Select(item => item.assignedOfficeId)));
            Assert.That(model.Residents.Select(item => item.dissatisfiedDays),
                Is.EqualTo(before.Select(item => item.dissatisfiedDays)));

            var report = model.CompleteDay(1, 100, true);
            Assert.That(preview.Production, Is.EqualTo(report.Production).Within(0.001f));
            Assert.That(report.WorkingResidents, Is.EqualTo(preview.WorkingResidents));
            Assert.That(preview.WorkingResidents,
                Is.EqualTo(model.Residents.Count(item => !string.IsNullOrEmpty(item.assignedOfficeId))));
        }

        [Test]
        public void CommutePreviewUsesCandidateMapWithoutChangingLiveRoutes()
        {
            var model = BuildInitialModel();
            var baseline = model.PreviewCommute(model.NavigationMap);
            var disconnected = new RoomNavigationMap(
                System.Array.Empty<RoomPlacementData>(), RoomLayoutData.All, 3.1f);

            var projected = model.PreviewCommute(disconnected);

            Assert.That(projected.WorkingResidents, Is.Zero);
            Assert.That(projected.Production, Is.Zero);
            Assert.That(model.PreviewCommute(model.NavigationMap).Production,
                Is.EqualTo(baseline.Production));
        }

        private static ResidentPopulationModel BuildInitialModel()
        {
            var layout = new RoomLayoutModel(RoomLayoutData.All);
            var navigation = new RoomNavigationMap(
                layout.ExportData(),
                RoomLayoutData.All,
                3.1f);
            return new ResidentPopulationModel(navigation, RoomLayoutData.All);
        }
    }
}
