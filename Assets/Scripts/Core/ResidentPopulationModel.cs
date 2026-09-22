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
        Move,
        LeaveCity
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

        public ResidentWarningState WarningState => leaveCountdownDays > 0
            ? ResidentWarningState.LeaveCity
            : dissatisfiedDays >= 2
                ? ResidentWarningState.Move
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
            float production,
            int foodServiceCost,
            string arrivalResidenceId,
            string relocatedResidentId,
            string departureResidentId)
        {
            DayNumber = dayNumber;
            Production = production;
            FoodServiceCost = foodServiceCost;
            ArrivalResidenceId = arrivalResidenceId;
            RelocatedResidentId = relocatedResidentId;
            DepartureResidentId = departureResidentId;
        }

        public int DayNumber { get; }
        public float Production { get; }
        public int FoodServiceCost { get; }
        public string ArrivalResidenceId { get; }
        public string RelocatedResidentId { get; }
        public string DepartureResidentId { get; }
        public bool ResidentArrived => !string.IsNullOrEmpty(ArrivalResidenceId);
        public bool ResidentRelocated => !string.IsNullOrEmpty(RelocatedResidentId);
        public bool ResidentDeparted => !string.IsNullOrEmpty(DepartureResidentId);
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
            var relocatedId = TryRelocateOneResident();
            var departedId = string.Empty;
            if (string.IsNullOrEmpty(relocatedId))
            {
                departedId = AdvanceOneLeaveCityCountdown();
            }

            var arrivalResidenceId = AdvanceArrival(
                humanFunctionPercent,
                bothFoodShopsOperating);

            return new ResidentDayReport(
                dayNumber,
                production,
                foodCost,
                arrivalResidenceId,
                relocatedId,
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
                    leaveCountdownDays = Math.Max(0, item.leaveCountdownDays),
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
            out Assignment best)
        {
            best = default;
            var found = false;
            foreach (var office in officeUse.Keys.OrderBy(id => id, StringComparer.Ordinal))
            {
                if (officeUse[office] >= OfficeCapacity ||
                    !TryDistance(residenceId, office, out var workDistance))
                {
                    continue;
                }

                foreach (var shop in shopUse.Keys.OrderBy(id => id, StringComparer.Ordinal))
                {
                    if (shopUse[shop] >= FoodShopCapacity ||
                        !TryDistance(office, shop, out var foodDistance))
                    {
                        continue;
                    }

                    var carEnabled = HasGarageAccess(residenceId) &&
                                     HasGarageAccess(office) &&
                                     HasGarageAccess(shop);
                    var workEfficiency = LegEfficiency(workDistance, carEnabled);
                    var foodEfficiency = LegEfficiency(foodDistance, carEnabled);
                    if (workEfficiency <= 0f || foodEfficiency <= 0f)
                    {
                        continue;
                    }

                    var candidate = new Assignment(
                        office,
                        shop,
                        Mathf.Min(workEfficiency, foodEfficiency),
                        workDistance + foodDistance);
                    if (!found || candidate.IsBetterThan(best))
                    {
                        best = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        private string TryRelocateOneResident()
        {
            foreach (var resident in residents
                         .Where(item => item.dissatisfiedDays >= 3 && item.leaveCountdownDays == 0)
                         .OrderByDescending(item => item.dissatisfiedDays)
                         .ThenBy(item => item.id, StringComparer.Ordinal))
            {
                var vacant = FindSuitableVacantResidence();
                if (!string.IsNullOrEmpty(vacant))
                {
                    resident.residenceId = vacant;
                    resident.dissatisfiedDays = 0;
                    resident.leaveCountdownDays = 0;
                    CumulativeRelocations++;
                    return resident.id;
                }

                resident.leaveCountdownDays = 2;
                return string.Empty;
            }

            return string.Empty;
        }

        private string AdvanceOneLeaveCityCountdown()
        {
            var resident = residents
                .Where(item => item.leaveCountdownDays > 0 && item.dissatisfiedDays > 3)
                .OrderByDescending(item => item.dissatisfiedDays)
                .ThenBy(item => item.id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (resident == null)
            {
                return string.Empty;
            }

            resident.leaveCountdownDays--;
            if (resident.leaveCountdownDays > 0)
            {
                return string.Empty;
            }

            residents.Remove(resident);
            CumulativeDepartures++;
            return resident.id;
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

        private bool HasGarageAccess(string roomId)
        {
            return navigation.NeighboursOf(roomId)
                .Any(neighbour => specs.TryGetValue(neighbour, out var room) &&
                                  room.Type == RoomType.Garage);
        }

        private bool TryDistance(string from, string to, out int distance)
        {
            return navigation.TryFindNearestReachable(from, new[] { to }, out _, out distance);
        }

        private static float LegEfficiency(int distance, bool carEnabled)
        {
            var fullRange = carEnabled ? 3 : 2;
            var maximumRange = carEnabled ? 5 : 4;
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
