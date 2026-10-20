using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum ConstructionGameMode { Fill, Endless }
    public enum ConstructionEndReason { None, BoardComplete, ResidentsGone, WildlifeGone }

    [Serializable]
    public sealed class ConstructionResidentSurvivalData
    {
        public string homeTileId;
        public int missedDays;
        public bool departed;
    }

    [Serializable]
    public sealed class ConstructionSurvivalSaveData
    {
        public int squirrels = 1;
        public int pigeons;
        public int predatedSquirrels;
        public int predatedPigeons;
        public int hedgehogNextArrivalShrubs;
        public bool foxEstablished;
        public int foxHungryDays;
        public int foxReturnDay;
        public int squirrelHungryDays;
        public int pigeonHungryDays;
        public int squirrelFedDays;
        public int pigeonFedDays;
        public List<ConstructionResidentSurvivalData> residents = new();
    }

    public enum ConstructionBuildFailure
    {
        None,
        LockedByStory,
        InvalidPlacement,
        DailyLimitReached,
        RunFinished,
        TypeLimitReached
    }

    [Serializable]
    public sealed class ConstructionRunSaveData
    {
        public ConstructionGameMode mode;
        public ConstructionEndReason endReason;
        public ConstructionSurvivalSaveData survival;
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
        public bool pigeonMeadowInspected;
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

    public sealed class ConstructionBuildImpact
    {
        public int animalMealsBefore;
        public int animalMealsAfter;
        public int workCyclesBefore;
        public int workCyclesAfter;
        public int residentsBefore;
        public int residentsAfter;
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
        private ConstructionSurvivalSaveData survival;

        public ConstructionGameMode Mode { get; private set; }
        public ConstructionEndReason EndReason { get; private set; }
        public int BoardCapacity => board.CurrentCapacity;
        public int ExpansionRings => board.ExpansionRings;
        public int MinCoordinate => board.MinCoordinate;
        public int MaxCoordinate => board.MaxCoordinate;
        public bool Contains(int column, int row) => board.Contains(column, row);
        public int ResidentPopulation => Mode == ConstructionGameMode.Endless
            ? board.BuiltTiles.Count(tile => tile.category == ConstructionCategory.Residence &&
                !survival.residents.Any(person => person.homeTileId == tile.id && person.departed))
            : board.BuiltTiles.Count(tile => tile.category == ConstructionCategory.Residence);
        public bool HasResident(string homeTileId) => board.BuiltTiles.Any(tile =>
            tile.id == homeTileId && tile.category == ConstructionCategory.Residence) &&
            (Mode != ConstructionGameMode.Endless || !survival.residents.Any(person =>
                person.homeTileId == homeTileId && person.departed));
        private string[] DepartedHomes => Mode == ConstructionGameMode.Endless
            ? survival.residents.Where(person => person.departed)
                .Select(person => person.homeTileId).ToArray()
            : Array.Empty<string>();

        public int BuiltCount => board.BuiltCount;
        public bool BuiltToday => board.BuiltTiles.Any(tile =>
            tile.id != ConstructionBoardModel.StarterOakId &&
            Math.Max(1, tile.builtDay) == CurrentDay);
        // In Fill Mode the starter oak is day one's scripted placement.
        // Every following day needs exactly one new room before settlement.
        public bool CanAdvanceFillDay => Mode == ConstructionGameMode.Fill && !IsFinished &&
            (CurrentDay == 1 || BuiltToday);
        public bool CanAdvanceConstructionDay => !IsFinished &&
            (CurrentDay == 1 || BuiltToday);
        public bool IsFull => board.IsFull;
        public IReadOnlyList<ConstructionTileData> BuiltTiles => board.BuiltTiles;
        // Fill-mode category caps total 80, allowing multiple ways to fill
        // the 49-cell board. Green, Residence and Square total only 44, so
        // a completed board must also include functional room types.
        // The starter oak counts toward the Green limit.
        public int CategoryLimit(ConstructionCategory category) =>
            Mode == ConstructionGameMode.Endless ? int.MaxValue : category switch
            {
                ConstructionCategory.Green => 21,
                ConstructionCategory.Residence => 12,
                ConstructionCategory.Workshop => 7,
                ConstructionCategory.Restaurant => 5,
                ConstructionCategory.Supermarket => 5,
                ConstructionCategory.Waste => 7,
                ConstructionCategory.Street => 12,
                ConstructionCategory.Square => 11,
                _ => throw new ArgumentOutOfRangeException(nameof(category))
            };
        public int CategoryBuiltCount(ConstructionCategory category) =>
            board.BuiltTiles.Count(tile => tile.category == category);
        public bool IsCategoryAtLimit(ConstructionCategory category) =>
            Mode == ConstructionGameMode.Fill &&
            CategoryBuiltCount(category) >= CategoryLimit(category);
        public ConstructionStoryStage StoryStage => story.Stage;
        public int AnimalScore => score.AnimalScore;
        public int HumanScore => score.HumanScore;
        public ConstructionFinalResult FinalResult => IsFinished
            ? score.FinalResult() : null;
        public int CurrentDay { get; private set; }
        public bool IsFinished { get; private set; }
        public string PigeonTileId { get; private set; }
        public int PigeonArrivalDay { get; private set; }
        public bool PigeonMeadowInspected { get; private set; }
        public int FirstResidentMealDay { get; private set; }
        public ConstructionEcologyDayResult LastEcologyDay { get; private set; }
        public ConstructionHumanDayResult LastHumanDay { get; private set; }
        public ConstructionWasteDayResult LastWasteDay { get; private set; }
        public ConstructionDayForecast LastForecast { get; private set; }
        public bool FirstResidentAte => story.ResidentAte;
        public bool HasConnectedMeadow => ConnectedMeadows().Any();
        public int SquirrelPopulation =>
            Mode == ConstructionGameMode.Endless ? survival.squirrels :
                Math.Max(0, ConstructionFoodModel.SquirrelPopulation(board,
                    CurrentDay) - survival.predatedSquirrels);
        public int PigeonPopulation =>
            Mode == ConstructionGameMode.Endless ? survival.pigeons :
                Math.Max(0, ConstructionFoodModel.PigeonPopulation(board,
                    CurrentDay, PigeonTileId) - survival.predatedPigeons);
        // These are habitat arrivals, not meals or score. Keep them derived
        // from the constructed board so loading a save cannot spawn wildlife
        // in an unbuilt legacy room.
        public ConstructionTileData HedgehogHabitat =>
            ConstructionFoodModel.MatureConnectedShrubCount(board, CurrentDay) >=
                Math.Max(1, survival.hedgehogNextArrivalShrubs)
                ? ConstructionFoodModel.HedgehogHabitat(board, CurrentDay) : null;
        public ConstructionTileData FoxHabitat =>
            CurrentDay < survival.foxReturnDay ? null :
                ConstructionFoodModel.FoxHabitat(board, CurrentDay,
                    survival.foxEstablished ? int.MaxValue : PigeonPopulation);
        public int MatureNearbyOakCount =>
            ConstructionFoodModel.MatureNearbyOakCount(board, CurrentDay);
        public int MatureConnectedMeadowCount =>
            ConstructionFoodModel.MatureConnectedMeadowCount(board,
                CurrentDay, PigeonTileId);
        public int MatureConnectedShrubCount =>
            ConstructionFoodModel.MatureConnectedShrubCount(board, CurrentDay);
        public string FirstResidentHomeTileId => board.FirstResidence?.id;
        public bool FirstResidentHasRestaurantAccess =>
            board.FirstResidenceCanReachService(ConstructionCategory.Restaurant);
        public IReadOnlyList<ConstructionTileData> FirstResidentRestaurantRoute =>
            board.FirstResidenceServiceRoute(ConstructionCategory.Restaurant);

        public ConstructionTileData At(int column, int row) => board.At(column, row);
        public bool IsUnlocked(ConstructionCategory category) =>
            story.IsUnlocked(category);
        public ConstructionPlacementFailure CheckPlacement(int column, int row,
            ConstructionCategory category, GreenPlanting planting = GreenPlanting.None)
        {
            var placement = board.CheckPlacement(column, row, category, planting);
            return placement == ConstructionPlacementFailure.None &&
                IsCategoryAtLimit(category)
                    ? ConstructionPlacementFailure.TypeLimitReached : placement;
        }
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
                availability.BlockedHomes, availability.BlockedFoodServices,
                DepartedHomes);
        }
        public ConstructionWasteAvailability PreviewWasteAvailability() =>
            waste.PreviewAfterCleanup(board);
        // Evaluate a staged build against a restored copy. Both sides use the
        // same day-settlement previews as the actual score, without spending
        // today's build or changing food, story or save state.
        public bool TryPreviewBuildImpact(int column, int row,
            ConstructionCategory category, GreenPlanting planting,
            out ConstructionBuildImpact impact)
        {
            impact = null;
            if (IsFinished || BuiltToday || !IsUnlocked(category) ||
                CheckPlacement(column, row, category, planting) !=
                    ConstructionPlacementFailure.None ||
                !TryRestore(Export(), out var projected) ||
                !projected.TryBuild(column, row, category, planting,
                    out _, out _, out _))
                return false;

            impact = new ConstructionBuildImpact
            {
                animalMealsBefore = PreviewEcologyDay().AnimalMeals,
                animalMealsAfter = projected.PreviewEcologyDay().AnimalMeals,
                workCyclesBefore = PreviewHumanDay().CompletedWorkCycles,
                workCyclesAfter = projected.PreviewHumanDay().CompletedWorkCycles,
                residentsBefore = ResidentPopulation,
                residentsAfter = projected.ResidentPopulation
            };
            return true;
        }
        public ConstructionEcologyDayResult PreviewEcologyDay()
        {
            var hunt = PlanPredation();
            var result = food.PreviewDay(CurrentDay, board, PigeonTileId,
                Math.Max(0, SquirrelPopulation -
                    (hunt.species == ConstructionPreySpecies.Squirrel ? 1 : 0)),
                Math.Max(0, PigeonPopulation -
                    (hunt.species == ConstructionPreySpecies.Pigeon ? 1 : 0)));
            MarkPredation(result, hunt);
            return result;
        }

        private ConstructionPredationPlan PlanPredation()
        {
            var den = FoxHabitat;
            return ConstructionFoodModel.PlanFoxHunt(board, CurrentDay,
                den?.id, PigeonTileId, PigeonPopulation, SquirrelPopulation,
                HedgehogHabitat?.id, den != null &&
                    survival.foxEstablished && survival.foxHungryDays >= 1);
        }

        private static void MarkPredation(ConstructionEcologyDayResult ecology,
            ConstructionPredationPlan hunt)
        {
            ecology.predatedSpecies = hunt.species;
            ecology.predationTileId = hunt.preyTileId;
            ecology.foxHuntRouteTileIds = hunt.routeTileIds.ToList();
        }
        public IReadOnlyList<ConstructionTileData> PreviewSquirrelFoodRoute()
        {
            var target = PreviewEcologyDay().squirrelFoodTileId;
            return board.GreenRouteBetween(ConstructionBoardModel.StarterOakId,
                target);
        }
        public IReadOnlyList<IReadOnlyList<ConstructionTileData>> PreviewSquirrelFoodRoutes()
            => PreviewEcologyDay().squirrelFoodTileIds.Select(target =>
                board.GreenRouteBetween(ConstructionBoardModel.StarterOakId, target))
                .ToArray();
        public IReadOnlyList<ConstructionTileData> PreviewPigeonFoodRoute()
        {
            var target = PreviewEcologyDay().pigeonFoodTileId;
            return board.GreenRouteBetween(PigeonTileId, target);
        }
        public IReadOnlyList<IReadOnlyList<ConstructionTileData>> PreviewPigeonFoodRoutes()
            => PreviewEcologyDay().pigeonFoodTileIds.Select(target =>
                board.GreenRouteBetween(PigeonTileId, target)).ToArray();
        public IReadOnlyList<ConstructionTileData> GreenFoodRouteTo(
            string originTileId, string foodTileId) =>
            board.GreenRouteBetween(originTileId, foodTileId);
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

        public ConstructionRunModel(ConstructionGameMode mode = ConstructionGameMode.Fill)
        {
            Mode = mode;
            survival = new ConstructionSurvivalSaveData();
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

        public bool InspectPigeonMeadow(string tileId)
        {
            if (IsFinished || story.Stage != ConstructionStoryStage.WelcomeResident ||
                string.IsNullOrEmpty(PigeonTileId) || tileId != PigeonTileId ||
                PigeonPopulation == 0)
                return false;
            PigeonMeadowInspected = true;
            return true;
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
            // An invalid attempt can still reveal an access problem in the
            // story, even if this day's one construction slot is already used.
            var check = board.CheckPlacement(column, row, category, planting);
            if (check == ConstructionPlacementFailure.None &&
                IsCategoryAtLimit(category))
            {
                placementFailure = ConstructionPlacementFailure.TypeLimitReached;
                failure = ConstructionBuildFailure.TypeLimitReached;
                return false;
            }
            if (check == ConstructionPlacementFailure.None && BuiltToday)
            {
                failure = ConstructionBuildFailure.DailyLimitReached;
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

        public bool TrySimulateFillDay(out ConstructionEcologyDayResult ecology)
        {
            ecology = null;
            return CanAdvanceFillDay && TrySimulateDay(out ecology);
        }

        public bool TrySimulateConstructionDay(out ConstructionEcologyDayResult ecology)
        {
            ecology = null;
            return CanAdvanceConstructionDay && TrySimulateDay(out ecology);
        }

        // Shared low-level settlement is also used by historical simulation
        // tests. The Fill Mode UI must use TrySimulateFillDay instead.
        // Points come only from food actually removed from a built green tile.
        public bool TrySimulateDay(out ConstructionEcologyDayResult ecology)
        {
            ecology = null;
            if (IsFinished || score.Days.Count != CurrentDay - 1 ||
                food.LastSettledDay >= CurrentDay) return false;
            var availability = waste.PreviewAfterCleanup(board);
            var expectedEcology = PreviewEcologyDay();
            var expectedHuman = ConstructionHumanModel.SettleDay(CurrentDay, board,
                availability.BlockedHomes, availability.BlockedFoodServices,
                DepartedHomes);
            var forecast = new ConstructionDayForecast
            {
                day = CurrentDay,
                animalMeals = expectedEcology.AnimalMeals,
                completedWorkCycles = expectedHuman.CompletedWorkCycles,
                wasteCleared = availability.Cleared.Values.Sum(),
                blockedHomeTileIds = availability.BlockedHomes.OrderBy(id => id).ToList(),
                blockedFoodTileIds = availability.BlockedFoodServices.OrderBy(id => id).ToList()
            };
            var foxPresent = FoxHabitat != null;
            var hunt = PlanPredation();
            if (foxPresent) survival.foxEstablished = true;
            ApplyPredation(hunt);
            ecology = food.SettleDay(CurrentDay, board, PigeonTileId,
                SquirrelPopulation, PigeonPopulation);
            MarkPredation(ecology, hunt);
            var human = ConstructionHumanModel.SettleDay(CurrentDay, board,
                availability.BlockedHomes, availability.BlockedFoodServices,
                DepartedHomes);
            var wasteDay = waste.SettleDay(CurrentDay, board, human, availability);
            if (ecology.squirrelAte && story.Stage ==
                ConstructionStoryStage.WatchSquirrelEat)
            {
                story.RecordSquirrelMeal();
                story.Observe(board);
            }
            if (!FinishDay(ecology.AnimalMeals, human.CompletedWorkCycles, human))
                throw new InvalidOperationException("Daily score rejected a settled ecology day.");
            if (Mode == ConstructionGameMode.Endless && !IsFinished)
                SettleSurvival(ecology, human);
            SettleFoxActivity(foxPresent, hunt, ecology.day);
            if (!string.IsNullOrEmpty(ecology.pigeonFoodTileId))
                PigeonTileId = ecology.pigeonFoodTileId;
            LastEcologyDay = ecology;
            LastHumanDay = human;
            LastWasteDay = wasteDay;
            LastForecast = forecast;
            return true;
        }

        private void ApplyPredation(ConstructionPredationPlan hunt)
        {
            switch (hunt.species)
            {
                case ConstructionPreySpecies.Pigeon:
                    if (Mode == ConstructionGameMode.Endless)
                    {
                        survival.pigeons--;
                        survival.pigeonFedDays = 0;
                    }
                    else survival.predatedPigeons++;
                    break;
                case ConstructionPreySpecies.Squirrel:
                    if (Mode == ConstructionGameMode.Endless)
                    {
                        survival.squirrels--;
                        survival.squirrelFedDays = 0;
                    }
                    else survival.predatedSquirrels++;
                    break;
                case ConstructionPreySpecies.Hedgehog:
                    survival.hedgehogNextArrivalShrubs =
                        ConstructionFoodModel.MatureConnectedShrubCount(board,
                            CurrentDay) + 1;
                    break;
            }
        }

        private void SettleFoxActivity(bool foxPresent,
            ConstructionPredationPlan hunt, int settledDay)
        {
            if (!foxPresent)
            {
                survival.foxHungryDays = 0;
                return;
            }
            survival.foxHungryDays = hunt.WillHunt ? 0 :
                survival.foxHungryDays + 1;
            if (survival.foxHungryDays < 3) return;
            survival.foxEstablished = false;
            survival.foxHungryDays = 0;
            survival.foxReturnDay = settledDay + 3;
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
            if (string.IsNullOrEmpty(PigeonTileId) &&
                story.Stage == ConstructionStoryStage.GrowGreen)
            {
                var meadow = ConnectedMeadows().FirstOrDefault(tile =>
                    Math.Max(1, tile.builtDay) < nextDay);
                if (meadow != null)
                {
                    PigeonTileId = meadow.id;
                    PigeonArrivalDay = nextDay;
                    if (Mode == ConstructionGameMode.Endless)
                        survival.pigeons = ConstructionFoodModel.PigeonFlockSize;
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
            if (board.IsFull && Mode == ConstructionGameMode.Fill)
            {
                IsFinished = true;
                EndReason = ConstructionEndReason.BoardComplete;
            }
            else
            {
                if (board.IsFull && !board.ExpandWhenFull())
                    throw new InvalidOperationException("The construction board cannot expand.");
                CurrentDay++;
                story.Observe(board);
            }
            return true;
        }

        private void SettleSurvival(ConstructionEcologyDayResult ecology,
            ConstructionHumanDayResult human)
        {
            var squirrels = survival.squirrels;
            var squirrelHungryDays = survival.squirrelHungryDays;
            var squirrelFedDays = survival.squirrelFedDays;
            UpdateAnimalPopulation(ref squirrels,
                ref squirrelHungryDays, ref squirrelFedDays,
                ecology.squirrelsFed, ecology.squirrelsPresent,
                ConstructionFoodModel.SquirrelPopulation(board, ecology.day));
            survival.squirrels = squirrels;
            survival.squirrelHungryDays = squirrelHungryDays;
            survival.squirrelFedDays = squirrelFedDays;
            var pigeons = survival.pigeons;
            var pigeonHungryDays = survival.pigeonHungryDays;
            var pigeonFedDays = survival.pigeonFedDays;
            UpdateAnimalPopulation(ref pigeons,
                ref pigeonHungryDays, ref pigeonFedDays,
                ecology.pigeonsFed, ecology.pigeonsPresent,
                ConstructionFoodModel.PigeonPopulation(board, ecology.day, PigeonTileId));
            survival.pigeons = pigeons;
            survival.pigeonHungryDays = pigeonHungryDays;
            survival.pigeonFedDays = pigeonFedDays;
            // Pigeons are represented as one social flock. A lone survivor
            // leaves the site rather than continuing as a one-bird "flock".
            if (PigeonArrivalDay > 0 && survival.pigeons == 1)
                survival.pigeons = 0;
            // The first service pair gives residents a fair chance to complete
            // a commute before absence can count toward departure.
            var routeSystemExists = board.BuiltTiles.Any(tile =>
                tile.category == ConstructionCategory.Workshop) &&
                board.BuiltTiles.Any(tile => tile.category is
                    ConstructionCategory.Restaurant or ConstructionCategory.Supermarket);
            foreach (var person in human.residents)
            {
                var record = survival.residents.FirstOrDefault(item =>
                    item.homeTileId == person.homeTileId);
                if (record == null)
                {
                    record = new ConstructionResidentSurvivalData
                        { homeTileId = person.homeTileId };
                    survival.residents.Add(record);
                }
                if (!routeSystemExists) continue;
                record.missedDays = person.completedCycle ? 0 : record.missedDays + 1;
                if (record.missedDays >= 3) record.departed = true;
            }
            if (survival.squirrels == 0 ||
                PigeonArrivalDay > 0 && survival.pigeons == 0)
                EndReason = ConstructionEndReason.WildlifeGone;
            else if (routeSystemExists &&
                survival.residents.Count > 0 && ResidentPopulation == 0)
                EndReason = ConstructionEndReason.ResidentsGone;
            if (EndReason != ConstructionEndReason.None)
            {
                IsFinished = true;
                CurrentDay = ecology.day;
            }
        }

        private static void UpdateAnimalPopulation(ref int count,
            ref int hungryDays, ref int fedDays, int fed, int present,
            int habitatCapacity)
        {
            if (count == 0) return;
            if (fed < present)
            {
                // Count missed portions, not just consecutive bad days: one
                // meadow feeds on alternate days, so that alone is not a
                // sustainable flock even if every second day looks green.
                hungryDays += present - fed;
                fedDays = 0;
                if (hungryDays >= 3)
                {
                    count--;
                    hungryDays -= 3;
                }
            }
            else
            {
                fedDays++;
                if (fedDays >= 2 && hungryDays > 0) hungryDays--;
                if (fedDays >= 3 && count < habitatCapacity)
                {
                    count++;
                    fedDays = 0;
                }
            }
        }

        private IEnumerable<ConstructionTileData> ConnectedMeadows() =>
            board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId)
                .Where(tile => tile.planting == GreenPlanting.Meadow);

        public ConstructionRunSaveData Export() => new()
        {
            mode = Mode,
            endReason = EndReason,
            survival = survival,
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
            pigeonMeadowInspected = PigeonMeadowInspected,
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
                (saved.pigeonMeadowInspected && string.IsNullOrEmpty(saved.pigeonTileId)) ||
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
                (saved.mode == ConstructionGameMode.Fill &&
                    saved.finished && !board.IsFull) ||
                (saved.mode == ConstructionGameMode.Fill && board.ExpansionRings != 0) ||
                !Enum.IsDefined(typeof(ConstructionGameMode), saved.mode) ||
                !Enum.IsDefined(typeof(ConstructionEndReason), saved.endReason) ||
                (!saved.finished && saved.endReason != ConstructionEndReason.None) ||
                (saved.mode == ConstructionGameMode.Fill &&
                    saved.endReason is ConstructionEndReason.ResidentsGone or
                        ConstructionEndReason.WildlifeGone) ||
                (saved.mode == ConstructionGameMode.Endless &&
                    (saved.endReason == ConstructionEndReason.BoardComplete ||
                     saved.finished && saved.endReason == ConstructionEndReason.None)) ||
                (saved.mode == ConstructionGameMode.Endless &&
                    (saved.survival == null || saved.survival.residents == null ||
                     saved.survival.squirrels < 0 || saved.survival.pigeons < 0 ||
                     saved.survival.squirrelHungryDays < 0 ||
                     saved.survival.pigeonHungryDays < 0 ||
                     saved.survival.squirrelFedDays < 0 ||
                     saved.survival.pigeonFedDays < 0 ||
                     saved.survival.residents.Any(person => person == null ||
                         person.missedDays < 0 ||
                         !board.BuiltTiles.Any(tile => tile.id == person.homeTileId &&
                             tile.category == ConstructionCategory.Residence)) ||
                     saved.survival.residents.Select(person => person.homeTileId)
                         .Distinct().Count() != saved.survival.residents.Count)) ||
                (saved.survival != null &&
                    (saved.survival.predatedSquirrels < 0 ||
                     saved.survival.predatedPigeons < 0 ||
                     saved.survival.hedgehogNextArrivalShrubs < 0 ||
                     saved.survival.hedgehogNextArrivalShrubs > board.BuiltCount + 1 ||
                     saved.survival.foxHungryDays < 0 ||
                     saved.survival.foxHungryDays > 2 ||
                     saved.survival.foxReturnDay < 0)) ||
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
                 lastEcologyDay.squirrelsFed < 0 ||
                 lastEcologyDay.squirrelsFed > ConstructionFoodModel.SquirrelPopulation(
                     board, lastEcologyDay.day) ||
                 lastEcologyDay.pigeonsFed < 0 ||
                 lastEcologyDay.pigeonsFed >
                     ConstructionFoodModel.PigeonPopulation(board,
                         lastEcologyDay.day, saved.pigeonTileId) ||
                 lastEcologyDay.squirrelsPresent < 0 ||
                 lastEcologyDay.squirrelsPresent >
                     ConstructionFoodModel.SquirrelPopulation(board,
                         lastEcologyDay.day) ||
                 lastEcologyDay.pigeonsPresent < 0 ||
                 lastEcologyDay.pigeonsPresent >
                     ConstructionFoodModel.PigeonPopulation(board,
                         lastEcologyDay.day, saved.pigeonTileId) ||
                 !Enum.IsDefined(typeof(ConstructionPreySpecies),
                     lastEcologyDay.predatedSpecies) ||
                 (lastEcologyDay.predatedSpecies != ConstructionPreySpecies.None &&
                     (string.IsNullOrEmpty(lastEcologyDay.predationTileId) ||
                      lastEcologyDay.foxHuntRouteTileIds == null ||
                      lastEcologyDay.foxHuntRouteTileIds.Count == 0))))
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
                lastHumanDay.ResidentCount > board.BuiltTiles.Count(tile =>
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
                 lastForecast.animalMeals >
                     ConstructionFoodModel.SquirrelPopulation(board,
                         lastForecast.day) +
                     ConstructionFoodModel.PigeonPopulation(board,
                         lastForecast.day, saved.pigeonTileId) +
                     (lastEcologyDay.predatedSpecies == ConstructionPreySpecies.None
                         ? 0 : 1) ||
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
                PigeonTileId = string.IsNullOrEmpty(saved.pigeonTileId)
                    ? null : saved.pigeonTileId,
                PigeonArrivalDay = saved.pigeonArrivalDay,
                PigeonMeadowInspected = saved.pigeonMeadowInspected,
                FirstResidentMealDay = saved.firstResidentMealDay,
                LastEcologyDay = lastEcologyDay,
                LastHumanDay = lastHumanDay,
                LastWasteDay = lastWasteDay,
                LastForecast = lastForecast
            };
            run.Mode = saved.mode;
            run.EndReason = saved.endReason;
            run.survival = saved.survival ?? new ConstructionSurvivalSaveData();
            return true;
        }
    }
}
