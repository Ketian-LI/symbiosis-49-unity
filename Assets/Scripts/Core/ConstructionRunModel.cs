using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum ConstructionBuildFailure
    {
        None,
        LockedByStory,
        InvalidPlacement,
        RunFinished
    }

    [Serializable]
    public sealed class ConstructionRunSaveData
    {
        public ConstructionBoardSaveData board;
        public ConstructionStorySaveData story;
        public ConstructionScoreSaveData score;
        public ConstructionFoodSaveData food;
        public ConstructionWasteSaveData waste;
        public ConstructionEcologyDayResult lastEcologyDay;
        public ConstructionHumanDayResult lastHumanDay;
        public ConstructionWasteDayResult lastWasteDay;
        public ConstructionDayForecast lastForecast;
        public int currentDay;
        public bool finished;
        public string pigeonTileId;
        public int pigeonArrivalDay;
        public int firstResidentMealDay;
    }

    [Serializable]
    public sealed class ConstructionDayForecast
    {
        public int day;
        public int animalMeals;
        public int completedWorkCycles;
        public int wasteCleared;
        public List<string> blockedHomeTileIds = new();
        public List<string> blockedFoodTileIds = new();
    }

    // The new construction rules deliberately do not depend on RoomLayoutData.All.
    // A presenter/simulation may read only Board.BuiltTiles for this mode.
    public sealed class ConstructionRunModel
    {
        private ConstructionBoardModel board;
        private ConstructionStoryModel story;
        private ConstructionScoreLedger score;
        private ConstructionFoodModel food;
        private ConstructionWasteModel waste;

        public int BuiltCount => board.BuiltCount;
        public bool IsFull => board.IsFull;
        public IReadOnlyList<ConstructionTileData> BuiltTiles => board.BuiltTiles;
        public ConstructionStoryStage StoryStage => story.Stage;
        public int AnimalScore => score.AnimalScore;
        public int HumanScore => score.HumanScore;
        public int CurrentDay { get; private set; }
        public bool IsFinished { get; private set; }
        public string PigeonTileId { get; private set; }
        public int PigeonArrivalDay { get; private set; }
        public int FirstResidentMealDay { get; private set; }
        public ConstructionEcologyDayResult LastEcologyDay { get; private set; }
        public ConstructionHumanDayResult LastHumanDay { get; private set; }
        public ConstructionWasteDayResult LastWasteDay { get; private set; }
        public ConstructionDayForecast LastForecast { get; private set; }
        public bool FirstResidentAte => story.ResidentAte;
        public bool HasConnectedMeadow => ConnectedMeadows().Any();
        public string FirstResidentHomeTileId => board.FirstResidence?.id;
        public bool FirstResidentHasRestaurantAccess =>
            board.FirstResidenceCanReachService(ConstructionCategory.Restaurant);
        public IReadOnlyList<ConstructionTileData> FirstResidentRestaurantRoute =>
            board.FirstResidenceServiceRoute(ConstructionCategory.Restaurant);

        public ConstructionTileData At(int column, int row) => board.At(column, row);
        public bool IsUnlocked(ConstructionCategory category) =>
            story.IsUnlocked(category);
        public ConstructionPlacementFailure CheckPlacement(int column, int row,
            ConstructionCategory category, GreenPlanting planting = GreenPlanting.None) =>
            board.CheckPlacement(column, row, category, planting);
        public int AdjacentGreenCount(string residenceId) =>
            board.AdjacentGreenCount(residenceId);
        public int ConnectedGreenCount(string greenId) =>
            board.ConnectedGreenCount(greenId);
        public int FoodStock(string tileId) => food.Stock(tileId);
        public int WasteBacklog(string tileId) => waste.Backlog(tileId);
        public bool ResidenceCanReachFood(string residenceId) =>
            board.ResidenceServiceRoute(residenceId,
                ConstructionCategory.Restaurant).Count > 0 ||
            board.ResidenceServiceRoute(residenceId,
                ConstructionCategory.Supermarket).Count > 0;
        public bool ResidenceCanReachWork(string residenceId) =>
            board.ResidenceServiceRoute(residenceId,
                ConstructionCategory.Workshop).Count > 0;
        public ConstructionHumanDayResult PreviewHumanDay()
        {
            var availability = waste.PreviewAfterCleanup(board);
            return ConstructionHumanModel.SettleDay(CurrentDay, board,
                availability.BlockedHomes, availability.BlockedFoodServices);
        }
        public ConstructionWasteAvailability PreviewWasteAvailability() =>
            waste.PreviewAfterCleanup(board);
        public ConstructionEcologyDayResult PreviewEcologyDay() =>
            food.PreviewDay(CurrentDay, board, PigeonTileId);
        public IReadOnlyList<ConstructionTileData> PreviewSquirrelFoodRoute()
        {
            var target = PreviewEcologyDay().squirrelFoodTileId;
            return board.GreenRouteBetween(ConstructionBoardModel.StarterOakId,
                target);
        }
        public IReadOnlyList<ConstructionTileData> PreviewPigeonFoodRoute()
        {
            var target = PreviewEcologyDay().pigeonFoodTileId;
            return board.GreenRouteBetween(PigeonTileId, target);
        }
        public IReadOnlyList<ConstructionTileData> ResidenceRouteTo(
            string residenceId, string serviceTileId) =>
            board.ResidenceServiceRouteTo(residenceId, serviceTileId);
        public int FoodCapacity(GreenPlanting planting) =>
            ConstructionFoodModel.Capacity(planting);
        public int GreenGrowthStage(string tileId)
        {
            var tile = board.BuiltTiles.FirstOrDefault(item => item.id == tileId);
            return ConstructionFoodModel.MaturityStage(tile, CurrentDay);
        }
        public int GreenDailyYield(string tileId)
        {
            var tile = board.BuiltTiles.FirstOrDefault(item => item.id == tileId);
            return tile == null ? 0 : ConstructionFoodModel.DailyYield(tile, CurrentDay);
        }

        public ConstructionRunModel()
        {
            board = new ConstructionBoardModel();
            story = new ConstructionStoryModel();
            score = new ConstructionScoreLedger();
            food = new ConstructionFoodModel();
            waste = new ConstructionWasteModel();
            CurrentDay = 1;
            story.Observe(board);
        }

        public void RecordSquirrelMeal()
        {
            if (IsFinished || story.Stage != ConstructionStoryStage.WatchSquirrelEat) return;
            story.RecordSquirrelMeal();
            story.Observe(board);
        }

        public void RecordDirectAccessBlocked()
        {
            if (IsFinished || story.Stage != ConstructionStoryStage.AwaitAccessProblem) return;
            story.RecordDirectAccessBlocked();
            story.Observe(board);
        }

        public void RecordRestaurantCrowded()
        {
            if (IsFinished || story.Stage != ConstructionStoryStage.AwaitRestaurantCrowding) return;
            story.RecordRestaurantCrowded();
            story.Observe(board);
        }

        public bool TryBuild(int column, int row, ConstructionCategory category,
            GreenPlanting planting, out ConstructionTileData built,
            out ConstructionBuildFailure failure,
            out ConstructionPlacementFailure placementFailure)
        {
            built = null;
            placementFailure = ConstructionPlacementFailure.None;
            if (IsFinished || board.IsFull)
            {
                failure = ConstructionBuildFailure.RunFinished;
                return false;
            }
            story.Observe(board);
            if (!story.IsUnlocked(category))
            {
                failure = ConstructionBuildFailure.LockedByStory;
                return false;
            }
            if (!board.TryBuild(column, row, category, planting,
                    out built, out placementFailure, CurrentDay))
            {
                if (placementFailure ==
                        ConstructionPlacementFailure.NeedsHomeOrConnectedStreet &&
                    story.Stage == ConstructionStoryStage.AwaitAccessProblem)
                {
                    story.RecordDirectAccessBlocked();
                    story.Observe(board);
                }
                failure = ConstructionBuildFailure.InvalidPlacement;
                return false;
            }
            story.Observe(board);
            failure = ConstructionBuildFailure.None;
            return true;
        }

        // The playable construction screen calls this method: points come only
        // from food actually removed from a built green tile.
        public bool TrySimulateDay(out ConstructionEcologyDayResult ecology)
        {
            ecology = null;
            if (IsFinished || score.Days.Count != CurrentDay - 1 ||
                food.LastSettledDay >= CurrentDay) return false;
            var availability = waste.PreviewAfterCleanup(board);
            var expectedEcology = food.PreviewDay(CurrentDay, board, PigeonTileId);
            var expectedHuman = ConstructionHumanModel.SettleDay(CurrentDay, board,
                availability.BlockedHomes, availability.BlockedFoodServices);
            var forecast = new ConstructionDayForecast
            {
                day = CurrentDay,
                animalMeals = expectedEcology.AnimalMeals,
                completedWorkCycles = expectedHuman.CompletedWorkCycles,
                wasteCleared = availability.Cleared.Values.Sum(),
                blockedHomeTileIds = availability.BlockedHomes.OrderBy(id => id).ToList(),
                blockedFoodTileIds = availability.BlockedFoodServices.OrderBy(id => id).ToList()
            };
            ecology = food.SettleDay(CurrentDay, board, PigeonTileId);
            var human = ConstructionHumanModel.SettleDay(CurrentDay, board,
                availability.BlockedHomes, availability.BlockedFoodServices);
            var wasteDay = waste.SettleDay(CurrentDay, board, human, availability);
            if (ecology.squirrelAte && story.Stage ==
                ConstructionStoryStage.WatchSquirrelEat)
            {
                story.RecordSquirrelMeal();
                story.Observe(board);
            }
            if (!FinishDay(ecology.AnimalMeals, human.CompletedWorkCycles, human))
                throw new InvalidOperationException("Daily score rejected a settled ecology day.");
            if (!string.IsNullOrEmpty(ecology.pigeonFoodTileId))
                PigeonTileId = ecology.pigeonFoodTileId;
            LastEcologyDay = ecology;
            LastHumanDay = human;
            LastWasteDay = wasteDay;
            LastForecast = forecast;
            return true;
        }

        // Compatibility entrypoint for existing model tests and v1 saves. The
        // runtime UI must not supply guessed meal counts through this method.
        public bool TryEndDay(int animalMeals, int completedWorkCycles)
            => FinishDay(animalMeals, completedWorkCycles, null);

        private bool FinishDay(int animalMeals, int completedWorkCycles,
            ConstructionHumanDayResult observedHuman)
        {
            if (IsFinished || !score.TryRecordDay(CurrentDay, animalMeals,
                    completedWorkCycles)) return false;
            food.MarkDayWithoutSimulation(CurrentDay);
            waste.MarkDayWithoutSimulation(CurrentDay);
            LastEcologyDay = null;
            LastHumanDay = null;
            LastWasteDay = null;
            LastForecast = null;
            var nextDay = CurrentDay + 1;
            if (PigeonTileId == null && story.Stage == ConstructionStoryStage.GrowGreen)
            {
                var meadow = ConnectedMeadows().FirstOrDefault(tile =>
                    Math.Max(1, tile.builtDay) < nextDay);
                if (meadow != null)
                {
                    PigeonTileId = meadow.id;
                    PigeonArrivalDay = nextDay;
                    story.RecordNewWildlifeArrival();
                    story.Observe(board);
                }
            }
            if (!story.ResidentAte && story.Stage == ConstructionStoryStage.WatchResidentEat &&
                (observedHuman == null
                    ? board.FirstResidenceServiceRoute(ConstructionCategory.Restaurant,
                        CurrentDay).Count > 0
                    : observedHuman.residents.Any(person =>
                        person.homeTileId == FirstResidentHomeTileId && person.ate)))
            {
                FirstResidentMealDay = CurrentDay;
                story.RecordResidentMeal();
                story.Observe(board);
            }
            if (observedHuman?.restaurantRejectedForCapacity > 0 &&
                story.Stage == ConstructionStoryStage.AwaitRestaurantCrowding)
            {
                story.RecordRestaurantCrowded();
                story.Observe(board);
            }
            if (board.IsFull) IsFinished = true;
            else CurrentDay++;
            return true;
        }

        private IEnumerable<ConstructionTileData> ConnectedMeadows() =>
            board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId)
                .Where(tile => tile.planting == GreenPlanting.Meadow);

        public ConstructionRunSaveData Export() => new()
        {
            board = board.Export(),
            story = story.Export(),
            score = score.Export(),
            food = food.Export(),
            waste = waste.Export(),
            lastEcologyDay = LastEcologyDay,
            lastHumanDay = LastHumanDay,
            lastWasteDay = LastWasteDay,
            lastForecast = LastForecast,
            currentDay = CurrentDay,
            finished = IsFinished,
            pigeonTileId = PigeonTileId,
            pigeonArrivalDay = PigeonArrivalDay,
            firstResidentMealDay = FirstResidentMealDay
        };

        public static bool TryRestore(ConstructionRunSaveData saved,
            out ConstructionRunModel run)
        {
            run = null;
            if (saved == null ||
                !ConstructionBoardModel.TryRestore(saved.board, out var board) ||
                !ConstructionStoryModel.TryRestore(saved.story, board, out var story) ||
                !ConstructionScoreLedger.TryRestore(saved.score, out var score) ||
                board.BuiltTiles.Any(tile =>
                    (tile.builtDay == 0 ? 1 : tile.builtDay) > saved.currentDay) ||
                saved.currentDay < 1 ||
                story.NewWildlifeArrived != !string.IsNullOrEmpty(saved.pigeonTileId) ||
                (string.IsNullOrEmpty(saved.pigeonTileId)
                    ? saved.pigeonArrivalDay != 0
                    : saved.pigeonArrivalDay < 2 ||
                      saved.pigeonArrivalDay > saved.currentDay + (saved.finished ? 1 : 0) ||
                      !board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId)
                          .Any(tile => tile.id == saved.pigeonTileId &&
                              tile.planting == GreenPlanting.Meadow) ||
                      !board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId)
                          .Any(tile => tile.planting == GreenPlanting.Meadow &&
                              Math.Max(1, tile.builtDay) < saved.pigeonArrivalDay)) ||
                story.ResidentAte != (saved.firstResidentMealDay > 0) ||
                (saved.firstResidentMealDay != 0 &&
                    (saved.firstResidentMealDay > saved.currentDay ||
                     board.FirstResidenceServiceRoute(
                         ConstructionCategory.Restaurant,
                         saved.firstResidentMealDay).Count == 0)) ||
                (saved.finished && score.Days.Count == 0) ||
                (saved.finished && !board.IsFull) ||
                saved.currentDay != score.Days.Count + (saved.finished ? 0 : 1))
                return false;

            ConstructionFoodModel food;
            if (saved.food == null ||
                saved.food.lastSettledDay == 0 && saved.food.tiles?.Count == 0)
                food = ConstructionFoodModel.FromLegacySave(saved.currentDay,
                    saved.finished);
            else if (!ConstructionFoodModel.TryRestore(saved.food, board,
                         saved.currentDay, saved.finished, out food))
                return false;
            ConstructionWasteModel waste;
            if (saved.waste == null ||
                saved.waste.lastSettledDay == 0 && saved.waste.tiles?.Count == 0)
                waste = ConstructionWasteModel.FromLegacySave(saved.currentDay,
                    saved.finished);
            else if (!ConstructionWasteModel.TryRestore(saved.waste, board,
                         saved.currentDay, saved.finished, out waste))
                return false;
            // Unity JsonUtility materializes a null nested serializable class
            // as an all-default object; day zero represents "no result yet".
            var lastEcologyDay = saved.lastEcologyDay?.day > 0
                ? saved.lastEcologyDay : null;
            if (lastEcologyDay != null &&
                (lastEcologyDay.day != score.Days.Last().day ||
                 lastEcologyDay.AnimalMeals != score.Days.Last().animalMeals ||
                 lastEcologyDay.pigeonsFed < 0 ||
                 lastEcologyDay.pigeonsFed > ConstructionFoodModel.PigeonFlockSize))
                return false;
            var lastHumanDay = saved.lastHumanDay?.day > 0
                ? saved.lastHumanDay : null;
            if (lastHumanDay != null &&
                (lastHumanDay.day != score.Days.Last().day ||
                 lastHumanDay.CompletedWorkCycles !=
                     score.Days.Last().completedWorkCycles ||
                 lastHumanDay.restaurantServed < 0 ||
                 lastHumanDay.restaurantDemand < lastHumanDay.restaurantServed ||
                 lastHumanDay.restaurantRejectedForCapacity < 0 ||
                 lastHumanDay.restaurantRejectedForCapacity >
                     lastHumanDay.restaurantDemand - lastHumanDay.restaurantServed ||
                 lastHumanDay.restaurantRejectedForWaste < 0 ||
                 lastHumanDay.restaurantRejectedForWaste >
                     lastHumanDay.restaurantDemand - lastHumanDay.restaurantServed ||
                 lastHumanDay.residents == null ||
                 lastHumanDay.ResidentCount != board.BuiltTiles.Count(tile =>
                     tile.category == ConstructionCategory.Residence &&
                     tile.builtDay <= lastHumanDay.day) ||
                 lastHumanDay.residents.Any(person => person == null ||
                     !board.BuiltTiles.Any(tile => tile.id == person.homeTileId &&
                         tile.category == ConstructionCategory.Residence &&
                         tile.builtDay <= lastHumanDay.day)) ||
                 lastHumanDay.residents.Select(person => person.homeTileId)
                     .Distinct().Count() != lastHumanDay.ResidentCount))
                return false;
            var lastWasteDay = saved.lastWasteDay?.day > 0
                ? saved.lastWasteDay : null;
            if (lastWasteDay != null &&
                (score.Days.Count == 0 ||
                 lastWasteDay.day != score.Days.Last().day ||
                 lastWasteDay.changes == null ||
                 lastWasteDay.changes.Any(change => change == null ||
                     change.cleared < 0 || change.produced < 0 ||
                     change.remaining < 0 ||
                     !board.BuiltTiles.Any(tile => tile.id == change.tileId &&
                         tile.builtDay <= lastWasteDay.day &&
                         tile.category is ConstructionCategory.Residence or
                             ConstructionCategory.Restaurant or
                             ConstructionCategory.Supermarket))))
                return false;
            var lastForecast = saved.lastForecast?.day > 0
                ? saved.lastForecast : null;
            if (lastForecast != null &&
                (score.Days.Count == 0 || lastEcologyDay == null ||
                 lastHumanDay == null || lastWasteDay == null ||
                 lastForecast.day != score.Days.Last().day ||
                 lastForecast.animalMeals < 0 ||
                 lastForecast.animalMeals > 1 + ConstructionFoodModel.PigeonFlockSize ||
                 lastForecast.completedWorkCycles < 0 ||
                 lastForecast.completedWorkCycles >
                     board.BuiltTiles.Count(tile =>
                         tile.category == ConstructionCategory.Residence &&
                         tile.builtDay <= lastForecast.day) ||
                 lastForecast.wasteCleared < 0 ||
                 lastForecast.wasteCleared >
                     board.BuiltTiles.Count(tile =>
                         tile.category == ConstructionCategory.Waste &&
                         tile.builtDay <= lastForecast.day) *
                     ConstructionWasteModel.CleanupCapacity ||
                 lastForecast.blockedHomeTileIds == null ||
                 lastForecast.blockedFoodTileIds == null ||
                 lastForecast.blockedHomeTileIds.Distinct().Count() !=
                     lastForecast.blockedHomeTileIds.Count ||
                 lastForecast.blockedFoodTileIds.Distinct().Count() !=
                     lastForecast.blockedFoodTileIds.Count ||
                 lastForecast.blockedHomeTileIds.Any(id => !board.BuiltTiles.Any(tile =>
                     tile.id == id && tile.category == ConstructionCategory.Residence &&
                     tile.builtDay <= lastForecast.day)) ||
                 lastForecast.blockedFoodTileIds.Any(id => !board.BuiltTiles.Any(tile =>
                     tile.id == id && tile.category is
                         ConstructionCategory.Restaurant or
                         ConstructionCategory.Supermarket &&
                     tile.builtDay <= lastForecast.day))))
                return false;

            run = new ConstructionRunModel
            {
                board = board,
                story = story,
                score = score,
                food = food,
                waste = waste,
                CurrentDay = saved.currentDay,
                IsFinished = saved.finished,
                PigeonTileId = saved.pigeonTileId,
                PigeonArrivalDay = saved.pigeonArrivalDay,
                FirstResidentMealDay = saved.firstResidentMealDay,
                LastEcologyDay = lastEcologyDay,
                LastHumanDay = lastHumanDay,
                LastWasteDay = lastWasteDay,
                LastForecast = lastForecast
            };
            return true;
        }
    }
}
