using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum ConstructionRoomIssue
    {
        Waste,
        EmptyHome,
        MissingWork,
        MissingMeal,
        WildlifeHungry,
        Route,
        TrafficRisk
    }

    // A room can show several concrete problems at once. Activity and cleanup
    // problems clear from the last settled day, while route access and traffic
    // exposure use the current board. Spare capacity is not a problem.
    public static class ConstructionRoomIssueModel
    {
        public static IReadOnlyList<ConstructionRoomIssue> ForTile(
            ConstructionRunModel run, ConstructionTileData tile,
            ConstructionHumanDayResult human,
            ConstructionEcologyDayResult ecology,
            ConstructionWasteAvailability waste)
        {
            var issues = new List<ConstructionRoomIssue>();
            if (run == null || tile == null) return issues;
            if ((tile.category is ConstructionCategory.Residence or
                    ConstructionCategory.Restaurant or ConstructionCategory.Supermarket) &&
                run.WasteBacklog(tile.id) > 0)
                issues.Add(ConstructionRoomIssue.Waste);

            if (tile.category == ConstructionCategory.Residence)
            {
                var occupied = run.HasResident(tile.id);
                if (!occupied)
                    issues.Add(ConstructionRoomIssue.EmptyHome);
                // A newly built home has no settled day yet, so its forecast
                // can introduce an issue. Once a day is observed, only that
                // resident's actual work and meal can clear the old issue.
                var resident = run.LastHumanDay?.residents?.FirstOrDefault(
                    person => person.homeTileId == tile.id) ??
                    human?.residents?.FirstOrDefault(person =>
                        person.homeTileId == tile.id);
                if (occupied && resident != null)
                {
                    if (!resident.worked)
                        issues.Add(ConstructionRoomIssue.MissingWork);
                    if (!resident.ate)
                        issues.Add(ConstructionRoomIssue.MissingMeal);
                }
            }
            else if (tile.category == ConstructionCategory.Green &&
                     ecology != null)
            {
                // A fresh habitat uses the forecast until its first day. An
                // existing habitat keeps its observed hunger state until the
                // animals actually feed at a later settlement.
                var observed = run.LastEcologyDay;
                if (observed != null && tile.builtDay <= observed.day)
                    ecology = observed;
                var squirrelsHere = run.VisibleSquirrelHomeTiles(
                    ecology.squirrelsPresent).Count(home => home.id == tile.id);
                var fedHere = ecology.squirrelMealHomeTileIds?.Count(id =>
                    id == tile.id) ?? 0;
                var hungrySquirrel = squirrelsHere > fedHere;
                var hungryPigeon = run.UsesGreenPatches
                    ? run.PigeonGroups(ecology.pigeonsPresent)
                        .Any(group => group.Origin.id == tile.id &&
                            (ecology.pigeonMealHomeTileIds?.Count(id =>
                                id == group.Origin.id) ?? 0) < group.Count)
                    : ecology.pigeonsPresent > ecology.pigeonsFed &&
                      ecology.pigeonHabitatTileId == tile.id;
                var hungryHedgehog = run.UsesGreenPatches
                    ? ecology.hedgehogHomeTileIds.Contains(tile.id) &&
                      !ecology.hedgehogMealHomeTileIds.Contains(tile.id)
                    : ecology.hedgehogPresent && !ecology.hedgehogFed &&
                      ecology.hedgehogHabitatTileId == tile.id;
                if (hungrySquirrel || hungryPigeon || hungryHedgehog)
                    issues.Add(ConstructionRoomIssue.WildlifeHungry);
            }
            else if (tile.category == ConstructionCategory.Street &&
                     run.StreetAdjacentWildlifeCount(tile.id) > 0)
                issues.Add(ConstructionRoomIssue.TrafficRisk);

            if ((tile.category is ConstructionCategory.Workshop or
                    ConstructionCategory.Restaurant or
                    ConstructionCategory.Supermarket or
                    ConstructionCategory.Square) &&
                human != null)
            {
                var utilization = ConstructionTileUtilizationModel.Evaluate(
                    tile, run, human, ecology, waste);
                if (utilization.Reason is
                    ConstructionUtilizationReason.UnreachableServiceDemand or
                    ConstructionUtilizationReason.SquareUnreachable)
                    issues.Add(ConstructionRoomIssue.Route);
            }
            return issues;
        }
    }
}
