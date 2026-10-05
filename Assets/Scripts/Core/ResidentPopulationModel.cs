using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum ResidentWarningState
    {
        None,
        RouteAtRisk,
        LeavingTomorrow
    }

    [Serializable]
    public sealed class ResidentState
    {
        public string id;
        public string residenceId;
        public string assignedOfficeId;
        public string assignedFoodShopId;
        public int dissatisfiedDays;
        public int leaveCountdownDays;
        public float lastEfficiency;

        public ResidentWarningState WarningState => dissatisfiedDays >= 2
            ? ResidentWarningState.LeavingTomorrow
            : dissatisfiedDays >= 1
                ? ResidentWarningState.RouteAtRisk
                : ResidentWarningState.None;

        public ResidentState Clone()
        {
            return new ResidentState
            {
                id = id,
                residenceId = residenceId,
                assignedOfficeId = assignedOfficeId,
                assignedFoodShopId = assignedFoodShopId,
                dissatisfiedDays = dissatisfiedDays,
                leaveCountdownDays = leaveCountdownDays,
                lastEfficiency = lastEfficiency
            };
        }
    }

    public readonly struct ResidentDayReport
    {
        public ResidentDayReport(
            int dayNumber,
            int workingResidents,
            float production,
            int foodServiceCost,
            string arrivalResidenceId,
            string relocatedResidentId,
            string departureResidentId)
        {
            DayNumber = dayNumber;
            WorkingResidents = workingResidents;
            Production = production;
            FoodServiceCost = foodServiceCost;
            ArrivalResidenceId = arrivalResidenceId;
            RelocatedResidentId = relocatedResidentId;
            DepartureResidentId = departureResidentId;
        }

        public int DayNumber { get; }
        public int WorkingResidents { get; }
        public float Production { get; }
        public int FoodServiceCost { get; }
        public string ArrivalResidenceId { get; }
        public string RelocatedResidentId { get; }
        public string DepartureResidentId { get; }
        public bool ResidentArrived => !string.IsNullOrEmpty(ArrivalResidenceId);
        public bool ResidentRelocated => !string.IsNullOrEmpty(RelocatedResidentId);
        public bool ResidentDeparted => !string.IsNullOrEmpty(DepartureResidentId);
    }

    public readonly struct ResidentCommutePreview
    {
        public ResidentCommutePreview(int workingResidents, float production)
        {
            WorkingResidents = workingResidents;
            Production = production;
        }

        public int WorkingResidents { get; }
        public float Production { get; }
    }

    public readonly struct ResidentFacilityUsePreview
    {
        public ResidentFacilityUsePreview(IReadOnlyDictionary<string, int> offices,
            IReadOnlyDictionary<string, int> foodShops)
        {
            Offices = offices;
            FoodShops = foodShops;
        }

        public IReadOnlyDictionary<string, int> Offices { get; }
        public IReadOnlyDictionary<string, int> FoodShops { get; }
    }

    public readonly struct ResidentRouteLegs
    {
        public ResidentRouteLegs(bool work, bool meal, bool home,
            bool assignmentBlocked = false)
        {
            Work = work;
            Meal = meal;
            Home = home;
            AssignmentBlocked = assignmentBlocked;
        }

        public bool Work { get; }
        public bool Meal { get; }
        public bool Home { get; }
        public bool AssignmentBlocked { get; }
        public bool Complete => Work && Meal && Home && !AssignmentBlocked;
    }

    public sealed class ResidentPopulationModel
    {
        public const int StartingResidents = 4;
        public const int MaximumResidents = 8;
        public const int OfficeCapacity = 2;
        public const int FoodShopCapacity = 4;

        private readonly Dictionary<string, RoomSpec> specs;
        private readonly List<ResidentState> residents = new();
        private RoomNavigationMap navigation;
        private int nextResidentNumber;

        public ResidentPopulationModel(
            RoomNavigationMap navigationMap,
            IEnumerable<RoomSpec> roomSpecs)
        {
            specs = roomSpecs.ToDictionary(room => room.Id);
            navigation = navigationMap;
            Reset();
        }

        public IReadOnlyList<ResidentState> Residents => residents;
        public RoomNavigationMap NavigationMap => navigation;
        public int ResidentCount => residents.Count;
        public string ProspectiveResidenceId { get; private set; }
        public int CumulativeArrivals { get; private set; }
        public int CumulativeRelocations { get; private set; }
        public int CumulativeDepartures { get; private set; }
        public int PeakResidents { get; private set; } = StartingResidents;
        public int NextResidentNumber => nextResidentNumber;

        public void UpdateNavigation(RoomNavigationMap navigationMap)
        {
            navigation = navigationMap;
        }

        // Uses the settlement's capacity-aware assignment without changing residents,
        // their warnings, or the live navigation graph.
        public ResidentCommutePreview PreviewCommute(RoomNavigationMap candidateNavigation)
        {
            if (candidateNavigation == null)
            {
                throw new ArgumentNullException(nameof(candidateNavigation));
            }

            var officeUse = specs.Values.Where(room => room.Type == RoomType.Office)
                .ToDictionary(room => room.Id, _ => 0);
            var shopUse = specs.Values.Where(room => room.Type == RoomType.Canteen)
                .ToDictionary(room => room.Id, _ => 0);
            var working = 0;
            var production = 0f;
            foreach (var resident in residents.OrderBy(item => item.id, StringComparer.Ordinal))
            {
                if (!TryAssign(resident.residenceId, officeUse, shopUse,
                        out var assignment, candidateNavigation))
                {
                    continue;
                }

                officeUse[assignment.OfficeId]++;
                shopUse[assignment.FoodShopId]++;
                working++;
                production += assignment.Efficiency;
            }

            return new ResidentCommutePreview(working, production);
        }

        // Planned full-cycle assignments, not the number of people physically
        // standing in a room at this instant. Uses the same capacity-aware
        // ordering as PreviewCommute and the end-of-day settlement.
        public ResidentFacilityUsePreview PreviewFacilityUse(
            RoomNavigationMap candidateNavigation = null)
        {
            candidateNavigation ??= navigation;
            if (candidateNavigation == null)
                throw new ArgumentNullException(nameof(candidateNavigation));

            var officeUse = specs.Values.Where(room => room.Type == RoomType.Office)
                .ToDictionary(room => room.Id, _ => 0);
            var shopUse = specs.Values.Where(room => room.Type == RoomType.Canteen)
                .ToDictionary(room => room.Id, _ => 0);
            foreach (var resident in residents.OrderBy(item => item.id, StringComparer.Ordinal))
            {
                if (!TryAssign(resident.residenceId, officeUse, shopUse,
                        out var assignment, candidateNavigation))
                    continue;
                officeUse[assignment.OfficeId]++;
                shopUse[assignment.FoodShopId]++;
            }
            return new ResidentFacilityUsePreview(officeUse, shopUse);
        }

        public bool TryGetRoute(string residentId, out string residenceId,
            out string officeId, out string foodShopId)
        {
            residenceId = officeId = foodShopId = string.Empty;
            var resident = residents.FirstOrDefault(item => item.id == residentId);
            if (resident == null)
            {
                return false;
            }

            residenceId = resident.residenceId;
            var officeUse = specs.Values.Where(room => room.Type == RoomType.Office)
                .ToDictionary(room => room.Id, _ => 0);
            var shopUse = specs.Values.Where(room => room.Type == RoomType.Canteen)
                .ToDictionary(room => room.Id, _ => 0);
            // Preview the same capacity-aware allocation used at settlement. A
            // separate one-person search would send everyone to one office.
            foreach (var candidate in residents.OrderBy(item => item.id, StringComparer.Ordinal))
            {
                if (!TryAssign(candidate.residenceId, officeUse, shopUse, out var assignment))
                {
                    if (candidate == resident)
                    {
                        return false;
                    }
                    continue;
                }

                officeUse[assignment.OfficeId]++;
                shopUse[assignment.FoodShopId]++;
                if (candidate != resident)
                {
                    continue;
                }
                officeId = assignment.OfficeId;
                foodShopId = assignment.FoodShopId;
                return true;
            }
            return false;
        }

        public ResidentRouteLegs PreviewRouteLegs(string residentId)
        {
            var resident = residents.FirstOrDefault(item => item.id == residentId);
            if (resident == null || navigation == null)
                return default;
            if (TryGetRoute(residentId, out _, out _, out _))
                return new ResidentRouteLegs(true, true, true);

            // This is only the first missing leg for the head-up display. The
            // capacity-aware full-cycle assignment remains the settlement rule.
            var work = false;
            var meal = false;
            foreach (var office in specs.Values.Where(item => item.Type == RoomType.Office))
            {
                if (!TryDistance(navigation, resident.residenceId, office.Id, out var workDistance) ||
                    LegEfficiency(workDistance, false) <= 0f)
                    continue;
                work = true;
                foreach (var shop in specs.Values.Where(item => item.Type == RoomType.Canteen))
                {
                    if (!TryDistance(navigation, office.Id, shop.Id, out var foodDistance) ||
                        LegEfficiency(foodDistance, false) <= 0f)
                        continue;
                    meal = true;
                    if (TryDistance(navigation, shop.Id, resident.residenceId, out _))
                        return new ResidentRouteLegs(true, true, true,
                            assignmentBlocked: true);
                }
            }
            return new ResidentRouteLegs(work, meal, false);
        }

        public ResidentDayReport CompleteDay(
            int dayNumber,
            int humanFunctionPercent,
            bool bothFoodShopsOperating)
        {
            var offices = specs.Values
                .Where(room => room.Type == RoomType.Office)
                .OrderBy(room => room.Id, StringComparer.Ordinal)
                .Select(room => room.Id)
                .ToArray();
            var shops = specs.Values
                .Where(room => room.Type == RoomType.Canteen)
                .OrderBy(room => room.Id, StringComparer.Ordinal)
                .Select(room => room.Id)
                .ToArray();
            var officeUse = offices.ToDictionary(id => id, _ => 0);
            var shopUse = shops.ToDictionary(id => id, _ => 0);

            var production = 0f;
            var workingResidents = 0;
            foreach (var resident in residents.OrderBy(item => item.id, StringComparer.Ordinal))
            {
                if (TryAssign(resident.residenceId, officeUse, shopUse, out var assignment))
                {
                    resident.assignedOfficeId = assignment.OfficeId;
                    resident.assignedFoodShopId = assignment.FoodShopId;
                    resident.lastEfficiency = assignment.Efficiency;
                    resident.dissatisfiedDays = 0;
                    resident.leaveCountdownDays = 0;
                    officeUse[assignment.OfficeId]++;
                    shopUse[assignment.FoodShopId]++;
                    workingResidents++;
                    production += assignment.Efficiency;
                }
                else
                {
                    resident.assignedOfficeId = string.Empty;
                    resident.assignedFoodShopId = string.Empty;
                    resident.lastEfficiency = 0f;
                    resident.dissatisfiedDays++;
                }
            }

            var foodCost = shopUse.Values.Sum(ShopOperatingCost);
            // A complete home-work-meal-home route resets the streak. A third
            // consecutive failed day makes that resident leave immediately;
            // an automatic relocation must not silently cancel this rule.
            var departing = residents.Where(item => item.dissatisfiedDays >= 3)
                .OrderBy(item => item.id, StringComparer.Ordinal).ToArray();
            foreach (var resident in departing)
            {
                residents.Remove(resident);
            }
            CumulativeDepartures += departing.Length;
            var departedId = departing.Length > 0 ? departing[0].id : string.Empty;

            var arrivalResidenceId = AdvanceArrival(
                humanFunctionPercent,
                bothFoodShopsOperating);

            return new ResidentDayReport(
                dayNumber,
                workingResidents,
                production,
                foodCost,
                arrivalResidenceId,
                string.Empty,
                departedId);
        }

        public void Restore(
            IEnumerable<ResidentSaveData> savedResidents,
            string prospectiveResidenceId,
            int nextNumber,
            int arrivals,
            int relocations,
            int departures,
            int peakResidents)
        {
            var restored = (savedResidents ?? Array.Empty<ResidentSaveData>())
                .Where(item => item != null && specs.TryGetValue(item.residenceId, out var room) &&
                               room.Type == RoomType.Residence)
                .Select(item => new ResidentState
                {
                    id = item.id,
                    residenceId = item.residenceId,
                    assignedOfficeId = item.assignedOfficeId,
                    assignedFoodShopId = item.assignedFoodShopId,
                    dissatisfiedDays = Math.Max(0, item.dissatisfiedDays),
                    leaveCountdownDays = 0,
                    lastEfficiency = Mathf.Clamp01(item.lastEfficiency)
                })
                .GroupBy(item => item.residenceId)
                .Select(group => group.First())
                .Take(MaximumResidents)
                .ToList();

            residents.Clear();
            if (restored.Count == 0)
            {
                Reset();
                return;
            }

            residents.AddRange(restored);
            ProspectiveResidenceId = IsVacantResidence(prospectiveResidenceId)
                ? prospectiveResidenceId
                : string.Empty;
            nextResidentNumber = Math.Max(restored.Count + 1, nextNumber);
            CumulativeArrivals = Math.Max(0, arrivals);
            CumulativeRelocations = Math.Max(0, relocations);
            CumulativeDepartures = Math.Max(0, departures);
            PeakResidents = Math.Max(residents.Count, peakResidents);
        }

        public List<ResidentSaveData> ExportResidents()
        {
            return residents.Select(item => new ResidentSaveData
            {
                id = item.id,
                residenceId = item.residenceId,
                assignedOfficeId = item.assignedOfficeId,
                assignedFoodShopId = item.assignedFoodShopId,
                dissatisfiedDays = item.dissatisfiedDays,
                leaveCountdownDays = item.leaveCountdownDays,
                lastEfficiency = item.lastEfficiency
            }).ToList();
        }

        public void Reset()
        {
            residents.Clear();
            var residences = ResidenceIds().Take(StartingResidents).ToArray();
            for (var index = 0; index < residences.Length; index++)
            {
                residents.Add(new ResidentState
                {
                    id = $"R{index + 1:000}",
                    residenceId = residences[index],
                    lastEfficiency = 1f
                });
            }

            nextResidentNumber = residents.Count + 1;
            ProspectiveResidenceId = string.Empty;
            CumulativeArrivals = 0;
            CumulativeRelocations = 0;
            CumulativeDepartures = 0;
            PeakResidents = residents.Count;
        }

        private bool TryAssign(
            string residenceId,
            IReadOnlyDictionary<string, int> officeUse,
            IReadOnlyDictionary<string, int> shopUse,
            out Assignment best,
            RoomNavigationMap candidateNavigation = null)
        {
            var routeMap = candidateNavigation ?? navigation;
            best = default;
            var found = false;
            foreach (var office in officeUse.Keys.OrderBy(id => id, StringComparer.Ordinal))
            {
                if (officeUse[office] >= OfficeCapacity ||
                    !TryDistance(routeMap, residenceId, office, out var workDistance))
                {
                    continue;
                }

                foreach (var shop in shopUse.Keys.OrderBy(id => id, StringComparer.Ordinal))
                {
                    if (shopUse[shop] >= FoodShopCapacity ||
                        !TryDistance(routeMap, office, shop, out var foodDistance) ||
                        !TryDistance(routeMap, shop, residenceId, out var returnDistance))
                    {
                        continue;
                    }

                    var carEnabled = HasGarageAccess(routeMap, residenceId) &&
                                     HasGarageAccess(routeMap, office) &&
                                     HasGarageAccess(routeMap, shop);
                    var squareSupport = HasSharedCommunitySquareAccess(routeMap, office, shop);
                    var workEfficiency = LegEfficiency(workDistance, carEnabled, squareSupport);
                    var foodEfficiency = LegEfficiency(foodDistance, carEnabled);
                    if (workEfficiency <= 0f || foodEfficiency <= 0f)
                    {
                        continue;
                    }

                    var candidate = new Assignment(
                        office,
                        shop,
                        CommunitySquareEfficiency(squareSupport,
                            Mathf.Min(workEfficiency, foodEfficiency)),
                        workDistance + foodDistance + returnDistance);
                    if (!found || candidate.IsBetterThan(best))
                    {
                        best = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        private string AdvanceArrival(int humanFunctionPercent, bool bothFoodShopsOperating)
        {
            var qualifies = humanFunctionPercent >= 75 &&
                            bothFoodShopsOperating &&
                            residents.Count < MaximumResidents;
            if (!qualifies)
            {
                ProspectiveResidenceId = string.Empty;
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(ProspectiveResidenceId))
            {
                if (!IsVacantResidence(ProspectiveResidenceId))
                {
                    ProspectiveResidenceId = string.Empty;
                    return string.Empty;
                }

                var arrivalResidence = ProspectiveResidenceId;
                residents.Add(new ResidentState
                {
                    id = $"R{nextResidentNumber++:000}",
                    residenceId = arrivalResidence,
                    lastEfficiency = 1f
                });
                ProspectiveResidenceId = string.Empty;
                CumulativeArrivals++;
                PeakResidents = Math.Max(PeakResidents, residents.Count);
                return arrivalResidence;
            }

            ProspectiveResidenceId = FindSuitableVacantResidence();
            return string.Empty;
        }

        private string FindSuitableVacantResidence()
        {
            foreach (var residence in ResidenceIds().Where(IsVacantResidence))
            {
                var officeUse = specs.Values
                    .Where(room => room.Type == RoomType.Office)
                    .ToDictionary(room => room.Id, _ => 0);
                var shopUse = specs.Values
                    .Where(room => room.Type == RoomType.Canteen)
                    .ToDictionary(room => room.Id, _ => 0);
                if (TryAssign(residence, officeUse, shopUse, out _))
                {
                    return residence;
                }
            }

            return string.Empty;
        }

        private bool IsVacantResidence(string roomId)
        {
            return !string.IsNullOrEmpty(roomId) &&
                   specs.TryGetValue(roomId, out var room) &&
                   room.Type == RoomType.Residence &&
                   residents.All(item => item.residenceId != roomId);
        }

        private IEnumerable<string> ResidenceIds()
        {
            return specs.Values
                .Where(room => room.Type == RoomType.Residence)
                .OrderBy(room => room.Id, StringComparer.Ordinal)
                .Select(room => room.Id);
        }

        private bool HasGarageAccess(RoomNavigationMap routeMap, string roomId)
        {
            return routeMap.NeighboursOf(roomId)
                .Any(neighbour => specs.TryGetValue(neighbour, out var room) &&
                                  room.Type == RoomType.Garage);
        }

        private bool HasSharedCommunitySquareAccess(RoomNavigationMap routeMap,
            string officeId, string foodShopId)
        {
            var shopNeighbours = new HashSet<string>(routeMap.NeighboursOf(foodShopId));
            return routeMap.NeighboursOf(officeId)
                .Any(neighbour => shopNeighbours.Contains(neighbour) &&
                                  specs.TryGetValue(neighbour, out var room) &&
                                  room.Type == RoomType.CommunitySquare);
        }

        private static float CommunitySquareEfficiency(bool squareSupport, float baseEfficiency)
        {
            // A shared square improves a long, viable journey; it does not
            // replace a path to either destination.
            return squareSupport && baseEfficiency > 0f && baseEfficiency < 1f
                ? Mathf.Min(1f, baseEfficiency + 0.25f)
                : baseEfficiency;
        }

        private static bool TryDistance(RoomNavigationMap routeMap, string from, string to, out int distance)
        {
            return routeMap.TryFindNearestReachable(from, new[] { to }, out _, out distance);
        }

        private static float LegEfficiency(int distance, bool carEnabled,
            bool squareSupported = false)
        {
            var fullRange = carEnabled ? 3 : 2;
            // When work and food share a community square, a rest stop makes
            // one extra step to work feasible without creating a new doorway.
            var maximumRange = (carEnabled ? 5 : 4) + (squareSupported ? 1 : 0);
            return distance <= fullRange
                ? 1f
                : distance <= maximumRange
                    ? 0.5f
                    : 0f;
        }

        private static int ShopOperatingCost(int occupancy)
        {
            return occupancy switch
            {
                <= 0 => 0,
                <= 2 => 1,
                _ => 2
            };
        }

        private readonly struct Assignment
        {
            public Assignment(string officeId, string foodShopId, float efficiency, int totalDistance)
            {
                OfficeId = officeId;
                FoodShopId = foodShopId;
                Efficiency = efficiency;
                TotalDistance = totalDistance;
            }

            public string OfficeId { get; }
            public string FoodShopId { get; }
            public float Efficiency { get; }
            public int TotalDistance { get; }

            public bool IsBetterThan(Assignment other)
            {
                if (!Mathf.Approximately(Efficiency, other.Efficiency))
                {
                    return Efficiency > other.Efficiency;
                }
                if (TotalDistance != other.TotalDistance)
                {
                    return TotalDistance < other.TotalDistance;
                }

                var officeComparison = string.CompareOrdinal(OfficeId, other.OfficeId);
                return officeComparison < 0 ||
                       officeComparison == 0 &&
                       string.CompareOrdinal(FoodShopId, other.FoodShopId) < 0;
            }
        }
    }
}
