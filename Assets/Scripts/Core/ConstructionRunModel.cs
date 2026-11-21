using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    // Keep the first two serialized values stable for existing saves.
    // BoardGame uses the 49-day Fill rules with physical camera input.
    public enum ConstructionGameMode { Fill, Endless, BoardGame }
    public enum ConstructionEndReason
    {
        None, BoardComplete, ResidentsGone, WildlifeGone,
        AnimalDeathsLimit, ResidentsUnemployed
    }
    public enum ConstructionWildlifeSpecies { Squirrel, Pigeon, Hedgehog, Fox }

    public sealed class ConstructionWildlifeReturnStatus
    {
        public bool Pending { get; internal set; }
        public int EarliestDay { get; internal set; }
        public bool HabitatReady { get; internal set; }
        public bool FoodReady { get; internal set; }
        public int RescueDeadlineDay { get; internal set; }
        public string HabitatTileId { get; internal set; }
    }

    [Serializable]
    public sealed class ConstructionResidentSurvivalData
    {
        public string homeTileId;
        public int missedDays;
        public bool departed;
    }

    [Serializable]
    public sealed class ConstructionTrafficDeathSaveData
    {
        public string streetTileId;
        public int count;
    }

    [Serializable]
    public sealed class ConstructionHedgehogPatchState
    {
        public string homeTileId;
        public int hungryDays;
        public int returnDay;
    }

    [Serializable]
    public sealed class ConstructionSurvivalSaveData
    {
        public int squirrels = 1;
        public int pigeons;
        public int predatedSquirrels;
        public int predatedPigeons;
        public int trafficKilledSquirrels;
        public int trafficKilledPigeons;
        public List<ConstructionTrafficDeathSaveData> trafficDeaths = new();
        public int hedgehogNextArrivalShrubs;
        public bool foxEstablished;
        public int foxHungryDays;
        public int foxReturnDay;
        public int squirrelHungryDays;
        public int pigeonHungryDays;
        public int squirrelFedDays;
        public int pigeonFedDays;
        public List<int> squirrelReturnDays = new();
        public List<int> pigeonReturnDays = new();
        public int returnedSquirrels;
        public int returnedPigeons;
        public int squirrelReturnCeiling = int.MaxValue;
        public int pigeonReturnCeiling = int.MaxValue;
        public int lastSquirrelReturnDay;
        public int lastPigeonReturnDay;
        public int squirrelExtinctDay;
        public int pigeonExtinctDay;
        public bool hedgehogAbsent;
        public int hedgehogReturnDay;
        public List<ConstructionHedgehogPatchState> hedgehogPatches = new();
        // One entry per actual death. Only the most recent seven settled days
        // are retained; departures and later immigration are not deaths.
        public List<int> animalDeathDays = new();
        public int unemployedResidentsLastDay;
        public int unemployedDaysInRow;
        public List<ConstructionResidentSurvivalData> residents = new();
    }

    public enum ConstructionBuildFailure
    {
        None,
        LockedByStory,
        InvalidPlacement,
        DailyLimitReached,
        RunFinished,
        TypeLimitReached,
        IntroTypeAlreadyBuilt
    }

    [Serializable]
    public sealed class ConstructionRunSaveData
    {
        // Stable anonymous identifier for an optional leaderboard submission.
        // Older saves receive one when restored, before their first submission.
        public string runId;
        // Missing/zero keeps historical trophies on the original bands.
        public int awardRulesVersion;
        // Zero denotes a save created before timed wildlife immigration.
        public int wildlifeReturnRulesVersion;
        // Missing in pre-onboarding saves, which continue under their
        // original free-build rules instead of being relocked mid-run.
        public int introRulesVersion;
        // Opt-in digital Fill hand. Missing in existing saves, which keep
        // their original day-by-day construction rules.
        public bool usesFillHand;
        // Zero preserves the original unlimited digital draw on older saves.
        public int fillHandDeckVersion;
        // Zero keeps existing hand saves on their established wildlife rules.
        public int greenPatchRulesVersion;
        public int fillHandRandomState;
        public List<ConstructionCategory> fillHandCards = new();
        public bool fillHandWitnessedLinkage;
        public bool fillHandExpanded;
        public bool fillHandExpandedToFive;
        // Bit 0 squirrel, bit 1 pigeon, bit 2 hedgehog: first meal awards one swap.
        public int fillHandFedSpeciesMask;
        public int fillHandRerollsSpent;
        // Optional rotating objectives for the digital three-card Fill mode.
        // Missing in earlier saves; those runs receive a fresh task set once.
        public ConstructionSpecialTaskStateSaveData specialTasks;
        public ConstructionPlacementScoreSaveData placementScore;
        // Per-run presentation preference. Skipping tips never changes the
        // required room sequence or the daily construction allowance.
        public bool tutorialTipsSkipped;
        // A real Fill tutorial skip is separate from the legacy tips preference.
        public bool fillTutorialSkipped;
        public int fillTutorialSkipBuiltCount;
        // The opening screen tour is shown once per run. It does not unlock
        // construction or replace the daily room lessons.
        public bool openingTourCompleted;
        // Version zero predates configurable Endless loss conditions.
        public int endlessLossRulesVersion;
        public int animalDeathLimit;
        public int unemployedResidentLimit;
        public bool endlessLossRulesConfirmed;
        public int endlessUpgradeRulesVersion;
        public ConstructionGameMode mode;
        public ConstructionEndReason endReason;
        public ConstructionSurvivalSaveData survival;
        public ConstructionBoardSaveData board;
        public ConstructionStorySaveData story;
        public ConstructionScoreSaveData score;
        public ConstructionFoodSaveData food;
        public ConstructionWasteSaveData waste;
        public List<ConstructionResidentDispatch> residentDispatches = new();
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
        public int humanPoints;
        public int wasteCleared;
        public List<string> blockedHomeTileIds = new();
        public List<string> blockedFoodTileIds = new();
    }

    public sealed class ConstructionBuildImpact
    {
        public int animalMealsBefore;
        public int animalMealsAfter;
        public int animalPointsBefore;
        public int animalPointsAfter;
        public int workCyclesBefore;
        public int workCyclesAfter;
        public int humanPointsBefore;
        public int humanPointsAfter;
        public int balancePointsBefore;
        public int balancePointsAfter;
        public int residentsBefore;
        public int residentsAfter;
    }

    // The new construction rules deliberately do not depend on RoomLayoutData.All.
    // A presenter/simulation may read only Board.BuiltTiles for this mode.
    public sealed partial class ConstructionRunModel
    {
        public const int RecommendedAnimalDeathLimit = 3;
        public const int RecommendedUnemployedResidentLimit = 1;
        public const int AnimalDeathWindowDays = 7;
        public const int UnemployedConsecutiveDays = 3;
        public const int MinimumEndlessLossLimit = 1;
        public const int MaximumEndlessLossLimit = 20;

        private ConstructionBoardModel board;
        private ConstructionStoryModel story;
        private ConstructionScoreLedger score;
        private ConstructionFoodModel food;
        private ConstructionWasteModel waste;
        private ConstructionSurvivalSaveData survival;
        // Ordered oldest to newest so the latest dispatch wins a scarce slot.
        private List<ConstructionResidentDispatch> residentDispatches = new();
        private int introRulesVersion = 1;
        private bool usesFillHand;
        private int awardRulesVersion;
        public int AwardRulesVersion => awardRulesVersion;
        private int fillHandDeckVersion;
        private int greenPatchRulesVersion;
        private int endlessUpgradeRulesVersion;
        private int fillHandRandomState;
        private List<ConstructionCategory> fillHandCards = new();
        private bool fillHandWitnessedLinkage;
        private bool fillHandExpanded;
        private bool fillHandExpandedToFive;
        private int fillHandFedSpeciesMask;
        private int fillHandRerollsSpent;

        public int AnimalDeathLimit { get; private set; } =
            RecommendedAnimalDeathLimit;
        public int UnemployedResidentLimit { get; private set; } =
            RecommendedUnemployedResidentLimit;
        public bool EndlessLossRulesConfirmed { get; private set; }
        public int RecentAnimalDeaths => survival.animalDeathDays?.Count ?? 0;
        public int UnemployedResidentsLastDay =>
            survival.unemployedResidentsLastDay;
        public int UnemployedDaysInRow => survival.unemployedDaysInRow;

        public (int current, int best, int target) EndlessCoexistenceProgress
        {
            get
            {
                var (current, best) = score.CoexistenceStreak();
                var target = best < 1 ? 1 : best < 3 ? 3 : best < 7 ? 7 :
                    (best / 7 + 1) * 7;
                return (current, best, target);
            }
        }

        public bool ConfigureEndlessLossRules(int deathLimit,
            int unemployedLimit)
        {
            if (Mode != ConstructionGameMode.Endless || IsFinished ||
                EndlessLossRulesConfirmed ||
                deathLimit is < MinimumEndlessLossLimit or > MaximumEndlessLossLimit ||
                unemployedLimit is < MinimumEndlessLossLimit or > MaximumEndlessLossLimit)
                return false;
            // Lock the limits after the opening choice so repeatedly changing
            // the work threshold cannot erase a losing streak.
            survival.unemployedDaysInRow = 0;
            AnimalDeathLimit = deathLimit;
            UnemployedResidentLimit = unemployedLimit;
            EndlessLossRulesConfirmed = true;
            return true;
        }

        public bool TutorialTipsSkipped { get; private set; }
        public void SetTutorialTipsSkipped(bool skipped) =>
            TutorialTipsSkipped = skipped;
        public bool FillTutorialSkipped { get; private set; }
        public int FillTutorialSkipBuiltCount { get; private set; }
        public bool SkipFillTutorial()
        {
            if (!UsesFillHand || !IsIntroPhase || IsFinished || BuiltToday)
                return false;
            FillTutorialSkipBuiltCount = board.BuiltCount;
            FillTutorialSkipped = true;
            TutorialTipsSkipped = true;
            OpeningTourCompleted = true;
            DealRandomFillHand();
            // The opening plot still requires the oak. Keep it available even
            // when the first random draw contains only later room types.
            if (!UsesBasicFillResources && board.BuiltCount == 0 &&
                !fillHandCards.Contains(ConstructionCategory.Green))
                fillHandCards[0] = ConstructionCategory.Green;
            return true;
        }
        public bool OpeningTourCompleted { get; private set; }
        public void SetOpeningTourCompleted(bool completed) =>
            OpeningTourCompleted = completed;

        public ConstructionGameMode Mode { get; private set; }
        public bool UsesFillHand => Mode == ConstructionGameMode.Fill && usesFillHand;
        public bool UsesFiniteFillDeck => UsesFillHand && fillHandDeckVersion >= 1;
        public bool UsesBasicFillResources => UsesFillHand &&
            fillHandDeckVersion == 2;
        public static bool IsBasicFillResource(ConstructionCategory category) =>
            category is ConstructionCategory.Green or ConstructionCategory.Street
                or ConstructionCategory.Waste;
        public bool UsesGreenPatches => UsesFiniteFillDeck &&
            greenPatchRulesVersion == 1;
        public bool UsesUncappedWaste => UsesFiniteFillDeck;
        public int FillHandSize => !UsesFiniteFillDeck ? 3 :
            fillHandExpandedToFive ? 5 : fillHandExpanded ? 4 : 3;
        public int FillHandStartingRerolls => UsesFiniteFillDeck &&
            fillHandDeckVersion >= 2 ? 2 : 0;
        public int FillHandRerollsRemaining => !UsesFiniteFillDeck ? 0 :
            FillHandStartingRerolls +
            ((fillHandFedSpeciesMask & 1) != 0 ? 1 : 0) +
            ((fillHandFedSpeciesMask & 2) != 0 ? 1 : 0) +
            ((fillHandFedSpeciesMask & 4) != 0 ? 1 : 0) -
            fillHandRerollsSpent;
        public bool FillHandWitnessedLinkage => fillHandWitnessedLinkage;
        // A separate 100-card digital supply. The physical board's 80 tiles
        // remain governed by their own inventory and draw rules.
        public static int DigitalFillCardSupply(ConstructionCategory category) =>
            category switch
            {
                ConstructionCategory.Green => 30,
                ConstructionCategory.Residence => 18,
                ConstructionCategory.Street => 14,
                ConstructionCategory.Square => 10,
                ConstructionCategory.Workshop => 9,
                ConstructionCategory.Restaurant => 7,
                ConstructionCategory.Supermarket => 6,
                ConstructionCategory.Waste => 6,
                _ => throw new ArgumentOutOfRangeException(nameof(category))
            };
        public int FillDeckCardsRemaining => !UsesFiniteFillDeck ? -1 :
            Enum.GetValues(typeof(ConstructionCategory))
                .Cast<ConstructionCategory>()
                .Where(category => !UsesBasicFillResources ||
                    !IsBasicFillResource(category))
                .Sum(FillDeckCategoryRemaining);
        private int FillDeckCategoryRemaining(ConstructionCategory category) =>
            Math.Max(0, DigitalFillCardSupply(category) -
                CategoryBuiltCount(category) -
                fillHandCards.Count(card => card == category));
        // The introductory batch remains visible as a numbered 3/3/2 queue;
        // completed slots can be dimmed while the next slot becomes active.
        public IReadOnlyList<ConstructionCategory> FillHandCards
        {
            get
            {
                if (!UsesFillHand) return Array.Empty<ConstructionCategory>();
                if (!IsIntroPhase) return fillHandCards.ToArray();
                var start = IntroTypesBuilt / 3 * 3;
                return Enumerable.Range(start,
                        Math.Min(3, ConstructionTutorialGuide.RoomCount - start))
                    .Select(ConstructionTutorialGuide.CategoryAt)
                    .ToArray();
            }
        }
        public int FillHandActiveIndex => UsesFillHand && IsIntroPhase
            ? IntroTypesBuilt % 3 : -1;
        public bool IsFillHandCardPlayable(ConstructionCategory category) =>
            UsesFillHand && !IsFinished &&
            (IsIntroPhase
                ? IntroRequiredCategory == category
                : UsesBasicFillResources && IsBasicFillResource(category)
                    ? FillDeckCategoryRemaining(category) > 0
                    : fillHandCards.Contains(category));
        public int FillBasicResourceRemaining(ConstructionCategory category) =>
            UsesBasicFillResources && IsBasicFillResource(category)
                ? FillDeckCategoryRemaining(category) : 0;

        private void AdvanceFillHandAfterBuild(ConstructionCategory category,
            bool wasIntro)
        {
            if (board.IsFull)
            {
                fillHandCards.Clear();
                return;
            }
            if (wasIntro)
            {
                if (!IsIntroPhase) DealRandomFillHand();
                return;
            }
            if (UsesBasicFillResources && IsBasicFillResource(category))
            {
                EnsurePlayableRandomFillHand();
                return;
            }
            if (!fillHandCards.Remove(category))
                throw new InvalidOperationException("Built room was not in hand.");
            if (CanDrawFillHandCategory())
                fillHandCards.Add(DrawFillHandCategory());
            EnsurePlayableRandomFillHand();
        }

        private void DealRandomFillHand()
        {
            fillHandCards.Clear();
            for (var index = 0; index < FillHandSize; index++)
                fillHandCards.Add(DrawFillHandCategory());
            EnsurePlayableRandomFillHand();
        }

        private bool CanDrawFillHandCategory() =>
            Enum.GetValues(typeof(ConstructionCategory))
                .Cast<ConstructionCategory>()
                .Any(category => !fillHandCards.Contains(category) &&
                    (!UsesBasicFillResources ||
                     !IsBasicFillResource(category)) &&
                    (!UsesFiniteFillDeck ||
                     FillDeckCategoryRemaining(category) > 0));

        private ConstructionCategory DrawFillHandCategory(
            ConstructionCategory? excluded = null, bool legalOnly = false)
        {
            // A hand shows distinct categories, while the remaining virtual
            // cards of each category determine its chance of being drawn.
            var options = Enum.GetValues(typeof(ConstructionCategory))
                .Cast<ConstructionCategory>()
                .Where(category => !fillHandCards.Contains(category) &&
                    (!UsesBasicFillResources ||
                     !IsBasicFillResource(category)) &&
                    category != excluded &&
                    (!legalOnly || HasLegalFillHandPlot(category)) &&
                    (!UsesFiniteFillDeck ||
                     FillDeckCategoryRemaining(category) > 0))
                .ToArray();
            if (options.Length == 0)
                throw new InvalidOperationException("No digital hand category left.");
            if (!UsesFiniteFillDeck)
                return options[(int)(NextFillHandRandom() %
                    (uint)options.Length)];
            var total = options.Sum(FillDeckCategoryRemaining);
            var roll = (int)(NextFillHandRandom() % (uint)total);
            foreach (var option in options)
            {
                roll -= FillDeckCategoryRemaining(option);
                if (roll < 0) return option;
            }
            throw new InvalidOperationException("Digital deck draw exhausted.");
        }

        private uint NextFillHandRandom()
        {
            if (fillHandRandomState == 0)
            {
                uint seed = 2166136261;
                foreach (var character in RunId)
                    seed = unchecked((seed ^ character) * 16777619);
                fillHandRandomState = unchecked((int)(seed == 0 ? 1 : seed));
            }
            var value = unchecked((uint)fillHandRandomState);
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            fillHandRandomState = unchecked((int)value);
            return value;
        }

        private bool HasLegalFillHandPlot(ConstructionCategory category)
        {
            if (IsCategoryAtLimit(category)) return false;
            for (var row = board.MinCoordinate; row <= board.MaxCoordinate; row++)
            for (var column = board.MinCoordinate; column <= board.MaxCoordinate;
                 column++)
            {
                var planting = category == ConstructionCategory.Green
                    ? ResolveGreenPlanting(column, row) : GreenPlanting.None;
                if (CheckPlacement(column, row, category, planting) ==
                    ConstructionPlacementFailure.None) return true;
            }
            return false;
        }

        private void EnsurePlayableRandomFillHand()
        {
            if (board.IsFull || fillHandCards.Any(HasLegalFillHandPlot) ||
                UsesBasicFillResources && Enum.GetValues(typeof(ConstructionCategory))
                    .Cast<ConstructionCategory>().Any(category =>
                        IsBasicFillResource(category) &&
                        FillDeckCategoryRemaining(category) > 0 &&
                        HasLegalFillHandPlot(category))) return;
            var available = Enum.GetValues(typeof(ConstructionCategory))
                .Cast<ConstructionCategory>()
                .Where(category => HasLegalFillHandPlot(category) &&
                    (!UsesBasicFillResources ||
                     !IsBasicFillResource(category)) &&
                    (!UsesFiniteFillDeck ||
                     DigitalFillCardSupply(category) >
                     CategoryBuiltCount(category)))
                .ToArray();
            if (available.Length == 0) return;
            // Return an unplayable hand to its supply and deal new choices
            // without spending a player reroll.
            fillHandCards.Clear();
            if (UsesFiniteFillDeck)
            {
                var total = available.Sum(FillDeckCategoryRemaining);
                var roll = (int)(NextFillHandRandom() % (uint)total);
                foreach (var category in available)
                {
                    roll -= FillDeckCategoryRemaining(category);
                    if (roll >= 0) continue;
                    fillHandCards.Add(category);
                    break;
                }
            }
            else
                fillHandCards.Add(available[(int)(NextFillHandRandom() %
                    (uint)available.Length)]);
            while (fillHandCards.Count < FillHandSize &&
                   CanDrawFillHandCategory())
                fillHandCards.Add(DrawFillHandCategory());
        }

        private void AwardFillHandProgress(ConstructionEcologyDayResult ecology,
            ConstructionHumanDayResult human)
        {
            if (!UsesFiniteFillDeck) return;
            if (ecology.squirrelsFed > 0) fillHandFedSpeciesMask |= 1;
            if (ecology.pigeonsFed > 0) fillHandFedSpeciesMask |= 2;
            if (ecology.hedgehogFed) fillHandFedSpeciesMask |= 4;
            if (human.residents.Any(person => person.linkageBonus > 0 ||
                    person.biodiversityBonus > 0))
                fillHandWitnessedLinkage = true;
            UnlockExtraFillHandCardsIfReady();
        }

        private void UnlockExtraFillHandCardsIfReady()
        {
            if (!UsesFiniteFillDeck || IsIntroPhase || board.IsFull) return;
            var squares = CategoryBuiltCount(ConstructionCategory.Square);
            if (squares >= 3 && !fillHandExpanded)
            {
                fillHandExpanded = true;
                if (CanDrawFillHandCategory())
                    fillHandCards.Add(DrawFillHandCategory());
            }
            if (squares >= 6 && !fillHandExpandedToFive)
            {
                fillHandExpandedToFive = true;
                if (CanDrawFillHandCategory())
                    fillHandCards.Add(DrawFillHandCategory());
            }
            EnsurePlayableRandomFillHand();
        }

        public bool CanRerollFillHandCard(ConstructionCategory category) =>
            UsesFiniteFillDeck && !IsIntroPhase && !IsFinished &&
            !BuiltToday && FillHandRerollsRemaining > 0 &&
            fillHandCards.Contains(category) &&
            Enum.GetValues(typeof(ConstructionCategory))
                .Cast<ConstructionCategory>().Any(other =>
                    other != category && !fillHandCards.Contains(other) &&
                    (!UsesBasicFillResources ||
                     !IsBasicFillResource(other)) &&
                    FillDeckCategoryRemaining(other) > 0 &&
                    HasLegalFillHandPlot(other));

        public bool TryRerollFillHandCard(ConstructionCategory category,
            out ConstructionCategory replacement)
        {
            replacement = category;
            if (!CanRerollFillHandCard(category)) return false;
            var index = fillHandCards.IndexOf(category);
            fillHandCards.RemoveAt(index);
            replacement = DrawFillHandCategory(category, legalOnly: true);
            fillHandCards.Insert(index, replacement);
            fillHandRerollsSpent++;
            return true;
        }
        public bool IsRandomBoardGame =>
            Mode == ConstructionGameMode.BoardGame && introRulesVersion == 2;
        public int BoardGameRulesVersion => Mode == ConstructionGameMode.BoardGame
            ? (introRulesVersion >= 2 ? 2 : 1) : 0;
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
            (UsesFillHand || tile.id != ConstructionBoardModel.StarterOakId) &&
            Math.Max(1, tile.builtDay) == CurrentDay);
        // Legacy Fill settles the supplied oak as its first day. Digital hand
        // Fill begins empty; placing its central-oak card triggers the first
        // settlement.
        public bool CanAdvanceFillDay => Mode != ConstructionGameMode.Endless && !IsFinished &&
            (UsesFillHand ? BuiltToday : CurrentDay == 1 || BuiltToday);
        // Hand Fill requires a player room on every internal ecology tick.
        // Endless may advance without building to observe ecology in place.
        public bool CanAdvanceConstructionDay => !IsFinished &&
            (Mode == ConstructionGameMode.Endless
                ? BuiltToday
                : UsesFillHand ? BuiltToday : CurrentDay == 1 || BuiltToday);
        public bool IsFull => board.IsFull;
        public IReadOnlyList<ConstructionTileData> BuiltTiles => board.BuiltTiles;
        public ConstructionResidentDispatch GetResidentDispatch(string homeTileId)
        {
            var plan = residentDispatches.FirstOrDefault(item =>
                item.homeTileId == homeTileId);
            return plan == null ? null : CopyDispatch(plan);
        }

        public IReadOnlyList<ConstructionTileData> DispatchWorkOptions(
            string homeTileId) => DispatchOptions(homeTileId, false);

        public IReadOnlyList<ConstructionTileData> DispatchMealOptions(
            string homeTileId) => DispatchOptions(homeTileId, true);

        private IReadOnlyList<ConstructionTileData> DispatchOptions(
            string homeTileId, bool meal)
        {
            if (!HasResident(homeTileId)) return Array.Empty<ConstructionTileData>();
            return board.BuiltTiles.Where(tile =>
                    (meal ? ConstructionHumanModel.DailyMealCapacity(tile.category)
                        : ConstructionHumanModel.DailyJobCapacity(tile.category)) > 0 &&
                    board.ResidenceServiceRouteTo(homeTileId, tile.id,
                        CurrentDay).Count > 0)
                .OrderBy(tile => tile.buildIndex).ToArray();
        }

        // Dispatch changes do not consume the day's room placement. Null on
        // either leg restores automatic allocation for that leg.
        public bool TrySetResidentDispatch(string homeTileId,
            string workTileId, string mealTileId)
        {
            if (IsFinished || !HasResident(homeTileId)) return false;
            workTileId = string.IsNullOrEmpty(workTileId) ? null : workTileId;
            mealTileId = string.IsNullOrEmpty(mealTileId) ? null : mealTileId;
            if (workTileId != null && !DispatchWorkOptions(homeTileId)
                    .Any(tile => tile.id == workTileId) ||
                mealTileId != null && !DispatchMealOptions(homeTileId)
                    .Any(tile => tile.id == mealTileId)) return false;
            residentDispatches.RemoveAll(item =>
                item.homeTileId == homeTileId);
            if (workTileId != null || mealTileId != null)
                residentDispatches.Add(new ConstructionResidentDispatch
                {
                    homeTileId = homeTileId,
                    workTileId = workTileId,
                    mealTileId = mealTileId
                });
            return true;
        }

        private static ConstructionResidentDispatch CopyDispatch(
            ConstructionResidentDispatch plan) => new()
        {
            homeTileId = plan.homeTileId,
            workTileId = plan.workTileId,
            mealTileId = plan.mealTileId
        };
        // The physical board-game deck contains a fixed 80-tile inventory.
        // Fill and Endless let players choose any mix of room categories;
        // Fill still ends when its 49 board cells have been occupied.
        public int CategoryLimit(ConstructionCategory category) =>
            UsesFiniteFillDeck ? DigitalFillCardSupply(category) :
            Mode != ConstructionGameMode.BoardGame ? int.MaxValue : category switch
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
        // The starter oak is supplied by the game, so Green is introduced
        // only when the player places a new green tile in legacy modes. In
        // hand Fill, the opening oak itself is the first player card.
        public bool IsIntroTypeUsed(ConstructionCategory category) =>
            board.BuiltTiles.Any(tile => tile.category == category &&
                (UsesFillHand || tile.id != ConstructionBoardModel.StarterOakId));
        public int IntroTypesBuilt => Enum.GetValues(typeof(ConstructionCategory))
            .Cast<ConstructionCategory>().Count(IsIntroTypeUsed);
        public bool IsIntroPhase => introRulesVersion == 1 &&
            !(UsesFillHand && FillTutorialSkipped) &&
            Mode != ConstructionGameMode.Endless && !IsFinished &&
            (UsesFillHand || CurrentDay > 1) &&
            IntroTypesBuilt < ConstructionTutorialGuide.RoomCount;
        public ConstructionCategory? IntroRequiredCategory => IsIntroPhase
            ? ConstructionTutorialGuide.RequiredCategory(TutorialChapter)
            : null;
        public bool IsIntroCategoryAvailable(ConstructionCategory category) =>
            !IsFinished && IsUnlocked(category) && !IsCategoryAtLimit(category) &&
            (!IsIntroPhase || !IsIntroTypeUsed(category));
        public bool IsCategoryAtLimit(ConstructionCategory category) =>
            (Mode == ConstructionGameMode.BoardGame || UsesFiniteFillDeck) &&
            CategoryBuiltCount(category) >= CategoryLimit(category);
        public ConstructionStoryStage StoryStage => story.Stage;
        public ConstructionTutorialChapter TutorialChapter =>
            Mode == ConstructionGameMode.Endless || IsRandomBoardGame
                ? ConstructionTutorialChapter.Complete
                : UsesFillHand
                    ? ConstructionTutorialGuide.ChapterAt(IntroTypesBuilt)
                : ConstructionTutorialGuide.FromBoard(board,
                    introRulesVersion == 1
                        ? UsesFillHand || CurrentDay > 1
                        : story.Stage != ConstructionStoryStage.WatchSquirrelEat,
                    false);
        public int HomesWithRestaurantRoute => story.HomesWithRestaurantRoute;
        public bool LaterHomeUsesStreet => story.LaterHomeUsesStreet;
        public int AnimalScore => score.AnimalScore;
        public int HumanScore => score.HumanScore;
        public int CompletedWorkCyclesTotal => score.Days.Sum(day =>
            day.completedWorkCycles);
        public int BalanceScore => score.BalanceScore;
        public bool HasBalanceScore => score.HasBalanceScore;
        public bool HasAdditiveCoexistenceScore =>
            score.HasAdditiveCoexistenceScore;
        public bool HasLinkageScore => score.HasLinkageScore;
        public bool HasTrafficPenaltyScore => score.HasTrafficPenaltyScore;
        public int CollisionPenalty => score.CollisionPenalty;
        public bool HasVariableGreenYield => score.HasVariableGreenYield;
        public bool HasFedAnimalScore => score.HasFedAnimalScore;
        public int ScoringVersion => score.ScoringVersion;
        public bool UsesLegacyAnimalScoring => score.UsesLegacyAnimalScoring;
        public ConstructionDayResult LastScoreDay => score.LastDay;
        public ConstructionFinalResult FinalResult => IsFinished
            ? score.FinalResult() : null;
        public string RunId { get; private set; }
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
        private int SquirrelHabitatCapacity(int day) => UsesGreenPatches
            ? ConstructionGreenPatchModel.SquirrelCapacity(board, day)
            : ConstructionFoodModel.SquirrelPopulation(board, day);
        private int PigeonHabitatCapacity(int day) => UsesGreenPatches
            ? ConstructionGreenPatchModel.PigeonCapacity(board, day)
            : ConstructionFoodModel.PigeonPopulation(board, day, PigeonTileId);
        public int SquirrelPopulation =>
            Mode == ConstructionGameMode.Endless ? survival.squirrels :
                Math.Min(survival.squirrelReturnDays.Count > 0
                        ? survival.squirrelReturnCeiling : int.MaxValue,
                    Math.Max(0, SquirrelHabitatCapacity(CurrentDay) -
                        survival.predatedSquirrels -
                        survival.trafficKilledSquirrels + survival.returnedSquirrels));
        public int PigeonPopulation =>
            Mode == ConstructionGameMode.Endless ? survival.pigeons :
                Math.Min(survival.pigeonReturnDays.Count > 0
                        ? survival.pigeonReturnCeiling : int.MaxValue,
                    Math.Max(0, (PigeonTileId == null ? 0 :
                        PigeonHabitatCapacity(CurrentDay)) - survival.predatedPigeons -
                        survival.trafficKilledPigeons + survival.returnedPigeons));
        // These are habitat arrivals, not meals or score. Keep them derived
        // from the constructed board so loading a save cannot spawn wildlife
        // in an unbuilt legacy room.
        public IReadOnlyList<ConstructionTileData> HedgehogHabitats =>
            UsesGreenPatches
                ? ConstructionGreenPatchModel.Find(board, CurrentDay)
                    .SelectMany(patch => patch.Shrubs.Take(1))
                    .Where(tile =>
                    {
                        var state = survival.hedgehogPatches?.FirstOrDefault(
                            entry => entry.homeTileId == tile.id);
                        return state == null
                            ? food.Stock(tile.id) + GreenDailyYield(tile.id) > 0
                            : state.returnDay == 0 ||
                            CurrentDay >= state.returnDay &&
                            food.Stock(tile.id) + GreenDailyYield(tile.id) > 0;
                    }).ToArray()
                : HedgehogHabitatLegacy is { } home
                    ? new[] { home } : Array.Empty<ConstructionTileData>();
        private ConstructionTileData HedgehogHabitatLegacy =>
            !survival.hedgehogAbsent &&
            ConstructionFoodModel.MatureConnectedShrubCount(board, CurrentDay) >=
                Math.Max(1, survival.hedgehogNextArrivalShrubs)
                ? ConstructionFoodModel.HedgehogHabitat(board, CurrentDay)
                : null;
        public ConstructionTileData HedgehogHabitat => UsesGreenPatches
            ? HedgehogHabitats.FirstOrDefault() : HedgehogHabitatLegacy;
        public ConstructionTileData FoxHabitat
        {
            get
            {
                if (CurrentDay < survival.foxReturnDay) return null;
                var den = UsesGreenPatches
                    ? (survival.foxEstablished || PigeonPopulation >= 3
                        ? ConstructionGreenPatchModel.Find(board, CurrentDay)
                            .Where(patch => patch.FoxCapacity > 0)
                            .Select(patch => patch.Shrubs.Last())
                            .FirstOrDefault(candidate =>
                                survival.foxEstablished || CanFoxHunt(candidate))
                        : null)
                    : ConstructionFoodModel.FoxHabitat(board, CurrentDay,
                        survival.foxEstablished ? int.MaxValue :
                            PigeonPopulation);
                return !UsesGreenPatches && survival.foxReturnDay > 0 && !survival.foxEstablished &&
                    !CanFoxHunt(den) ? null : den;
            }
        }
        public int MatureNearbyOakCount =>
            ConstructionFoodModel.MatureNearbyOakCount(board, CurrentDay);
        public int MaximumNearbyOakCount =>
            ConstructionFoodModel.MaximumNearbyOakCount(board);
        public int MatureConnectedMeadowCount =>
            ConstructionFoodModel.MatureConnectedMeadowCount(board,
                CurrentDay, PigeonTileId);
        public int MatureConnectedShrubCount =>
            ConstructionFoodModel.MatureConnectedShrubCount(board, CurrentDay);
        public ConstructionWildlifeReturnStatus ReturnStatus(
            ConstructionWildlifeSpecies species)
        {
            var status = new ConstructionWildlifeReturnStatus();
            if (IsFinished) return status;
            switch (species)
            {
                case ConstructionWildlifeSpecies.Squirrel:
                    status.Pending = Mode != ConstructionGameMode.Endless
                        ? survival.squirrelReturnDays.Count > 0
                        : survival.squirrelExtinctDay > 0 && survival.squirrels == 0;
                    status.EarliestDay = Mode != ConstructionGameMode.Endless
                        ? status.Pending ? survival.squirrelReturnDays.Min() : 0
                        : status.Pending ? survival.squirrelExtinctDay + 3 : 0;
                    status.RescueDeadlineDay = Mode == ConstructionGameMode.Endless &&
                        status.Pending ? survival.squirrelExtinctDay + 5 : 0;
                    status.HabitatTileId = ConstructionBoardModel.StarterOakId;
                    var squirrelTarget = SquirrelPopulation + 1;
                    status.HabitatReady = (UsesGreenPatches
                            ? SquirrelHabitatCapacity(CurrentDay) > 0 :
                            MatureNearbyOakCount > 0) &&
                        squirrelTarget <= SquirrelHabitatCapacity(CurrentDay);
                    status.FoodReady = CanFeedSquirrels(squirrelTarget);
                    break;
                case ConstructionWildlifeSpecies.Pigeon:
                    status.Pending = Mode != ConstructionGameMode.Endless
                        ? survival.pigeonReturnDays.Count > 0
                        : survival.pigeonExtinctDay > 0 && survival.pigeons == 0;
                    status.EarliestDay = Mode != ConstructionGameMode.Endless
                        ? status.Pending ? survival.pigeonReturnDays.Min() : 0
                        : status.Pending ? survival.pigeonExtinctDay + 3 : 0;
                    status.RescueDeadlineDay = Mode == ConstructionGameMode.Endless &&
                        status.Pending ? survival.pigeonExtinctDay + 5 : 0;
                    status.HabitatTileId = PigeonTileId;
                    var pigeonTarget = Mode == ConstructionGameMode.Endless &&
                        PigeonPopulation == 0 ? 2 : PigeonPopulation + 1;
                    status.HabitatReady = !string.IsNullOrEmpty(PigeonTileId) &&
                        (UsesGreenPatches ||
                         MatureConnectedMeadowCount >= pigeonTarget) &&
                        pigeonTarget <= PigeonHabitatCapacity(CurrentDay);
                    status.FoodReady = CanFeedPigeonsForTwoDays(pigeonTarget);
                    break;
                case ConstructionWildlifeSpecies.Hedgehog:
                    var missingPatch = UsesGreenPatches
                        ? survival.hedgehogPatches?
                            .Where(entry => entry.returnDay > 0)
                            .OrderBy(entry => entry.returnDay)
                            .FirstOrDefault()
                        : null;
                    status.Pending = UsesGreenPatches
                        ? missingPatch != null : survival.hedgehogAbsent;
                    status.EarliestDay = status.Pending
                        ? UsesGreenPatches ? missingPatch.returnDay :
                            survival.hedgehogReturnDay : 0;
                    var shrubs = ConnectedMatureShrubs();
                    status.HabitatTileId = missingPatch?.homeTileId ??
                        shrubs.FirstOrDefault()?.id ??
                        board.BuiltTiles.Where(tile =>
                                tile.category == ConstructionCategory.Green &&
                                tile.planting == GreenPlanting.Shrub)
                            .OrderBy(tile => tile.buildIndex)
                            .FirstOrDefault()?.id;
                    status.HabitatReady = UsesGreenPatches
                        ? shrubs.Any(tile => tile.id ==
                            status.HabitatTileId) : shrubs.Length > 0;
                    status.FoodReady = shrubs.Any(tile =>
                        (!UsesGreenPatches || tile.id ==
                            status.HabitatTileId) &&
                        food.Stock(tile.id) +
                        ConstructionFoodModel.DailyYield(tile, CurrentDay,
                            score.HasVariableGreenYield, RunId,
                            UsesGreenUpgrades
                                ? GreenUpgradeLevel(tile.id) : 1) >= 1);
                    break;
                case ConstructionWildlifeSpecies.Fox:
                    status.Pending = survival.foxReturnDay > 0 && FoxHabitat == null;
                    status.EarliestDay = status.Pending
                        ? survival.foxReturnDay : 0;
                    var foxShrubs = ConnectedMatureShrubs();
                    status.HabitatTileId = foxShrubs.LastOrDefault()?.id ??
                        board.BuiltTiles.Where(tile =>
                                tile.category == ConstructionCategory.Green &&
                                tile.planting == GreenPlanting.Shrub)
                            .OrderBy(tile => tile.buildIndex)
                            .LastOrDefault()?.id;
                    status.HabitatReady = UsesGreenPatches
                        ? ConstructionGreenPatchModel.Find(board, CurrentDay)
                            .Any(patch => patch.FoxCapacity > 0)
                        : foxShrubs.Length >= 2;
                    status.FoodReady = PigeonPopulation >= 3 &&
                        CanFoxHunt(foxShrubs.LastOrDefault());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(species));
            }
            return status;
        }

        private ConstructionTileData[] ConnectedMatureShrubs() =>
            (UsesGreenPatches ? board.BuiltTiles :
                board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId))
                .Where(tile => tile.planting == GreenPlanting.Shrub &&
                    tile.builtDay <= CurrentDay && GreenGrowthStage(tile.id) == 2)
                .OrderBy(tile => tile.buildIndex).ToArray();

        private bool CanFoxHunt(ConstructionTileData den) => den != null &&
            ConstructionFoodModel.PlanFoxHunt(board, CurrentDay, den.id,
                PigeonTileId, PigeonPopulation, SquirrelPopulation,
                HedgehogHabitat?.id, true, UsesGreenPatches,
                UsesGreenPatches ? HedgehogHabitats.Select(tile => tile.id)
                    .ToArray() : null).WillHunt;

        private bool CanFeedSquirrels(int target)
        {
            if (target < 1) return false;
            var homes = ConstructionFoodModel.SquirrelHomeTiles(board,
                CurrentDay, target, UsesGreenPatches);
            var candidateOaks = UsesGreenPatches ? board.BuiltTiles :
                board.ConnectedGreenTiles(ConstructionBoardModel.StarterOakId);
            var oaks = candidateOaks
                .Where(tile => tile.planting == GreenPlanting.Oak &&
                    tile.builtDay <= CurrentDay && GreenGrowthStage(tile.id) == 2 &&
                    homes.Any(home =>
                        ConstructionBoardModel.GridDistance(tile.column,
                            tile.row, home.column, home.row) <= 1 &&
                        GreenFoodRouteTo(home.id, tile.id).Count > 0))
                .ToArray();
            return oaks.Count(tile =>
                food.Stock(tile.id) + Math.Min(
                    ConstructionFoodModel.DailyYield(tile, CurrentDay,
                        score.HasVariableGreenYield, RunId,
                        UsesGreenUpgrades ? GreenUpgradeLevel(tile.id) : 1),
                    FoodCapacity(tile.id) -
                    food.Stock(tile.id)) > 0) >= target;
        }

        private bool CanFeedPigeonsForTwoDays(int target)
        {
            if (target < 1 || string.IsNullOrEmpty(PigeonTileId)) return false;
            if (UsesGreenPatches)
            {
                var groups = ConstructionGreenPatchModel.PigeonGroups(
                    board, CurrentDay, target);
                if (groups.Sum(group => group.Count) < target) return false;
                foreach (var group in groups)
                {
                    var stocks = group.Patch.Meadows.ToDictionary(tile =>
                        tile.id, tile => food.Stock(tile.id));
                    for (var day = CurrentDay; day <= CurrentDay + 1; day++)
                    {
                        foreach (var tile in group.Patch.Meadows)
                            stocks[tile.id] += Math.Min(
                                ConstructionFoodModel.DailyYield(tile, day,
                                    score.HasVariableGreenYield, RunId,
                                    GreenUpgradeLevel(tile.id)),
                                FoodCapacity(tile.id) - stocks[tile.id]);
                        var needed = group.Count;
                        foreach (var tile in group.Patch.Meadows)
                        {
                            var eaten = Math.Min(needed, stocks[tile.id]);
                            stocks[tile.id] -= eaten;
                            needed -= eaten;
                        }
                        if (needed > 0) return false;
                    }
                }
                return true;
            }
            var meadows = board.AnimalReachableGreenTiles(PigeonTileId, CurrentDay)
                .Where(tile => tile.planting == GreenPlanting.Meadow &&
                    tile.builtDay <= CurrentDay && GreenGrowthStage(tile.id) == 2)
                .ToArray();
            if (meadows.Length < target) return false;
            var stock = meadows.ToDictionary(tile => tile.id,
                tile => food.Stock(tile.id));
            for (var day = CurrentDay; day <= CurrentDay + 1; day++)
            {
                foreach (var tile in meadows)
                    stock[tile.id] += Math.Min(
                        ConstructionFoodModel.DailyYield(tile, day,
                            score.HasVariableGreenYield, RunId,
                            UsesGreenUpgrades ? GreenUpgradeLevel(tile.id) : 1),
                        FoodCapacity(tile.id) -
                        stock[tile.id]);
                var remaining = target;
                foreach (var tile in meadows)
                {
                    var eaten = Math.Min(remaining, stock[tile.id]);
                    stock[tile.id] -= eaten;
                    remaining -= eaten;
                    if (remaining == 0) break;
                }
                if (remaining > 0) return false;
            }
            return true;
        }
        public string FirstResidentHomeTileId => board.FirstResidence?.id;
        public bool FirstResidentHasRestaurantAccess =>
            board.FirstResidenceCanReachService(ConstructionCategory.Restaurant);
        public IReadOnlyList<ConstructionTileData> FirstResidentRestaurantRoute =>
            board.FirstResidenceServiceRoute(ConstructionCategory.Restaurant);

        public ConstructionTileData At(int column, int row) => board.At(column, row);
        public bool IsUnlocked(ConstructionCategory category) =>
            Mode == ConstructionGameMode.Endless
                ? Enum.IsDefined(typeof(ConstructionCategory), category)
                : IsRandomBoardGame
                    ? CurrentDay > 1 &&
                      Enum.IsDefined(typeof(ConstructionCategory), category)
                : UsesFillHand
                    ? Enum.IsDefined(typeof(ConstructionCategory), category) &&
                      IsFillHandCardPlayable(category)
                : introRulesVersion == 1
                    ? CurrentDay > 1 &&
                      Enum.IsDefined(typeof(ConstructionCategory), category) &&
                      (!IsIntroPhase || IntroRequiredCategory == category)
                    : story.IsUnlocked(category);
        // Green is one player-facing room type. Its vegetation is determined
        // by the local habitat, not by a second construction-menu choice.
        // Hand Fill deals the central oak as its first Green card. After that,
        // until the oak has a connected meadow, a new Green becomes meadow.
        // Legacy Fill still requires its first player Green beside the oak.
        public GreenPlanting ResolveGreenPlanting(int column, int row)
        {
            if (UsesFillHand && board.BuiltCount == 0)
                return GreenPlanting.Oak;
            var built = board.BuiltTiles;
            if (!HasConnectedMeadow)
                return GreenPlanting.Meadow;

            var adjacent = built.Where(tile =>
                ConstructionBoardModel.AreCardinalNeighbours(tile.column, tile.row,
                    column, row))
                .ToArray();
            if (adjacent.Any(tile => tile.category != ConstructionCategory.Green))
                return GreenPlanting.Meadow;
            if (ConstructionBoardModel.AreCardinalNeighbours(column, row,
                    ConstructionBoardModel.StarterColumn,
                    ConstructionBoardModel.StarterRow))
                return GreenPlanting.Oak;
            var green = adjacent.Where(tile =>
                tile.category == ConstructionCategory.Green).ToArray();
            if (green.Length >= 2) return GreenPlanting.Shrub;
            return green.Any(tile => tile.planting == GreenPlanting.Meadow)
                ? GreenPlanting.Oak : GreenPlanting.Meadow;
        }
        public ConstructionPlacementFailure CheckPlacement(int column, int row,
            ConstructionCategory category, GreenPlanting planting = GreenPlanting.None,
            ResidenceKind residenceKind = ResidenceKind.Standard)
        {
            if (category == ConstructionCategory.Green && planting == GreenPlanting.None)
                planting = ResolveGreenPlanting(column, row);
            if (IsIntroPhase && IsIntroTypeUsed(category))
                return ConstructionPlacementFailure.IntroTypeAlreadyBuilt;
            if (IsIntroPhase &&
                Enum.IsDefined(typeof(ConstructionCategory), category) &&
                IntroRequiredCategory != category)
                return ConstructionPlacementFailure.IntroTypeNotNext;
            if (UsesFillHand && board.BuiltCount == 0)
            {
                if (category != ConstructionCategory.Green)
                    return ConstructionPlacementFailure.IntroTypeNotNext;
                if (planting != GreenPlanting.Oak)
                    return ConstructionPlacementFailure.InvalidPlanting;
                if (residenceKind != ResidenceKind.Standard)
                    return ConstructionPlacementFailure.InvalidResidenceKind;
                if (!board.Contains(column, row))
                    return ConstructionPlacementFailure.OutsideBoard;
                return column == ConstructionBoardModel.StarterColumn &&
                       row == ConstructionBoardModel.StarterRow
                    ? ConstructionPlacementFailure.None
                    : ConstructionPlacementFailure.StarterOakMustBeCenter;
            }
            // Live builds obey the same room prerequisites throughout the
            // tutorial and free play. Save replay is handled separately by
            // ConstructionBoardModel.TryRestore for earlier introductions.
            var placement = board.CheckPlacement(column, row, category, planting,
                residenceKind: residenceKind);
            if (placement != ConstructionPlacementFailure.None) return placement;
            if (IsIntroPhase && IntroRequiredCategory == ConstructionCategory.Square &&
                category == ConstructionCategory.Square &&
                !WouldConnectSquareToHome(column, row))
                return ConstructionPlacementFailure.NeedsHomeOrConnectedStreet;
            if (IsIntroPhase && IntroRequiredCategory == ConstructionCategory.Green &&
                category == ConstructionCategory.Green &&
                !ConstructionBoardModel.AreCardinalNeighbours(column, row,
                    ConstructionBoardModel.StarterColumn,
                    ConstructionBoardModel.StarterRow))
                return ConstructionPlacementFailure.Disconnected;
            return IsCategoryAtLimit(category)
                ? ConstructionPlacementFailure.TypeLimitReached
                : ConstructionPlacementFailure.None;
        }
        public int AdjacentGreenCount(string residenceId) =>
            board.AdjacentGreenCount(residenceId);
        public bool WouldConnectSquareToHome(int column, int row) =>
            board.WouldConnectSquareToHome(column, row);
        public int ConnectedGreenCount(string greenId) =>
            board.ConnectedGreenCount(greenId);
        public ConstructionGreenPatch GreenPatchFor(string greenId) =>
            !UsesGreenPatches || string.IsNullOrEmpty(greenId) ? null :
                ConstructionGreenPatchModel.Find(board, CurrentDay)
                    .FirstOrDefault(patch => patch.Tiles.Any(tile =>
                        tile.id == greenId));
        public int FoodStock(string tileId) => food.Stock(tileId);
        public int WasteBacklog(string tileId) => waste.Backlog(tileId);
        public bool ResidenceCanReachFood(string residenceId) =>
            board.ResidenceServiceRoute(residenceId,
                ConstructionCategory.Restaurant).Count > 0 ||
            board.ResidenceServiceRoute(residenceId,
                ConstructionCategory.Supermarket).Count > 0;
        public bool ResidenceCanReachWork(string residenceId) =>
            board.ResidenceServiceRoute(residenceId,
                ConstructionCategory.Workshop).Count > 0 ||
            board.ResidenceServiceRoute(residenceId,
                ConstructionCategory.Restaurant).Count > 0 ||
            board.ResidenceServiceRoute(residenceId,
                ConstructionCategory.Supermarket).Count > 0;

        // Check a proposed home on a copy of the run so the confirmation card
        // describes this plot's routes without using the daily build action.
        public bool TryPreviewResidenceRoutes(int column, int row,
            out bool canReachWork, out bool canReachMeal)
        {
            canReachWork = false;
            canReachMeal = false;
            if (CheckPlacement(column, row, ConstructionCategory.Residence,
                    GreenPlanting.None) != ConstructionPlacementFailure.None ||
                !TryRestore(Export(), out var projected) ||
                !projected.TryBuild(column, row,
                    ConstructionCategory.Residence, GreenPlanting.None,
                    out var home, out _, out _))
                return false;
            canReachWork = projected.ResidenceCanReachWork(home.id);
            canReachMeal = projected.ResidenceCanReachFood(home.id);
            return true;
        }

        public ConstructionHumanDayResult PreviewHumanDay()
        {
            // A finished run has already settled its final day. Previewing
            // ecology again would attempt to settle the same food day twice.
            if (IsFinished)
                return LastHumanDay ?? new ConstructionHumanDayResult
                    { day = CurrentDay };
            var availability = PreviewWasteAvailability();
            var human = ConstructionHumanModel.SettleDay(CurrentDay, board,
                availability.BlockedHomes, availability.BlockedFoodServices,
                DepartedHomes, residentDispatches,
                score.ScoringVersion >= 11, UsesNeighborUpgrades);
            if (score.HasLinkageScore)
                ConstructionLinkageScorer.Apply(board, CurrentDay,
                    PreviewEcologyDay(), human,
                    score.HasLocalFoodAreaScore,
                    score.ScoringVersion >= 8);
            return human;
        }
        public ConstructionWasteAvailability PreviewWasteAvailability() =>
            waste.PreviewAfterCleanup(board, UsesUncappedWaste);
        // Evaluate a staged build against a restored copy. Both sides use the
        // same day-settlement previews as the actual score, without spending
        // today's build or changing food, story or save state.
        public bool TryPreviewBuildImpact(int column, int row,
            ConstructionCategory category, GreenPlanting planting,
            out ConstructionBuildImpact impact,
            ResidenceKind residenceKind = ResidenceKind.Standard)
        {
            impact = null;
            if (IsFinished || BuiltToday || !IsUnlocked(category) ||
                CheckPlacement(column, row, category, planting, residenceKind) !=
                    ConstructionPlacementFailure.None ||
                !TryRestore(Export(), out var projected) ||
                !projected.TryBuild(column, row, category, planting,
                    out _, out _, out _, residenceKind))
                return false;

            var animalBefore = PreviewEcologyDay();
            var animalAfter = projected.PreviewEcologyDay();
            var humanBefore = PreviewHumanDay();
            var humanAfter = projected.PreviewHumanDay();
            impact = new ConstructionBuildImpact
            {
                animalMealsBefore = animalBefore.AnimalMeals,
                animalMealsAfter = animalAfter.AnimalMeals,
                animalPointsBefore = score.UsesLegacyAnimalScoring
                    ? animalBefore.LegacyAnimalMeals : animalBefore.animalPoints,
                animalPointsAfter = score.UsesLegacyAnimalScoring
                    ? animalAfter.LegacyAnimalMeals : animalAfter.animalPoints,
                workCyclesBefore = humanBefore.CompletedWorkCycles,
                workCyclesAfter = humanAfter.CompletedWorkCycles,
                humanPointsBefore = score.HasBalanceScore
                    ? humanBefore.SatisfactionPoints : humanBefore.CompletedWorkCycles,
                humanPointsAfter = score.HasBalanceScore
                    ? humanAfter.SatisfactionPoints : humanAfter.CompletedWorkCycles,
                balancePointsBefore = score.HasBalanceScore
                    ? score.CoexistencePoints(animalBefore.animalPoints,
                        humanBefore.SatisfactionPoints) : 0,
                balancePointsAfter = score.HasBalanceScore
                    ? score.CoexistencePoints(animalAfter.animalPoints,
                        humanAfter.SatisfactionPoints) : 0,
                residentsBefore = ResidentPopulation,
                residentsAfter = projected.ResidentPopulation
            };
            return true;
        }
        public ConstructionEcologyDayResult PreviewEcologyDay()
        {
            var availability = PreviewWasteAvailability();
            var human = ConstructionHumanModel.SettleDay(CurrentDay, board,
                availability.BlockedHomes, availability.BlockedFoodServices,
                DepartedHomes, residentDispatches,
                score.ScoringVersion >= 11, UsesNeighborUpgrades);
            var hunt = PlanPredation();
            var traffic = PlanTrafficCollision(hunt, human);
            var result = food.PreviewDay(CurrentDay, board, PigeonTileId,
                Math.Max(0, SquirrelPopulation -
                    (hunt.species == ConstructionPreySpecies.Squirrel ? 1 : 0) -
                    (traffic.species == ConstructionTrafficSpecies.Squirrel ? 1 : 0)),
                Math.Max(0, PigeonPopulation -
                    (hunt.species == ConstructionPreySpecies.Pigeon ? 1 : 0) -
                    (traffic.species == ConstructionTrafficSpecies.Pigeon ? 1 : 0)),
                hunt.species == ConstructionPreySpecies.Hedgehog ||
                    traffic.species == ConstructionTrafficSpecies.Hedgehog
                    ? null : HedgehogHabitat?.id,
                UrbanFoodSources(availability, human),
                score.HasVariableGreenYield, RunId,
                score.HasLocalFoodAreaScore, UsesGreenUpgrades,
                UsesGreenPatches,
                UsesGreenPatches ? HedgehogHabitats.Where(tile =>
                    tile.id != (hunt.species ==
                        ConstructionPreySpecies.Hedgehog
                            ? hunt.preyTileId : null) &&
                    tile.id != (traffic.species ==
                        ConstructionTrafficSpecies.Hedgehog
                            ? traffic.wildlifeTileId : null))
                    .Select(tile => tile.id).ToArray() : null,
                score.HasFedAnimalScore);
            result.pigeonHabitatTileId = result.pigeonsPresent > 0
                ? PigeonTileId : null;
            result.hedgehogHabitatTileId = result.hedgehogPresent
                ? result.hedgehogHomeTileIds.FirstOrDefault() : null;
            MarkPredation(result, hunt);
            MarkTraffic(result, traffic);
            ApplyEnhancedGreenScore(result);
            return result;
        }

        private IReadOnlyList<ConstructionUrbanFoodSource> UrbanFoodSources(
            ConstructionWasteAvailability availability,
            ConstructionHumanDayResult human)
        {
            var sources = new List<ConstructionUrbanFoodSource>();
            foreach (var tile in board.BuiltTiles)
            {
                var portions = tile.category switch
                {
                    ConstructionCategory.Supermarket => human.residents.Count(person =>
                        person.mealTileId == tile.id) > 0 ? 1 : 0,
                    ConstructionCategory.Waste => availability.CollectedByRoom.TryGetValue(
                        tile.id, out var collected) && collected > 0 ? 1 : 0,
                    ConstructionCategory.Square => human.residents.Count(person =>
                        person.visitedSquare && person.squareTileId == tile.id),
                    _ => 0
                };
                if (portions > 0)
                    sources.Add(new ConstructionUrbanFoodSource
                    {
                        tileId = tile.id,
                        category = tile.category,
                        portions = portions
                    });
            }
            return sources;
        }

        private ConstructionPredationPlan PlanPredation()
        {
            var den = FoxHabitat;
            return ConstructionFoodModel.PlanFoxHunt(board, CurrentDay,
                den?.id, PigeonTileId, PigeonPopulation, SquirrelPopulation,
                HedgehogHabitat?.id, den != null &&
                    survival.foxEstablished && survival.foxHungryDays >= 1,
                UsesGreenPatches,
                UsesGreenPatches ? HedgehogHabitats.Select(tile => tile.id)
                    .ToArray() : null);
        }

        private IReadOnlyCollection<string> ProtectedStreets(
            ConstructionHumanDayResult human)
        {
            if (score.ScoringVersion < 8 || human?.residents == null)
                return Array.Empty<string>();
            var visited = human.residents
                .Where(person => person.completedCycle && person.visitedSquare)
                .Select(person => board.BuiltTiles.FirstOrDefault(tile =>
                    tile.id == person.squareTileId &&
                    tile.category == ConstructionCategory.Square &&
                    tile.builtDay <= CurrentDay))
                .Where(tile => tile != null).ToArray();
            if (visited.Length == 0) return Array.Empty<string>();
            return board.BuiltTiles.Where(street =>
                    street.category == ConstructionCategory.Street &&
                    street.builtDay <= CurrentDay &&
                    visited.Any(square => ConstructionBoardModel.AreNeighbours(
                        square.column, square.row,
                        street.column, street.row)))
                .Select(street => street.id).ToArray();
        }

        public bool IsStreetProtectedTomorrow(string streetTileId)
        {
            if (IsFinished || string.IsNullOrEmpty(streetTileId)) return false;
            var availability = PreviewWasteAvailability();
            var human = ConstructionHumanModel.SettleDay(CurrentDay, board,
                availability.BlockedHomes, availability.BlockedFoodServices,
                DepartedHomes, residentDispatches,
                score.ScoringVersion >= 11, UsesNeighborUpgrades);
            return ProtectedStreets(human).Contains(streetTileId);
        }

        private ConstructionTrafficCollision PlanTrafficCollision(
            ConstructionPredationPlan hunt, ConstructionHumanDayResult human) =>
            ConstructionTrafficModel.PlanDay(board, CurrentDay,
                SquirrelPopulation, PigeonPopulation, PigeonTileId,
                HedgehogHabitat?.id, FoxHabitat?.id, hunt.species,
                ProtectedStreets(human), UsesGreenPatches,
                UsesGreenPatches ? HedgehogHabitats.Select(tile => tile.id)
                    .ToArray() : null, hunt.preyTileId,
                useNeighborUpgrades: UsesNeighborUpgrades);

        private static void MarkPredation(ConstructionEcologyDayResult ecology,
            ConstructionPredationPlan hunt)
        {
            ecology.predatedSpecies = hunt.species;
            ecology.predationTileId = hunt.preyTileId;
            ecology.foxHuntRouteTileIds = hunt.routeTileIds.ToList();
        }

        private static void MarkTraffic(ConstructionEcologyDayResult ecology,
            ConstructionTrafficCollision collision)
        {
            ecology.trafficSpecies = collision.species;
            ecology.trafficStreetTileId = collision.streetTileId;
            ecology.trafficWildlifeTileId = collision.wildlifeTileId;
        }
        public IReadOnlyList<ConstructionTileData> PreviewSquirrelFoodRoute()
        {
            var ecology = PreviewEcologyDay();
            return ecology.squirrelFoodTileIds.Count == 0
                ? Array.Empty<ConstructionTileData>()
                : SquirrelFoodRouteForMeal(ecology, 0);
        }
        // The board view spreads visible squirrels over nearby mature oaks.
        // Route inspection uses the same homes so selecting any occupied oak
        // points to the animals actually shown in that room.
        public IReadOnlyList<ConstructionTileData> VisibleSquirrelHomeTiles(
            int maximum)
        {
            if (maximum <= 0 || SquirrelPopulation <= 0)
                return Array.Empty<ConstructionTileData>();
            var oaks = ConstructionFoodModel.SquirrelHomeTiles(board,
                CurrentDay, SquirrelPopulation, UsesGreenPatches).ToArray();
            if (oaks.Length == 0)
                return Array.Empty<ConstructionTileData>();
            return Enumerable.Range(0, Math.Min(maximum, SquirrelPopulation))
                .Select(index => oaks[index % oaks.Length]).ToArray();
        }
        public IReadOnlyList<IReadOnlyList<ConstructionTileData>> PreviewSquirrelFoodRoutes()
        {
            var ecology = PreviewEcologyDay();
            return Enumerable.Range(0, ecology.squirrelFoodTileIds.Count)
                .Select(index => SquirrelFoodRouteForMeal(ecology, index))
                .ToArray();
        }

        private IReadOnlyList<ConstructionTileData> SquirrelFoodRouteForMeal(
            ConstructionEcologyDayResult ecology, int index)
        {
            var target = ecology.squirrelFoodTileIds[index];
            var homes = ecology.squirrelMealHomeTileIds;
            return homes != null && index < homes.Count &&
                   !string.IsNullOrEmpty(homes[index])
                ? WildlifeFoodRoute(homes[index], target)
                : SquirrelFoodRouteTo(target);
        }

        private IReadOnlyList<ConstructionTileData> SquirrelFoodRouteTo(
            string target)
        {
            if (string.IsNullOrEmpty(target))
                return Array.Empty<ConstructionTileData>();
            return ConstructionFoodModel.SquirrelHomeTiles(board, CurrentDay,
                    SquirrelPopulation, UsesGreenPatches)
                .Select(home => WildlifeFoodRoute(home.id, target))
                .Where(route => route.Count > 0)
                .OrderBy(route => route.Count)
                .FirstOrDefault() ?? Array.Empty<ConstructionTileData>();
        }
        public IReadOnlyList<ConstructionTileData> PreviewPigeonFoodRoute()
        {
            var ecology = PreviewEcologyDay();
            var origin = UsesGreenPatches &&
                ecology.pigeonMealHomeTileIds.Count > 0
                ? ecology.pigeonMealHomeTileIds[0] : PigeonTileId;
            return WildlifeFoodRoute(origin, ecology.pigeonFoodTileId);
        }
        public IReadOnlyList<IReadOnlyList<ConstructionTileData>> PreviewPigeonFoodRoutes()
        {
            var ecology = PreviewEcologyDay();
            if (!UsesGreenPatches)
                return ecology.pigeonFoodTileIds.Select(target =>
                    WildlifeFoodRoute(PigeonTileId, target)).ToArray();
            return ecology.pigeonMealTileIds.Select((target, index) =>
                WildlifeFoodRoute(UsesGreenPatches &&
                    index < ecology.pigeonMealHomeTileIds.Count
                        ? ecology.pigeonMealHomeTileIds[index] :
                        PigeonTileId, target)).ToArray();
        }
        public IReadOnlyList<ConstructionTileData> VisiblePigeonHomeTiles(
            int maximum)
        {
            if (maximum <= 0 || PigeonPopulation <= 0)
                return Array.Empty<ConstructionTileData>();
            if (UsesGreenPatches)
                return ConstructionGreenPatchModel.PigeonHomes(board,
                    CurrentDay, Math.Min(maximum, PigeonPopulation));
            var anchor = board.BuiltTiles.FirstOrDefault(tile =>
                tile.id == PigeonTileId);
            return anchor == null ? Array.Empty<ConstructionTileData>() :
                Enumerable.Repeat(anchor, Math.Min(maximum, PigeonPopulation))
                    .ToArray();
        }
        public ConstructionTileData VisitorPigeonTile => UsesGreenPatches
            ? ConstructionGreenPatchModel.Find(board, CurrentDay)
                .Where(patch => patch.Meadows.Count < 2)
                .SelectMany(patch => patch.Tiles.Where(tile =>
                    tile.planting == GreenPlanting.Meadow))
                .OrderBy(tile => tile.buildIndex)
                .FirstOrDefault()
            : null;
        public IReadOnlyList<ConstructionPigeonGroup> PigeonGroups(
            int population) => UsesGreenPatches
                ? ConstructionGreenPatchModel.PigeonGroups(board, CurrentDay,
                    population)
                : Array.Empty<ConstructionPigeonGroup>();
        public IReadOnlyList<ConstructionTileData> GreenFoodRouteTo(
            string originTileId, string foodTileId) =>
            WildlifeFoodRoute(originTileId, foodTileId);
        private IReadOnlyList<ConstructionTileData> WildlifeFoodRoute(
            string originTileId, string foodTileId)
        {
            return board.AnimalRouteBetween(originTileId, foodTileId,
                CurrentDay);
        }
        private void ApplyEnhancedGreenScore(
            ConstructionEcologyDayResult ecology)
        {
            if (score.ScoringVersion < 11 || ecology == null) return;
            // Recompute from observed meals. A visit without a meal cannot
            // create a feeding-source award.
            var fed = ConstructionFoodAreaScorer.ScoreFed(board, ecology,
                enhancedGreenSourceScore: true,
                balancedScore: score.ScoringVersion >= 12);
            ecology.animalPoints = fed.points;
            ecology.fedScoringSourceTileIds = fed.sourceTileIds;
        }
        public IReadOnlyList<ConstructionTileData> ResidenceRouteTo(
            string residenceId, string serviceTileId) =>
            board.ResidenceServiceRouteTo(residenceId, serviceTileId);
        public int FoodCapacity(GreenPlanting planting) =>
            ConstructionFoodModel.Capacity(planting);
        public int FoodCapacity(string greenTileId)
        {
            var tile = board.BuiltTiles.FirstOrDefault(item =>
                item.id == greenTileId &&
                item.category == ConstructionCategory.Green);
            return ConstructionFoodModel.Capacity(board, tile,
                UsesGreenUpgrades);
        }
        public int GreenGrowthStage(string tileId)
        {
            var tile = board.BuiltTiles.FirstOrDefault(item => item.id == tileId);
            return ConstructionFoodModel.MaturityStage(tile, CurrentDay);
        }
        // Each exposed animal on one of the eight neighbouring greens contributes to this
        // street's daily collision risk. It is not a promised death count.
        public int StreetAdjacentWildlifeCount(string streetTileId)
        {
            var street = board.BuiltTiles.FirstOrDefault(item =>
                item.id == streetTileId && item.category == ConstructionCategory.Street);
            if (street == null) return 0;
            var hedgehogTileId = HedgehogHabitat?.id;
            var hedgehogHomeIds = UsesGreenPatches
                ? HedgehogHabitats.Select(tile => tile.id).ToHashSet()
                : new HashSet<string>();
            var foxTileId = FoxHabitat?.id;
            var pigeonGroups = UsesGreenPatches
                ? PigeonGroups(PigeonPopulation).ToDictionary(group =>
                    group.Origin.id, group => group.Count)
                : new Dictionary<string, int>();
            var squirrelHomes = ConstructionFoodModel.SquirrelHomeTiles(board,
                    CurrentDay, SquirrelPopulation, UsesGreenPatches)
                .Select(home => home.id).ToHashSet();
            var count = 0;
            foreach (var neighbor in board.BuiltTiles.Where(tile =>
                         tile.category == ConstructionCategory.Green &&
                         ConstructionBoardModel.AreNeighbours(street.column,
                             street.row, tile.column, tile.row)))
            {
                if (squirrelHomes.Contains(neighbor.id)) count++;
                if (UsesGreenPatches)
                {
                    if (pigeonGroups.TryGetValue(neighbor.id,
                            out var localPigeons)) count += localPigeons;
                    if (hedgehogHomeIds.Contains(neighbor.id)) count++;
                }
                else
                {
                    if (neighbor.id == PigeonTileId)
                        count += PigeonPopulation;
                    if (neighbor.id == hedgehogTileId) count++;
                }
                if (neighbor.id == foxTileId) count++;
            }
            return count;
        }
        public int StreetTrafficDeathCount(string streetTileId) =>
            survival.trafficDeaths?.FirstOrDefault(item =>
                item.streetTileId == streetTileId)?.count ?? 0;
        public int GreenDailyYield(string tileId)
        {
            var tile = board.BuiltTiles.FirstOrDefault(item => item.id == tileId);
            return tile == null ? 0 : ConstructionFoodModel.DailyYield(tile,
                CurrentDay, score.HasVariableGreenYield, RunId,
                UsesGreenUpgrades ? GreenUpgradeLevel(tileId) : 1);
        }

        public ConstructionRunModel(ConstructionGameMode mode = ConstructionGameMode.Fill,
            bool useFillHand = false)
        {
            Mode = mode;
            awardRulesVersion = ConstructionAwardModel.CurrentRulesVersion;
            usesFillHand = mode == ConstructionGameMode.Fill && useFillHand;
            fillHandDeckVersion = usesFillHand ? 3 : 0;
            greenPatchRulesVersion = usesFillHand ? 1 : 0;
            endlessUpgradeRulesVersion = mode == ConstructionGameMode.Endless ? 1 : 0;
            // Version 2 is the physical random-draw rule set. Restored
            // BoardGame saves with version 0/1 keep their original rules.
            introRulesVersion = mode == ConstructionGameMode.BoardGame ? 2 : 1;
            RunId = Guid.NewGuid().ToString("D");
            survival = new ConstructionSurvivalSaveData();
            board = new ConstructionBoardModel(mode == ConstructionGameMode.Endless,
                includeStarterOak: !UsesFillHand);
            story = new ConstructionStoryModel();
            score = new ConstructionScoreLedger(UsesFiniteFillDeck || mode == ConstructionGameMode.Endless
                ? ConstructionScoreLedger.CurrentScoringVersion : 9);
            food = new ConstructionFoodModel();
            waste = new ConstructionWasteModel();
            CurrentDay = 1;
            story.Observe(board);
            InitializeSpecialTasks();
            InitializePlacementScore();
        }

        public void RecordSquirrelMeal()
        {
            if (IsFinished || story.Stage != ConstructionStoryStage.WatchSquirrelEat) return;
            story.RecordSquirrelMeal();
            story.Observe(board);
        }

        public bool InspectPigeonMeadow(string tileId)
        {
            if (IsFinished || string.IsNullOrEmpty(PigeonTileId) ||
                tileId != PigeonTileId ||
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
            out ConstructionPlacementFailure placementFailure,
            ResidenceKind residenceKind = ResidenceKind.Standard)
        {
            built = null;
            placementFailure = ConstructionPlacementFailure.None;
            var wasFillIntro = UsesFillHand && IsIntroPhase;
            if (category == ConstructionCategory.Green && planting == GreenPlanting.None)
                planting = ResolveGreenPlanting(column, row);
            if (IsFinished || board.IsFull)
            {
                failure = ConstructionBuildFailure.RunFinished;
                return false;
            }
            story.Observe(board);
            if (IsIntroPhase && IsIntroTypeUsed(category))
            {
                placementFailure = ConstructionPlacementFailure.IntroTypeAlreadyBuilt;
                failure = ConstructionBuildFailure.IntroTypeAlreadyBuilt;
                return false;
            }
            if (!IsUnlocked(category))
            {
                if (IsIntroPhase &&
                    Enum.IsDefined(typeof(ConstructionCategory), category))
                    placementFailure = ConstructionPlacementFailure.IntroTypeNotNext;
                failure = ConstructionBuildFailure.LockedByStory;
                return false;
            }
            // An invalid attempt can still reveal an access problem in the
            // story, even if this day's one construction slot is already used.
            var check = CheckPlacement(column, row, category, planting,
                residenceKind);
            if (check == ConstructionPlacementFailure.TypeLimitReached)
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
            if (check != ConstructionPlacementFailure.None)
            {
                placementFailure = check;
                if (check == ConstructionPlacementFailure.NeedsHomeOrConnectedStreet &&
                    story.Stage == ConstructionStoryStage.AwaitAccessProblem)
                {
                    story.RecordDirectAccessBlocked();
                    story.Observe(board);
                }
                failure = ConstructionBuildFailure.InvalidPlacement;
                return false;
            }
            var placed = UsesFillHand && board.BuiltCount == 0
                ? board.TryPlaceStarterOak(column, row, out built,
                    out placementFailure, CurrentDay)
                : board.TryBuild(column, row, category, planting,
                    out built, out placementFailure, CurrentDay,
                    residenceKind: residenceKind);
            if (!placed)
            {
                failure = ConstructionBuildFailure.InvalidPlacement;
                return false;
            }
            RecordPlacementScore(built);
            AwardSpecialTaskBuildEffect(built);
            RefreshWildlifeReturns();
            story.Observe(board);
            if (UsesFillHand)
                AdvanceFillHandAfterBuild(category, wasFillIntro);
            UpdateSpecialTasksAfterBuild(category);
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
        // V7 points come from distinct food areas available within one step
        // of occupied animal habitat; meals still determine survival.
        public bool TrySimulateDay(out ConstructionEcologyDayResult ecology)
        {
            ecology = null;
            if (IsFinished || UsesFillHand && !BuiltToday ||
                score.Days.Count != CurrentDay - 1 ||
                food.LastSettledDay >= CurrentDay) return false;
            RefreshWildlifeReturns();
            var availability = PreviewWasteAvailability();
            var expectedEcology = PreviewEcologyDay();
            var expectedHuman = ConstructionHumanModel.SettleDay(CurrentDay, board,
                availability.BlockedHomes, availability.BlockedFoodServices,
                DepartedHomes, residentDispatches,
                score.ScoringVersion >= 11, UsesNeighborUpgrades);
            if (score.HasLinkageScore)
                ConstructionLinkageScorer.Apply(board, CurrentDay,
                    expectedEcology, expectedHuman,
                    score.HasLocalFoodAreaScore,
                    score.ScoringVersion >= 8);
            var forecast = new ConstructionDayForecast
            {
                day = CurrentDay,
                animalMeals = score.UsesLegacyAnimalScoring
                    ? expectedEcology.LegacyAnimalMeals : expectedEcology.AnimalMeals,
                completedWorkCycles = expectedHuman.CompletedWorkCycles,
                humanPoints = expectedHuman.SatisfactionPoints,
                wasteCleared = availability.Cleared.Values.Sum(),
                blockedHomeTileIds = availability.BlockedHomes.OrderBy(id => id).ToList(),
                blockedFoodTileIds = availability.BlockedFoodServices.OrderBy(id => id).ToList()
            };
            var foxPresent = FoxHabitat != null;
            var hunt = PlanPredation();
            var traffic = PlanTrafficCollision(hunt, expectedHuman);
            if (foxPresent) survival.foxEstablished = true;
            ApplyPredation(hunt);
            ApplyTrafficCollision(traffic);
            ecology = food.SettleDay(CurrentDay, board, PigeonTileId,
                SquirrelPopulation, PigeonPopulation,
                hunt.species == ConstructionPreySpecies.Hedgehog ||
                    traffic.species == ConstructionTrafficSpecies.Hedgehog
                    ? null : HedgehogHabitat?.id,
                UrbanFoodSources(availability, expectedHuman),
                score.HasVariableGreenYield, RunId,
                score.HasLocalFoodAreaScore, UsesGreenUpgrades,
                UsesGreenPatches,
                UsesGreenPatches ? HedgehogHabitats.Select(tile => tile.id)
                    .ToArray() : null, score.HasFedAnimalScore);
            ecology.pigeonHabitatTileId = ecology.pigeonsPresent > 0
                ? PigeonTileId : null;
            ecology.hedgehogHabitatTileId = ecology.hedgehogPresent
                ? ecology.hedgehogHomeTileIds.FirstOrDefault() : null;
            MarkPredation(ecology, hunt);
            MarkTraffic(ecology, traffic);
            ApplyEnhancedGreenScore(ecology);
            var human = ConstructionHumanModel.SettleDay(CurrentDay, board,
                availability.BlockedHomes, availability.BlockedFoodServices,
                DepartedHomes, residentDispatches,
                score.ScoringVersion >= 11, UsesNeighborUpgrades);
            var linkage = score.HasLinkageScore
                ? ConstructionLinkageScorer.Apply(board, CurrentDay, ecology,
                    human, score.HasLocalFoodAreaScore,
                    score.ScoringVersion >= 8)
                : null;
            var supermarketForaged = ecology.urbanMealTileIds
                .Where(id => board.BuiltTiles.Any(tile => tile.id == id &&
                    tile.category == ConstructionCategory.Supermarket))
                .GroupBy(id => id).ToDictionary(group => group.Key,
                    group => group.Count());
            var wasteDay = waste.SettleDay(CurrentDay, board, human, availability,
                supermarketForaged, UsesUncappedWaste);
            forecast.wasteCleared = wasteDay.Cleared;
            if (ecology.squirrelAte && story.Stage ==
                ConstructionStoryStage.WatchSquirrelEat)
            {
                story.RecordSquirrelMeal();
                story.Observe(board);
            }
            if (!FinishDay(score.UsesLegacyAnimalScoring
                    ? ecology.LegacyAnimalMeals : ecology.AnimalMeals,
                    human.CompletedWorkCycles, human, ecology.animalPoints,
                    human.SatisfactionPoints, linkage,
                    score.HasTrafficPenaltyScore && traffic.Happened
                        ? score.CollisionPenalty : 0))
                throw new InvalidOperationException("Daily score rejected a settled ecology day.");
            if (Mode == ConstructionGameMode.Endless && !IsFinished)
            {
                SettleSurvival(ecology, human);
                if (!IsFinished) RefreshWildlifeReturns();
            }
            SettleFoxActivity(foxPresent, hunt, ecology.day);
            if (traffic.species == ConstructionTrafficSpecies.Fox)
            {
                survival.foxEstablished = false;
                survival.foxHungryDays = 0;
                survival.foxReturnDay = ecology.day + 3;
            }
            // The flock may visit a supermarket or waste room for food, but
            // its persistent home must remain a green tile.
            var pigeonFoodTileId = ecology.pigeonFoodTileId;
            if (board.BuiltTiles.Any(tile => tile.id == pigeonFoodTileId &&
                tile.category == ConstructionCategory.Green))
                PigeonTileId = pigeonFoodTileId;
            LastEcologyDay = ecology;
            LastHumanDay = human;
            LastWasteDay = wasteDay;
            LastForecast = forecast;
            AwardFillHandProgress(ecology, human);
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
                    RegisterWildlifeLoss(ConstructionWildlifeSpecies.Pigeon,
                        CurrentDay);
                    break;
                case ConstructionPreySpecies.Squirrel:
                    if (Mode == ConstructionGameMode.Endless)
                    {
                        survival.squirrels--;
                        survival.squirrelFedDays = 0;
                    }
                    else survival.predatedSquirrels++;
                    RegisterWildlifeLoss(ConstructionWildlifeSpecies.Squirrel,
                        CurrentDay);
                    break;
                case ConstructionPreySpecies.Hedgehog:
                    RegisterWildlifeLoss(ConstructionWildlifeSpecies.Hedgehog,
                        CurrentDay, hunt.preyTileId);
                    break;
            }
            if (Mode == ConstructionGameMode.Endless &&
                hunt.species != ConstructionPreySpecies.None)
                RecordAnimalDeath(CurrentDay);
        }

        private void ApplyTrafficCollision(ConstructionTrafficCollision collision)
        {
            if (!collision.Happened) return;
            survival.trafficDeaths ??= new List<ConstructionTrafficDeathSaveData>();
            var record = survival.trafficDeaths.FirstOrDefault(item =>
                item.streetTileId == collision.streetTileId);
            if (record == null)
            {
                record = new ConstructionTrafficDeathSaveData
                    { streetTileId = collision.streetTileId };
                survival.trafficDeaths.Add(record);
            }
            record.count++;
            if (Mode == ConstructionGameMode.Endless &&
                collision.species != ConstructionTrafficSpecies.None)
                RecordAnimalDeath(CurrentDay);
            switch (collision.species)
            {
                case ConstructionTrafficSpecies.Squirrel:
                    if (Mode == ConstructionGameMode.Endless)
                    {
                        survival.squirrels--;
                        survival.squirrelFedDays = 0;
                    }
                    else survival.trafficKilledSquirrels++;
                    RegisterWildlifeLoss(ConstructionWildlifeSpecies.Squirrel,
                        CurrentDay);
                    break;
                case ConstructionTrafficSpecies.Pigeon:
                    if (Mode == ConstructionGameMode.Endless)
                    {
                        survival.pigeons--;
                        survival.pigeonFedDays = 0;
                    }
                    else survival.trafficKilledPigeons++;
                    RegisterWildlifeLoss(ConstructionWildlifeSpecies.Pigeon,
                        CurrentDay);
                    break;
                case ConstructionTrafficSpecies.Hedgehog:
                    RegisterWildlifeLoss(ConstructionWildlifeSpecies.Hedgehog,
                        CurrentDay, collision.wildlifeTileId);
                    break;
            }
        }

        private void RegisterWildlifeLoss(ConstructionWildlifeSpecies species,
            int settledDay, string habitatId = null)
        {
            switch (species)
            {
                case ConstructionWildlifeSpecies.Squirrel:
                    survival.squirrelFedDays = 0;
                    if (Mode != ConstructionGameMode.Endless)
                    {
                        survival.squirrelReturnDays.Add(settledDay + 3);
                        survival.squirrelReturnCeiling = Math.Min(
                            survival.squirrelReturnCeiling, SquirrelPopulation);
                    }
                    else if (survival.squirrels == 0 &&
                        survival.squirrelExtinctDay == 0)
                        survival.squirrelExtinctDay = settledDay;
                    break;
                case ConstructionWildlifeSpecies.Pigeon:
                    survival.pigeonFedDays = 0;
                    if (Mode != ConstructionGameMode.Endless)
                    {
                        survival.pigeonReturnDays.Add(settledDay + 3);
                        survival.pigeonReturnCeiling = Math.Min(
                            survival.pigeonReturnCeiling, PigeonPopulation);
                    }
                    else if (survival.pigeons == 0 &&
                        survival.pigeonExtinctDay == 0)
                        survival.pigeonExtinctDay = settledDay;
                    break;
                case ConstructionWildlifeSpecies.Hedgehog:
                    if (UsesGreenPatches)
                    {
                        MarkHedgehogPatchAbsent(habitatId, settledDay + 3);
                        break;
                    }
                    survival.hedgehogAbsent = true;
                    survival.hedgehogReturnDay = settledDay + 3;
                    survival.hedgehogNextArrivalShrubs = 0;
                    break;
            }
        }

        private void MarkHedgehogPatchAbsent(string homeId, int returnDay)
        {
            if (string.IsNullOrEmpty(homeId)) return;
            survival.hedgehogPatches ??=
                new List<ConstructionHedgehogPatchState>();
            var state = survival.hedgehogPatches.FirstOrDefault(entry =>
                entry.homeTileId == homeId);
            if (state == null)
            {
                state = new ConstructionHedgehogPatchState
                    { homeTileId = homeId };
                survival.hedgehogPatches.Add(state);
            }
            state.hungryDays = 0;
            state.returnDay = returnDay;
        }

        private void RecordAnimalDeath(int settledDay)
        {
            if (Mode != ConstructionGameMode.Endless ||
                !EndlessLossRulesConfirmed) return;
            survival.animalDeathDays ??= new List<int>();
            survival.animalDeathDays.Add(settledDay);
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
            => FinishDay(animalMeals, completedWorkCycles, null, animalMeals,
                completedWorkCycles);

        private bool FinishDay(int animalMeals, int completedWorkCycles,
            ConstructionHumanDayResult observedHuman, int animalPoints,
            int humanPoints, ConstructionLinkageDayResult linkage = null,
            int trafficPenaltyPoints = 0)
        {
            if (IsFinished || !score.TryRecordDay(CurrentDay, animalMeals,
                    completedWorkCycles, animalPoints, humanPoints,
                    linkage?.sharedSquareMeals ?? 0,
                    linkage?.sharedSupermarketMeals ?? 0,
                    linkage?.sharedGreenMeals ?? 0,
                    linkageRulesApplied: observedHuman != null,
                    trafficPenaltyPoints: trafficPenaltyPoints,
                    trafficPenaltyRulesApplied: observedHuman != null,
                    biodiversityHomes: linkage?.biodiversityHomes ?? 0)) return false;
            AwardCompletedCommunities(observedHuman);
            AwardSpecialTaskDayEffect(animalMeals, observedHuman, linkage);
            food.MarkDayWithoutSimulation(CurrentDay);
            waste.MarkDayWithoutSimulation(CurrentDay);
            LastEcologyDay = null;
            LastHumanDay = null;
            LastWasteDay = null;
            LastForecast = null;
            var nextDay = CurrentDay + 1;
            if (string.IsNullOrEmpty(PigeonTileId) &&
                (UsesGreenPatches ||
                 story.Stage == ConstructionStoryStage.GrowGreen))
            {
                var meadow = UsesGreenPatches
                    ? ConstructionGreenPatchModel.Find(board, nextDay)
                        .Where(patch => patch.PigeonCapacity >= 2)
                        .SelectMany(patch => patch.Meadows.Take(1))
                        .FirstOrDefault()
                    : ConnectedMeadows().FirstOrDefault(tile =>
                        Math.Max(1, tile.builtDay) < nextDay);
                if (meadow != null)
                {
                    PigeonTileId = meadow.id;
                    PigeonArrivalDay = nextDay;
                    if (Mode == ConstructionGameMode.Endless)
                        survival.pigeons = ConstructionFoodModel.PigeonFlockSize;
                    story.RecordNewWildlifeArrival();
                    if (story.Stage == ConstructionStoryStage.GrowGreen)
                    {
                        story.Observe(board);
                    }
                }
            }
            if (!story.ResidentAte &&
                (observedHuman == null
                    ? story.Stage == ConstructionStoryStage.WatchResidentEat &&
                      board.FirstResidenceServiceRoute(ConstructionCategory.Restaurant,
                          CurrentDay).Count > 0
                    : observedHuman.residents.Any(person =>
                        person.homeTileId == FirstResidentHomeTileId && person.ate)))
            {
                FirstResidentMealDay = CurrentDay;
                story.RecordResidentMeal();
                story.Observe(board);
            }
            if (story.Stage == ConstructionStoryStage.AwaitRestaurantCrowding &&
                observedHuman != null &&
                (observedHuman.restaurantRejectedForCapacity > 0 ||
                 observedHuman.restaurantServed >=
                 ConstructionHumanModel.DailyMealCapacity(
                     ConstructionCategory.Restaurant)))
            {
                story.RecordRestaurantCrowded();
                story.Observe(board);
            }
            UpdateSpecialTasksAfterSettlement(observedHuman);
            if (board.IsFull && Mode != ConstructionGameMode.Endless)
            {
                IsFinished = true;
                EndReason = ConstructionEndReason.BoardComplete;
                AwardSpecialTaskEndgameEffect();
            }
            else
            {
                CurrentDay++;
                if (Mode != ConstructionGameMode.Endless)
                    RefreshWildlifeReturns();
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
            var squirrelStarved = UpdateAnimalPopulation(ref squirrels,
                ref squirrelHungryDays, ref squirrelFedDays,
                ecology.squirrelsFed, ecology.squirrelsPresent,
                SquirrelHabitatCapacity(ecology.day));
            if (squirrelStarved) RecordAnimalDeath(ecology.day);
            survival.squirrels = squirrels;
            survival.squirrelHungryDays = squirrelHungryDays;
            survival.squirrelFedDays = squirrelFedDays;
            if (survival.squirrels == 0 &&
                survival.squirrelExtinctDay == 0)
                survival.squirrelExtinctDay = ecology.day;
            var pigeons = survival.pigeons;
            var pigeonHungryDays = survival.pigeonHungryDays;
            var pigeonFedDays = survival.pigeonFedDays;
            var pigeonStarved = UpdateAnimalPopulation(ref pigeons,
                ref pigeonHungryDays, ref pigeonFedDays,
                ecology.pigeonsFed, ecology.pigeonsPresent,
                PigeonHabitatCapacity(ecology.day));
            if (pigeonStarved) RecordAnimalDeath(ecology.day);
            survival.pigeons = pigeons;
            survival.pigeonHungryDays = pigeonHungryDays;
            survival.pigeonFedDays = pigeonFedDays;
            // Pigeons are represented as one social flock. A lone survivor
            // leaves the site rather than continuing as a one-bird "flock".
            if (PigeonArrivalDay > 0 && survival.pigeons == 1)
                survival.pigeons = 0;
            if (PigeonArrivalDay > 0 && survival.pigeons == 0 &&
                survival.pigeonExtinctDay == 0)
                survival.pigeonExtinctDay = ecology.day;
            if (UsesGreenPatches)
                foreach (var homeId in ecology.hedgehogHomeTileIds)
                {
                    survival.hedgehogPatches ??=
                        new List<ConstructionHedgehogPatchState>();
                    var state = survival.hedgehogPatches.FirstOrDefault(entry =>
                        entry.homeTileId == homeId);
                    if (state == null)
                    {
                        state = new ConstructionHedgehogPatchState
                            { homeTileId = homeId };
                        survival.hedgehogPatches.Add(state);
                    }
                    if (ecology.hedgehogMealHomeTileIds.Contains(homeId))
                    {
                        state.hungryDays = 0;
                        state.returnDay = 0;
                    }
                    else if (++state.hungryDays >= 3)
                        MarkHedgehogPatchAbsent(homeId, ecology.day + 3);
                }
            // The first service pair gives residents a fair chance to complete
            // a commute before absence can count toward departure.
            var routeSystemExists = board.BuiltTiles.Any(tile => tile.category is
                ConstructionCategory.Workshop or ConstructionCategory.Restaurant or
                ConstructionCategory.Supermarket) &&
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
            survival.animalDeathDays ??= new List<int>();
            survival.animalDeathDays.RemoveAll(day =>
                day < ecology.day - AnimalDeathWindowDays + 1);
            survival.unemployedResidentsLastDay = human.residents.Count(person =>
                !person.worked);
            survival.unemployedDaysInRow = EndlessLossRulesConfirmed &&
                human.ResidentCount > 0 &&
                survival.unemployedResidentsLastDay >= UnemployedResidentLimit
                    ? survival.unemployedDaysInRow + 1 : 0;
            if (EndlessLossRulesConfirmed &&
                RecentAnimalDeaths >= AnimalDeathLimit)
                EndReason = ConstructionEndReason.AnimalDeathsLimit;
            else if (EndlessLossRulesConfirmed &&
                survival.unemployedDaysInRow >= UnemployedConsecutiveDays)
                EndReason = ConstructionEndReason.ResidentsUnemployed;
            else if (survival.squirrels == 0 &&
                ecology.day >= survival.squirrelExtinctDay + 5 ||
                PigeonArrivalDay > 0 && survival.pigeons == 0 &&
                ecology.day >= survival.pigeonExtinctDay + 5)
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

        // Return is immigration, never resurrection of the animal recorded
        // as killed. Day d+3 is the first eligible morning after a loss on d.
        // Food and habitat checks are repeated after each build and day turn;
        // a failed check leaves the appointment pending rather than spawning
        // an animal into the same starvation trap.
        private void RefreshWildlifeReturns()
        {
            if (IsFinished) return;
            if (Mode != ConstructionGameMode.Endless)
            {
                if (survival.squirrelReturnDays.Count > 0 &&
                    survival.squirrelReturnDays[0] <= CurrentDay &&
                    survival.lastSquirrelReturnDay != CurrentDay)
                {
                    var status = ReturnStatus(ConstructionWildlifeSpecies.Squirrel);
                    if (status.HabitatReady && status.FoodReady ||
                        SquirrelPopulation >= SquirrelHabitatCapacity(CurrentDay))
                    {
                        survival.squirrelReturnDays.RemoveAt(0);
                        survival.returnedSquirrels++;
                        survival.lastSquirrelReturnDay = CurrentDay;
                        survival.squirrelReturnCeiling =
                            survival.squirrelReturnDays.Count == 0
                                ? int.MaxValue
                                : Math.Min(SquirrelHabitatCapacity(CurrentDay),
                                    survival.squirrelReturnCeiling + 1);
                    }
                }
                if (survival.pigeonReturnDays.Count > 0 &&
                    survival.pigeonReturnDays[0] <= CurrentDay &&
                    survival.lastPigeonReturnDay != CurrentDay)
                {
                    var status = ReturnStatus(ConstructionWildlifeSpecies.Pigeon);
                    if (status.HabitatReady && status.FoodReady ||
                        PigeonPopulation >= PigeonHabitatCapacity(CurrentDay))
                    {
                        survival.pigeonReturnDays.RemoveAt(0);
                        survival.returnedPigeons++;
                        survival.lastPigeonReturnDay = CurrentDay;
                        survival.pigeonReturnCeiling =
                            survival.pigeonReturnDays.Count == 0
                                ? int.MaxValue
                                : Math.Min(PigeonHabitatCapacity(CurrentDay),
                                    survival.pigeonReturnCeiling + 1);
                    }
                }
            }
            else
            {
                if (survival.squirrelExtinctDay > 0 && survival.squirrels == 0 &&
                    CurrentDay >= survival.squirrelExtinctDay + 3)
                {
                    var status = ReturnStatus(ConstructionWildlifeSpecies.Squirrel);
                    if (status.HabitatReady && status.FoodReady)
                    {
                        survival.squirrels = 1;
                        survival.squirrelExtinctDay = 0;
                        survival.squirrelHungryDays = 0;
                        survival.squirrelFedDays = 0;
                    }
                }
                if (survival.pigeonExtinctDay > 0 && survival.pigeons == 0 &&
                    CurrentDay >= survival.pigeonExtinctDay + 3)
                {
                    var status = ReturnStatus(ConstructionWildlifeSpecies.Pigeon);
                    if (status.HabitatReady && status.FoodReady)
                    {
                        survival.pigeons = ConstructionFoodModel.PigeonFlockSize;
                        survival.pigeonExtinctDay = 0;
                        survival.pigeonHungryDays = 0;
                        survival.pigeonFedDays = 0;
                    }
                }
            }
            if (survival.hedgehogAbsent &&
                CurrentDay >= survival.hedgehogReturnDay)
            {
                var status = ReturnStatus(ConstructionWildlifeSpecies.Hedgehog);
                if (status.HabitatReady && status.FoodReady)
                {
                    survival.hedgehogAbsent = false;
                    survival.hedgehogReturnDay = 0;
                }
            }
        }

        private static bool UpdateAnimalPopulation(ref int count,
            ref int hungryDays, ref int fedDays, int fed, int present,
            int habitatCapacity)
        {
            if (count == 0) return false;
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
                    return true;
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
            return false;
        }

        private IEnumerable<ConstructionTileData> ConnectedMeadows() =>
            board.AnimalReachableGreenTiles(ConstructionBoardModel.StarterOakId)
                .Where(tile => tile.planting == GreenPlanting.Meadow);

        public ConstructionRunSaveData Export() => new()
        {
            runId = RunId,
            awardRulesVersion = AwardRulesVersion,
            wildlifeReturnRulesVersion = 1,
            introRulesVersion = introRulesVersion,
            usesFillHand = UsesFillHand,
            fillHandDeckVersion = UsesFillHand ? fillHandDeckVersion : 0,
            greenPatchRulesVersion = UsesGreenPatches ? 1 : 0,
            fillHandRandomState = UsesFillHand ? fillHandRandomState : 0,
            fillHandCards = UsesFillHand
                ? fillHandCards.ToList() : new List<ConstructionCategory>(),
            fillHandWitnessedLinkage = UsesFiniteFillDeck &&
                fillHandWitnessedLinkage,
            fillHandExpanded = UsesFiniteFillDeck && fillHandExpanded,
            fillHandExpandedToFive = UsesFiniteFillDeck && fillHandExpandedToFive,
            fillHandFedSpeciesMask = UsesFiniteFillDeck
                ? fillHandFedSpeciesMask : 0,
            fillHandRerollsSpent = UsesFiniteFillDeck
                ? fillHandRerollsSpent : 0,
            specialTasks = ExportSpecialTasks(),
            placementScore = ExportPlacementScore(),
            tutorialTipsSkipped = TutorialTipsSkipped,
            fillTutorialSkipped = FillTutorialSkipped,
            fillTutorialSkipBuiltCount = FillTutorialSkipBuiltCount,
            openingTourCompleted = OpeningTourCompleted,
            endlessLossRulesVersion = 1,
            animalDeathLimit = AnimalDeathLimit,
            unemployedResidentLimit = UnemployedResidentLimit,
            endlessLossRulesConfirmed = EndlessLossRulesConfirmed,
            endlessUpgradeRulesVersion = endlessUpgradeRulesVersion,
            mode = Mode,
            endReason = EndReason,
            survival = CopySurvival(survival),
            board = board.Export(),
            story = story.Export(),
            score = score.Export(),
            food = food.Export(),
            waste = waste.Export(),
            residentDispatches = residentDispatches.Select(CopyDispatch).ToList(),
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
            // Accept hands saved under the former 2/4-square thresholds;
            // active hands are reconciled to the current 3/6 rule below.
            if (saved == null ||
                saved.awardRulesVersion < 0 ||
                saved.awardRulesVersion > ConstructionAwardModel.CurrentRulesVersion ||
                (!string.IsNullOrEmpty(saved.runId) &&
                 !Guid.TryParse(saved.runId, out _)) ||
                (saved.usesFillHand && string.IsNullOrEmpty(saved.runId)) ||
                saved.fillHandDeckVersion is < 0 or > 3 ||
                 (saved.fillTutorialSkipped &&
                  (!saved.usesFillHand || saved.fillTutorialSkipBuiltCount < 0 ||
                   saved.fillTutorialSkipBuiltCount >=
                       ConstructionTutorialGuide.RoomCount ||
                    saved.fillTutorialSkipBuiltCount >
                        (saved.board?.tiles?.Count ?? -1))) ||
                saved.greenPatchRulesVersion is < 0 or > 1 ||
                (saved.greenPatchRulesVersion == 1 &&
                 (!saved.usesFillHand || saved.fillHandDeckVersion < 1)) ||
                (!saved.usesFillHand && saved.fillHandDeckVersion != 0) ||
                saved.introRulesVersion is < 0 or > 2 ||
                (saved.usesFillHand &&
                    (saved.mode != ConstructionGameMode.Fill ||
                     saved.introRulesVersion != 1 ||
                     saved.fillHandCards == null ||
                     saved.fillHandCards.Count > 5 ||
                     saved.fillHandCards.Distinct().Count() !=
                         saved.fillHandCards.Count ||
                     saved.fillHandCards.Any(category =>
                         !Enum.IsDefined(typeof(ConstructionCategory), category) ||
                          saved.fillHandDeckVersion == 2 &&
                          IsBasicFillResource(category)))) ||
                (saved.introRulesVersion == 2 &&
                 saved.mode != ConstructionGameMode.BoardGame) ||
                saved.wildlifeReturnRulesVersion is < 0 or > 1 ||
                saved.endlessLossRulesVersion is < 0 or > 1 ||
                 saved.endlessUpgradeRulesVersion is < 0 or > 1 ||
                 (saved.endlessUpgradeRulesVersion != 0 &&
                     saved.mode != ConstructionGameMode.Endless) ||
                (saved.endlessLossRulesVersion == 1 &&
                    (saved.animalDeathLimit is < MinimumEndlessLossLimit or
                        > MaximumEndlessLossLimit ||
                     saved.unemployedResidentLimit is < MinimumEndlessLossLimit or
                        > MaximumEndlessLossLimit)) ||
                !ConstructionBoardModel.TryRestore(saved.board, out var board,
                    saved.mode == ConstructionGameMode.Endless,
                    allowEmptyStarter: saved.usesFillHand) ||
                !ConstructionStoryModel.TryRestore(saved.story, board, out var story) ||
                !ConstructionScoreLedger.TryRestore(saved.score, out var score) ||
                (saved.usesFillHand &&
                    (saved.currentDay < 1 ||
                     (board.BuiltCount < ConstructionTutorialGuide.RoomCount &&
                          !saved.fillTutorialSkipped
                         ? saved.fillHandCards.Count != 0
                         : saved.fillHandCards.Count > (board.IsFull ? 0 :
                             saved.fillHandExpandedToFive ? 5 :
                             saved.fillHandExpanded ? 4 : 3)) ||
                     (saved.fillHandCards.Count > 0 &&
                      saved.fillHandRandomState == 0))) ||
                (saved.fillHandDeckVersion == 0 &&
                 (saved.fillHandWitnessedLinkage || saved.fillHandExpanded ||
                  saved.fillHandExpandedToFive ||
                  saved.fillHandFedSpeciesMask != 0 ||
                  saved.fillHandRerollsSpent != 0)) ||
                saved.fillHandFedSpeciesMask is < 0 or > 7 ||
                saved.fillHandRerollsSpent < 0 ||
                saved.fillHandRerollsSpent >
                     (saved.fillHandDeckVersion >= 2 ? 2 : 0) +
                    ((saved.fillHandFedSpeciesMask & 1) != 0 ? 1 : 0) +
                    ((saved.fillHandFedSpeciesMask & 2) != 0 ? 1 : 0) +
                    ((saved.fillHandFedSpeciesMask & 4) != 0 ? 1 : 0) ||
                (saved.fillHandExpanded &&
                 board.BuiltTiles.Count(tile =>
                     tile.category == ConstructionCategory.Square) < 2) ||
                (saved.fillHandExpandedToFive &&
                 (!saved.fillHandExpanded ||
                  board.BuiltTiles.Count(tile =>
                      tile.category == ConstructionCategory.Square) < 4)) ||
                (saved.fillHandDeckVersion >= 1 &&
                 Enum.GetValues(typeof(ConstructionCategory))
                     .Cast<ConstructionCategory>().Any(category =>
                         board.BuiltTiles.Count(tile =>
                             tile.category == category) +
                         saved.fillHandCards.Count(card =>
                             card == category) >
                         DigitalFillCardSupply(category))) ||
                board.BuiltTiles.Any(tile =>
                    (tile.builtDay == 0 ? 1 : tile.builtDay) > saved.currentDay) ||
                saved.currentDay < 1 ||
                story.NewWildlifeArrived != !string.IsNullOrEmpty(saved.pigeonTileId) ||
                (saved.pigeonMeadowInspected && string.IsNullOrEmpty(saved.pigeonTileId)) ||
                (string.IsNullOrEmpty(saved.pigeonTileId)
                    ? saved.pigeonArrivalDay != 0
                    : saved.pigeonArrivalDay < 2 ||
                      saved.pigeonArrivalDay > saved.currentDay + (saved.finished ? 1 : 0) ||
                      !(saved.greenPatchRulesVersion == 1
                          ? HasValidPatchPigeonHistory(board, saved.pigeonArrivalDay,
                              saved.currentDay + (saved.finished ? 1 : 0), saved.pigeonTileId)
                          : board.AnimalReachableGreenTiles(
                                  ConstructionBoardModel.StarterOakId,
                                  useLegacyEightNeighbours: true)
                              .Any(tile => tile.id == saved.pigeonTileId &&
                                  tile.planting == GreenPlanting.Meadow) &&
                            board.AnimalReachableGreenTiles(
                                  ConstructionBoardModel.StarterOakId,
                                  useLegacyEightNeighbours: true)
                              .Any(tile => tile.planting ==
                                  GreenPlanting.Meadow &&
                                  Math.Max(1, tile.builtDay) <
                                  saved.pigeonArrivalDay))) ||
                story.ResidentAte != (saved.firstResidentMealDay > 0) ||
                (saved.firstResidentMealDay != 0 &&
                    (saved.firstResidentMealDay > saved.currentDay ||
                     board.FirstResidenceServiceRoute(
                         ConstructionCategory.Restaurant,
                         saved.firstResidentMealDay,
                         useLegacyEightNeighbours: true).Count == 0 &&
                     board.FirstResidenceServiceRoute(
                         ConstructionCategory.Supermarket,
                         saved.firstResidentMealDay,
                         useLegacyEightNeighbours: true).Count == 0)) ||
                (saved.finished && score.Days.Count == 0) ||
                (saved.mode != ConstructionGameMode.Endless &&
                    saved.finished && !board.IsFull) ||
                (saved.mode != ConstructionGameMode.Endless && board.ExpansionRings != 0) ||
                !Enum.IsDefined(typeof(ConstructionGameMode), saved.mode) ||
                !Enum.IsDefined(typeof(ConstructionEndReason), saved.endReason) ||
                (!saved.finished && saved.endReason != ConstructionEndReason.None) ||
                (saved.mode != ConstructionGameMode.Endless &&
                    saved.endReason is ConstructionEndReason.ResidentsGone or
                        ConstructionEndReason.WildlifeGone or
                        ConstructionEndReason.AnimalDeathsLimit or
                        ConstructionEndReason.ResidentsUnemployed) ||
                (saved.mode == ConstructionGameMode.Endless &&
                    (saved.endReason == ConstructionEndReason.BoardComplete ||
                     saved.finished && saved.endReason == ConstructionEndReason.None ||
                     (saved.endlessLossRulesVersion == 0 ||
                      !saved.endlessLossRulesConfirmed) &&
                     (saved.endReason == ConstructionEndReason.AnimalDeathsLimit ||
                      saved.endReason == ConstructionEndReason.ResidentsUnemployed))) ||
                (saved.mode == ConstructionGameMode.Endless &&
                    (saved.survival == null || saved.survival.residents == null ||
                     saved.survival.squirrels < 0 || saved.survival.pigeons < 0 ||
                     saved.survival.squirrelHungryDays < 0 ||
                     saved.survival.pigeonHungryDays < 0 ||
                     saved.survival.squirrelFedDays < 0 ||
                     saved.survival.pigeonFedDays < 0 ||
                      (saved.endlessLossRulesVersion == 1 &&
                          (saved.survival.animalDeathDays == null ||
                           saved.survival.animalDeathDays.Any(day =>
                               day < 1 || day > saved.currentDay -
                                   (saved.finished ? 0 : 1) ||
                               day < saved.currentDay -
                                   (saved.finished ? 0 : 1) -
                                   AnimalDeathWindowDays + 1) ||
                           !saved.survival.animalDeathDays.SequenceEqual(
                               saved.survival.animalDeathDays.OrderBy(day => day)) ||
                           saved.survival.unemployedResidentsLastDay < 0 ||
                           saved.survival.unemployedResidentsLastDay >
                               board.BuiltTiles.Count(tile => tile.category ==
                                   ConstructionCategory.Residence) ||
                           saved.survival.unemployedDaysInRow < 0 ||
                           saved.survival.unemployedDaysInRow >
                               UnemployedConsecutiveDays)) ||
                     saved.survival.residents.Any(person => person == null ||
                         person.missedDays < 0 ||
                         !board.BuiltTiles.Any(tile => tile.id == person.homeTileId &&
                             tile.category == ConstructionCategory.Residence)) ||
                     saved.survival.residents.Select(person => person.homeTileId)
                         .Distinct().Count() != saved.survival.residents.Count)) ||
                (saved.survival != null &&
                    (saved.survival.predatedSquirrels < 0 ||
                     saved.survival.predatedPigeons < 0 ||
                     saved.survival.trafficKilledSquirrels < 0 ||
                     saved.survival.trafficKilledPigeons < 0 ||
                     (saved.survival.trafficDeaths != null &&
                      (saved.survival.trafficDeaths.Any(item => item == null ||
                           item.count < 1 ||
                           !board.BuiltTiles.Any(tile =>
                               tile.id == item.streetTileId &&
                               tile.category == ConstructionCategory.Street)) ||
                       saved.survival.trafficDeaths.Select(item =>
                           item.streetTileId).Distinct().Count() !=
                           saved.survival.trafficDeaths.Count ||
                       saved.survival.trafficDeaths.Sum(item =>
                           (long)item.count) >
                           saved.currentDay)) ||
                     saved.survival.hedgehogNextArrivalShrubs < 0 ||
                     saved.survival.hedgehogNextArrivalShrubs > board.BuiltCount + 1 ||
                     saved.survival.foxHungryDays < 0 ||
                     saved.survival.foxHungryDays > 2 ||
                     saved.survival.foxReturnDay < 0)) ||
                saved.currentDay != score.Days.Count + (saved.finished ? 0 : 1))
                return false;
            if (saved.greenPatchRulesVersion == 1 &&
                (saved.survival?.hedgehogPatches == null ||
                 saved.survival.hedgehogPatches.Any(state =>
                     state == null || state.hungryDays is < 0 or > 2 ||
                     state.returnDay < 0 ||
                     state.returnDay > saved.currentDay + 3 ||
                     !board.BuiltTiles.Any(tile =>
                         tile.id == state.homeTileId &&
                         tile.planting == GreenPlanting.Shrub)) ||
                 saved.survival.hedgehogPatches.Select(state =>
                     state.homeTileId).Distinct().Count() !=
                 saved.survival.hedgehogPatches.Count))
                return false;

            ConstructionFoodModel food;
            if (saved.usesFillHand && board.BuiltCount == 0)
            {
                // The hand opening is an empty board. The food model still
                // exports its dormant starter-oak stock before that card is
                // placed, while ordinary restore requires the oak to exist.
                // Accept only that exact, unsettled opening state.
                if (saved.food == null || saved.food.lastSettledDay != 0 ||
                    saved.food.tiles == null || saved.food.tiles.Count != 1 ||
                    saved.food.tiles[0] == null ||
                    saved.food.tiles[0].tileId !=
                    ConstructionBoardModel.StarterOakId ||
                    saved.food.tiles[0].stock != 1)
                    return false;
                food = new ConstructionFoodModel();
            }
            else if (saved.food == null ||
                saved.food.lastSettledDay == 0 && saved.food.tiles?.Count == 0)
                food = ConstructionFoodModel.FromLegacySave(saved.currentDay,
                    saved.finished);
            else if (!ConstructionFoodModel.TryRestore(saved.food, board,
                         saved.currentDay, saved.finished, out food,
                         saved.endlessUpgradeRulesVersion == 1 ||
                         saved.placementScore?.version >=
                             ConstructionPlacementScore.GreenUpgradeRulesVersion))
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
            var expectedFedAnimalScore = score.HasFedAnimalScore &&
                lastEcologyDay != null
                    ? ConstructionFoodAreaScorer.ScoreFed(board,
                        lastEcologyDay, score.ScoringVersion >= 11,
                        score.ScoringVersion >= 12)
                    : null;
            if (lastEcologyDay == null &&
                score.LastDay?.trafficPenaltyRulesApplied == true) return false;
            if (lastEcologyDay != null &&
                (score.Days.Count == 0 ||
                 lastEcologyDay.day != score.Days.Last().day ||
                 (score.HasTrafficPenaltyScore &&
                  !score.LastDay.trafficPenaltyRulesApplied) ||
                 (score.LastDay.trafficPenaltyRulesApplied &&
                  score.LastDay.trafficPenaltyPoints !=
                  (lastEcologyDay.trafficSpecies ==
                   ConstructionTrafficSpecies.None ? 0 :
                   score.CollisionPenalty)) ||
                 (score.UsesLegacyAnimalScoring
                     ? lastEcologyDay.LegacyAnimalMeals
                     : lastEcologyDay.AnimalMeals) !=
                     score.Days.Last().animalMeals ||
                 (!score.UsesLegacyAnimalScoring &&
                     lastEcologyDay.animalPoints !=
                     score.Days.Last().animalPoints) ||
                 lastEcologyDay.animalPoints < 0 ||
                 (!score.HasLocalFoodAreaScore &&
                  lastEcologyDay.animalPoints >
                      3 * lastEcologyDay.AnimalMeals) ||
                 (score.HasLocalFoodAreaScore &&
                  (lastEcologyDay.scoredFoodTileIds == null ||
                   lastEcologyDay.scoredFoodTileIds.Count !=
                       lastEcologyDay.scoredFoodTileIds.Distinct().Count() ||
                   lastEcologyDay.scoredFoodTileIds.Any(id =>
                       !board.BuiltTiles.Any(tile => tile.id == id &&
                           tile.builtDay <= lastEcologyDay.day &&
                           ConstructionFoodAreaScorer.PointsFor(
                               tile.category) > 0)) ||
                    !score.HasFedAnimalScore && lastEcologyDay.animalPoints !=
                       lastEcologyDay.scoredFoodTileIds.Sum(id =>
                           ConstructionFoodAreaScorer.PointsFor(
                               board.BuiltTiles.FirstOrDefault(tile =>
                                   tile.id == id)?.category ??
                                ConstructionCategory.Residence)))) ||
                  (score.HasFedAnimalScore &&
                   (lastEcologyDay.fedScoringSourceTileIds == null ||
                    !lastEcologyDay.fedScoringSourceTileIds.SequenceEqual(
                        expectedFedAnimalScore.sourceTileIds) ||
                    lastEcologyDay.animalPoints !=
                        expectedFedAnimalScore.points)) ||
                 (lastEcologyDay.hedgehogFed &&
                     (!lastEcologyDay.hedgehogPresent ||
                      string.IsNullOrEmpty(lastEcologyDay.hedgehogFoodTileId))) ||
                 (saved.greenPatchRulesVersion == 1 &&
                     (lastEcologyDay.hedgehogHomeTileIds == null ||
                      lastEcologyDay.hedgehogMealHomeTileIds == null ||
                      lastEcologyDay.hedgehogMealTileIds == null ||
                      lastEcologyDay.hedgehogMealHomeTileIds.Count !=
                          lastEcologyDay.hedgehogMealTileIds.Count ||
                      lastEcologyDay.hedgehogHomeTileIds.Count >
                          ConstructionGreenPatchModel.Find(board,
                              lastEcologyDay.day).Sum(patch =>
                              patch.HedgehogCapacity) ||
                      lastEcologyDay.hedgehogMealHomeTileIds.Any(id =>
                          !lastEcologyDay.hedgehogHomeTileIds.Contains(id)))) ||
                 (!score.UsesLegacyAnimalScoring &&
                     lastEcologyDay.urbanMealTileIds == null) ||
                 (lastEcologyDay.urbanMealTileIds ?? new List<string>()).Any(id =>
                     !board.BuiltTiles.Any(tile => tile.id == id &&
                         (tile.category is ConstructionCategory.Supermarket or
                             ConstructionCategory.Waste or
                             ConstructionCategory.Square) &&
                         tile.builtDay <= lastEcologyDay.day)) ||
                 lastEcologyDay.squirrelsFed < 0 ||
                 lastEcologyDay.squirrelsFed >
                     (saved.greenPatchRulesVersion == 1
                         ? ConstructionGreenPatchModel.SquirrelCapacity(board,
                             lastEcologyDay.day)
                         : ConstructionFoodModel.SquirrelPopulation(board,
                             lastEcologyDay.day,
                             useLegacyEightNeighbours: true)) ||
                 lastEcologyDay.pigeonsFed < 0 ||
                 lastEcologyDay.pigeonsFed >
                     (saved.greenPatchRulesVersion == 1
                         ? ConstructionGreenPatchModel.PigeonCapacity(board,
                             lastEcologyDay.day)
                         : ConstructionFoodModel.PigeonPopulation(board,
                             lastEcologyDay.day, saved.pigeonTileId,
                             useLegacyEightNeighbours: true)) ||
                 lastEcologyDay.squirrelsPresent < 0 ||
                 lastEcologyDay.squirrelsPresent >
                     (saved.greenPatchRulesVersion == 1
                         ? ConstructionGreenPatchModel.SquirrelCapacity(board,
                             lastEcologyDay.day)
                         : ConstructionFoodModel.SquirrelPopulation(board,
                             lastEcologyDay.day,
                             useLegacyEightNeighbours: true)) ||
                 lastEcologyDay.pigeonsPresent < 0 ||
                 lastEcologyDay.pigeonsPresent >
                     (saved.greenPatchRulesVersion == 1
                         ? ConstructionGreenPatchModel.PigeonCapacity(board,
                             lastEcologyDay.day)
                         : ConstructionFoodModel.PigeonPopulation(board,
                             lastEcologyDay.day, saved.pigeonTileId,
                             useLegacyEightNeighbours: true)) ||
                 (score.ScoringVersion >= 8 &&
                     ((lastEcologyDay.pigeonsPresent > 0) !=
                          !string.IsNullOrEmpty(
                              lastEcologyDay.pigeonHabitatTileId) ||
                      (lastEcologyDay.hedgehogPresent) !=
                          !string.IsNullOrEmpty(
                              lastEcologyDay.hedgehogHabitatTileId) ||
                      !ValidSavedWildlifeHabitat(board,
                          lastEcologyDay.pigeonHabitatTileId,
                          GreenPlanting.Meadow, lastEcologyDay.day) ||
                      !ValidSavedWildlifeHabitat(board,
                          lastEcologyDay.hedgehogHabitatTileId,
                          GreenPlanting.Shrub, lastEcologyDay.day))) ||
                 !Enum.IsDefined(typeof(ConstructionPreySpecies),
                     lastEcologyDay.predatedSpecies) ||
                 !Enum.IsDefined(typeof(ConstructionTrafficSpecies),
                     lastEcologyDay.trafficSpecies) ||
                 (lastEcologyDay.trafficSpecies == ConstructionTrafficSpecies.None
                     ? !string.IsNullOrEmpty(lastEcologyDay.trafficStreetTileId) ||
                       !string.IsNullOrEmpty(lastEcologyDay.trafficWildlifeTileId)
                     : !board.BuiltTiles.Any(street =>
                           street.id == lastEcologyDay.trafficStreetTileId &&
                           street.category == ConstructionCategory.Street &&
                           street.builtDay <= lastEcologyDay.day &&
                           board.BuiltTiles.Any(wildlife =>
                               wildlife.id == lastEcologyDay.trafficWildlifeTileId &&
                               wildlife.category == ConstructionCategory.Green &&
                               wildlife.builtDay <= lastEcologyDay.day &&
                               ConstructionBoardModel.AreNeighbours(
                                   street.column, street.row,
                                   wildlife.column, wildlife.row)))) ||
                 (lastEcologyDay.predatedSpecies != ConstructionPreySpecies.None &&
                     (string.IsNullOrEmpty(lastEcologyDay.predationTileId) ||
                      lastEcologyDay.foxHuntRouteTileIds == null ||
                      lastEcologyDay.foxHuntRouteTileIds.Count == 0))))
                return false;
            var lastHumanDay = saved.lastHumanDay?.day > 0
                ? saved.lastHumanDay : null;
            if (lastHumanDay != null &&
                (score.Days.Count == 0 ||
                 lastHumanDay.day != score.Days.Last().day ||
                 lastHumanDay.CompletedWorkCycles !=
                     score.Days.Last().completedWorkCycles ||
                 score.HasBalanceScore && lastHumanDay.SatisfactionPoints !=
                     score.Days.Last().humanPoints ||
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
            // Older saves have no square fields and pass with their default
            // values. A new square meal must be backed by a single completed
            // resident visit on that same settled day.
            if (lastHumanDay != null &&
                (lastHumanDay.residents.Any(person =>
                     person.visitedSquare !=
                         !string.IsNullOrEmpty(person.squareTileId) ||
                     person.squareBonus != (person.visitedSquare ? 1 : 0) ||
                     person.visitedSquare &&
                         (!person.completedCycle ||
                          !board.BuiltTiles.Any(tile =>
                              tile.id == person.squareTileId &&
                              tile.category == ConstructionCategory.Square &&
                              tile.builtDay <= lastHumanDay.day) ||
                          board.ResidenceServiceRouteTo(person.homeTileId,
                              person.squareTileId, lastHumanDay.day,
                              useLegacyEightNeighbours: true).Count == 0)) ||
                 lastHumanDay.residents.Where(person => person.visitedSquare)
                     .Select(person => person.squareTileId).Distinct().Count() !=
                     lastHumanDay.SquareVisits))
                return false;
            if (lastEcologyDay != null &&
                (lastEcologyDay.urbanMealTileIds ?? new List<string>())
                    .Where(id => board.BuiltTiles.Any(tile => tile.id == id &&
                        tile.category == ConstructionCategory.Square))
                    .GroupBy(id => id).Any(group => group.Count() > 1 ||
                        lastHumanDay?.residents?.Any(person =>
                            person.visitedSquare &&
                            person.squareTileId == group.Key) != true))
                return false;
            if (lastHumanDay != null && score.Days.Count > 0)
            {
                var recorded = score.Days.Last();
                if (lastHumanDay.residents.Any(person =>
                        !Enum.IsDefined(typeof(ConstructionLinkageKind),
                            person.linkageKind) ||
                        person.linkageBonus != ConstructionLinkageScorer.PointsFor(
                            person.linkageKind) ||
                        !person.completedCycle && (person.linkageBonus != 0 ||
                            person.biodiversityBonus != 0) ||
                        (score.ScoringVersion < 8 &&
                            person.biodiversityBonus != 0) ||
                        person.biodiversityBonus != 0 &&
                            person.biodiversityBonus !=
                                ConstructionLinkageScorer.BiodiversityPointsPerHome ||
                        (score.ScoringVersion >= 8 &&
                            (person.linkageBonus > 0) !=
                                !string.IsNullOrEmpty(
                                    person.linkageSourceTileId))) ||
                    !recorded.linkageRulesApplied &&
                        lastHumanDay.residents.Any(person =>
                            person.linkageBonus != 0 ||
                            person.biodiversityBonus != 0) ||
                    recorded.linkageRulesApplied && lastEcologyDay == null)
                    return false;
                if (recorded.linkageRulesApplied)
                {
                    var baseline = new ConstructionHumanDayResult
                    {
                        day = lastHumanDay.day,
                        residents = lastHumanDay.residents.Select(person =>
                            new ConstructionResidentDayResult
                            {
                                homeTileId = person.homeTileId,
                                mealTileId = person.mealTileId,
                                squareTileId = person.squareTileId,
                                ate = person.ate,
                                completedCycle = person.completedCycle,
                                visitedSquare = person.visitedSquare,
                                satisfactionPoints = person.satisfactionPoints -
                                    person.linkageBonus -
                                    person.biodiversityBonus
                            }).ToList()
                    };
                    var expected = ConstructionLinkageScorer.Apply(board,
                        lastHumanDay.day, lastEcologyDay, baseline,
                        score.HasLocalFoodAreaScore,
                        score.ScoringVersion >= 8);
                    if (expected.sharedSquareMeals != recorded.sharedSquareMeals ||
                        expected.sharedSupermarketMeals !=
                            recorded.sharedSupermarketMeals ||
                        expected.sharedGreenMeals != recorded.sharedGreenMeals ||
                        expected.biodiversityHomes != recorded.biodiversityHomes ||
                        expected.LinkagePoints != recorded.linkagePoints ||
                        baseline.residents.Where((person, index) =>
                            person.linkageBonus !=
                                lastHumanDay.residents[index].linkageBonus ||
                            person.linkageKind !=
                                lastHumanDay.residents[index].linkageKind ||
                            person.biodiversityBonus !=
                                lastHumanDay.residents[index].biodiversityBonus ||
                            score.ScoringVersion >= 8 &&
                                (!string.Equals(
                                    person.linkageSourceTileId ?? string.Empty,
                                    lastHumanDay.residents[index]
                                        .linkageSourceTileId ?? string.Empty,
                                    StringComparison.Ordinal) ||
                                 !(person.biodiversityHabitatTileIds ??
                                       new List<string>()).SequenceEqual(
                                     lastHumanDay.residents[index]
                                         .biodiversityHabitatTileIds ??
                                     new List<string>()))).Any())
                        return false;
                }
            }
            var lastWasteDay = saved.lastWasteDay?.day > 0
                ? saved.lastWasteDay : null;
            if (lastWasteDay != null &&
                (score.Days.Count == 0 ||
                 lastWasteDay.day != score.Days.Last().day ||
                 lastWasteDay.changes == null ||
                 lastWasteDay.changes.Any(change => change == null ||
                     change.cleared < 0 || change.produced < 0 ||
                     change.foraged < 0 || change.foraged > change.produced ||
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
                     (saved.greenPatchRulesVersion == 1
                         ? ConstructionGreenPatchModel.SquirrelCapacity(board,
                               lastForecast.day) +
                           ConstructionGreenPatchModel.PigeonCapacity(board,
                               lastForecast.day)
                         : ConstructionFoodModel.SquirrelPopulation(board,
                               lastForecast.day,
                               useLegacyEightNeighbours: true) +
                           ConstructionFoodModel.PigeonPopulation(board,
                               lastForecast.day, saved.pigeonTileId,
                               useLegacyEightNeighbours: true)) +
                     (saved.greenPatchRulesVersion == 1
                         ? ConstructionGreenPatchModel.Find(board,
                             lastForecast.day).Sum(patch =>
                             patch.HedgehogCapacity) : 1) +
                     (score.UsesLegacyAnimalScoring &&
                      lastEcologyDay.predatedSpecies != ConstructionPreySpecies.None
                         ? 1 : 0) ||
                 lastForecast.completedWorkCycles < 0 ||
                 lastForecast.completedWorkCycles >
                     board.BuiltTiles.Count(tile =>
                         tile.category == ConstructionCategory.Residence &&
                         tile.builtDay <= lastForecast.day) ||
                 score.HasBalanceScore &&
                     lastForecast.humanPoints != lastHumanDay.SatisfactionPoints ||
                 lastForecast.wasteCleared < 0 ||
                 lastForecast.wasteCleared != lastWasteDay.Cleared ||
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

            var savedDispatches = saved.residentDispatches ??
                new List<ConstructionResidentDispatch>();
            var dispatchHomes = new HashSet<string>();
            foreach (var plan in savedDispatches)
            {
                if (plan == null || string.IsNullOrEmpty(plan.homeTileId) ||
                    !dispatchHomes.Add(plan.homeTileId) ||
                    (string.IsNullOrEmpty(plan.workTileId) &&
                     string.IsNullOrEmpty(plan.mealTileId)) ||
                    !board.BuiltTiles.Any(tile => tile.id == plan.homeTileId &&
                        tile.category == ConstructionCategory.Residence) ||
                    !ValidSavedDispatchTarget(board, saved.currentDay,
                        plan.homeTileId, plan.workTileId, false) ||
                    !ValidSavedDispatchTarget(board, saved.currentDay,
                        plan.homeTileId, plan.mealTileId, true))
                    return false;
            }

            // Active saves keep their original scoring version. V5 introduced
            // collision penalties and V6 changes green yield; either change
            // would make a resumed run incomparable with a fresh one.
            if (!saved.finished && score.ScoringVersion == 3)
                score = score.UpgradeVersionThreeToFour();
            run = new ConstructionRunModel
            {
                RunId = string.IsNullOrEmpty(saved.runId)
                    ? Guid.NewGuid().ToString("D") : saved.runId,
                awardRulesVersion = saved.awardRulesVersion == 0 ? 1 : saved.awardRulesVersion,
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
            run.residentDispatches = savedDispatches.Select(CopyDispatch)
                .ToList();
            run.Mode = saved.mode;
            run.introRulesVersion = saved.introRulesVersion;
            run.usesFillHand = saved.usesFillHand;
            // Version 2 kept green, street and waste outside the hand. Return
            // that unused supply to the common deck without resetting the
            // run, held buildings, random state or earned/spent swaps.
            run.fillHandDeckVersion = saved.fillHandDeckVersion == 2
                ? 3 : saved.fillHandDeckVersion;
            run.greenPatchRulesVersion = saved.greenPatchRulesVersion;
            run.endlessUpgradeRulesVersion = saved.endlessUpgradeRulesVersion;
            run.fillHandRandomState = saved.fillHandRandomState;
            run.fillHandCards = saved.fillHandCards?.ToList() ??
                new List<ConstructionCategory>();
            run.fillHandWitnessedLinkage = saved.fillHandWitnessedLinkage;
            run.fillHandExpanded = saved.fillHandExpanded;
            run.fillHandExpandedToFive = saved.fillHandExpandedToFive;
            run.fillHandFedSpeciesMask = saved.fillHandFedSpeciesMask;
            run.fillHandRerollsSpent = saved.fillHandRerollsSpent;
            run.AnimalDeathLimit = saved.endlessLossRulesVersion == 1
                ? saved.animalDeathLimit : RecommendedAnimalDeathLimit;
            run.UnemployedResidentLimit = saved.endlessLossRulesVersion == 1
                ? saved.unemployedResidentLimit : RecommendedUnemployedResidentLimit;
            run.EndlessLossRulesConfirmed = saved.endlessLossRulesVersion == 1 &&
                saved.endlessLossRulesConfirmed;
            run.TutorialTipsSkipped = saved.tutorialTipsSkipped;
            run.FillTutorialSkipped = saved.fillTutorialSkipped;
            run.FillTutorialSkipBuiltCount = saved.fillTutorialSkipBuiltCount;
            run.OpeningTourCompleted = saved.openingTourCompleted;
            run.EndReason = saved.endReason;
            // A restored run may be used for a build preview. Its timed
            // arrivals must never mutate the source run's survival state.
            run.survival = CopySurvival(saved.survival);
            if (saved.endlessLossRulesVersion == 0)
            {
                run.survival.animalDeathDays.Clear();
                run.survival.unemployedResidentsLastDay = 0;
                run.survival.unemployedDaysInRow = 0;
            }
            run.survival.trafficDeaths ??= new List<ConstructionTrafficDeathSaveData>();
            if (!run.MigrateWildlifeReturnState(
                    saved.wildlifeReturnRulesVersion))
            {
                run = null;
                return false;
            }
            run.RefreshWildlifeReturns();
            if (!run.TryRestoreSpecialTasks(saved.specialTasks))
            {
                run = null;
                return false;
            }
            if (!run.TryRestorePlacementScore(saved.placementScore))
            {
                run = null;
                return false;
            }
            if (run.UsesFillHand && !run.IsIntroPhase && !run.IsFinished)
            {
                // Earlier rules unlocked slots after two and four squares.
                // Keep those saves loadable, then match the current 3/6 rule.
                var squares = run.CategoryBuiltCount(ConstructionCategory.Square);
                run.fillHandExpanded = squares >= 3;
                run.fillHandExpandedToFive = squares >= 6;
                while (run.fillHandCards.Count > run.FillHandSize)
                    run.fillHandCards.RemoveAt(run.fillHandCards.Count - 1);
                while (run.fillHandCards.Count < run.FillHandSize &&
                       run.CanDrawFillHandCategory())
                    run.fillHandCards.Add(run.DrawFillHandCategory());
                run.EnsurePlayableRandomFillHand();
            }
            return true;
        }

        // Earlier patch saves used the flock's latest green food source as
        // this reference. It can have been planted after the first arrival.
        // Check arrival habitat at the historic day and the saved reference
        // at the current day, without inventing an earlier planting date.
        private static bool HasValidPatchPigeonHistory(ConstructionBoardModel board,
            int arrivalDay, int currentDay, string referenceId)
        {
            var reference = board.BuiltTiles.FirstOrDefault(tile => tile.id == referenceId);
            return reference != null && reference.category == ConstructionCategory.Green &&
                reference.builtDay <= currentDay &&
                ConstructionFoodModel.MaturityStage(reference, currentDay) == 2 &&
                ConstructionGreenPatchModel.Find(board, arrivalDay)
                    .Any(patch => patch.PigeonCapacity >= 2);
        }

        private bool MigrateWildlifeReturnState(int rulesVersion)
        {
            survival.squirrelReturnDays ??= new List<int>();
            survival.pigeonReturnDays ??= new List<int>();
            var squirrelDeaths = survival.predatedSquirrels +
                survival.trafficKilledSquirrels;
            var pigeonDeaths = survival.predatedPigeons +
                survival.trafficKilledPigeons;
            if (rulesVersion == 0)
            {
                // Old saves had permanent casualty counters, but no dates.
                // Preserve the visible population and start a fresh, explicit
                // wait rather than summoning historical victims on load.
                survival.squirrelReturnDays.Clear();
                survival.pigeonReturnDays.Clear();
                survival.returnedSquirrels = 0;
                survival.returnedPigeons = 0;
                survival.squirrelReturnCeiling = int.MaxValue;
                survival.pigeonReturnCeiling = int.MaxValue;
                survival.lastSquirrelReturnDay = 0;
                survival.lastPigeonReturnDay = 0;
                survival.squirrelExtinctDay = 0;
                survival.pigeonExtinctDay = 0;
                if (survival.hedgehogNextArrivalShrubs >
                    MatureConnectedShrubCount)
                {
                    survival.hedgehogAbsent = true;
                    survival.hedgehogReturnDay = CurrentDay + 3;
                    survival.hedgehogNextArrivalShrubs = 0;
                }
                else
                {
                    survival.hedgehogAbsent = false;
                    survival.hedgehogReturnDay = 0;
                }
            }
            if (survival.returnedSquirrels < 0 ||
                survival.returnedPigeons < 0 ||
                survival.lastSquirrelReturnDay < 0 ||
                survival.lastPigeonReturnDay < 0 ||
                survival.lastSquirrelReturnDay > CurrentDay ||
                survival.lastPigeonReturnDay > CurrentDay ||
                survival.squirrelExtinctDay < 0 ||
                survival.pigeonExtinctDay < 0 ||
                survival.squirrelExtinctDay > CurrentDay ||
                survival.pigeonExtinctDay > CurrentDay ||
                survival.hedgehogReturnDay < 0 ||
                (survival.hedgehogAbsent &&
                    survival.hedgehogReturnDay == 0) ||
                survival.squirrelReturnDays.Any(day => day < 3 ||
                    day > CurrentDay + 3) ||
                survival.pigeonReturnDays.Any(day => day < 3 ||
                    day > CurrentDay + 3) ||
                !survival.squirrelReturnDays.SequenceEqual(
                    survival.squirrelReturnDays.OrderBy(day => day)) ||
                !survival.pigeonReturnDays.SequenceEqual(
                    survival.pigeonReturnDays.OrderBy(day => day)))
                return false;
            if (Mode != ConstructionGameMode.Endless)
            {
                var missingSquirrels = squirrelDeaths -
                    survival.returnedSquirrels -
                    survival.squirrelReturnDays.Count;
                var missingPigeons = pigeonDeaths -
                    survival.returnedPigeons -
                    survival.pigeonReturnDays.Count;
                if (missingSquirrels < 0 || missingPigeons < 0) return false;
                survival.squirrelReturnDays.AddRange(Enumerable.Repeat(
                    CurrentDay + 3, missingSquirrels));
                survival.pigeonReturnDays.AddRange(Enumerable.Repeat(
                    CurrentDay + 3, missingPigeons));
                var squirrelBase = Math.Max(0,
                    SquirrelHabitatCapacity(CurrentDay) -
                    squirrelDeaths + survival.returnedSquirrels);
                var pigeonBase = Math.Max(0,
                    PigeonHabitatCapacity(CurrentDay) - pigeonDeaths +
                    survival.returnedPigeons);
                survival.squirrelReturnCeiling =
                    survival.squirrelReturnDays.Count == 0 ? int.MaxValue :
                        Math.Min(survival.squirrelReturnCeiling < 0
                                ? squirrelBase : survival.squirrelReturnCeiling,
                            squirrelBase);
                survival.pigeonReturnCeiling =
                    survival.pigeonReturnDays.Count == 0 ? int.MaxValue :
                        Math.Min(survival.pigeonReturnCeiling < 0
                                ? pigeonBase : survival.pigeonReturnCeiling,
                            pigeonBase);
            }
            else
            {
                if (survival.squirrels == 0 &&
                    survival.squirrelExtinctDay == 0 && !IsFinished)
                    survival.squirrelExtinctDay = CurrentDay;
                if (PigeonArrivalDay > 0 && survival.pigeons == 0 &&
                    survival.pigeonExtinctDay == 0 && !IsFinished)
                    survival.pigeonExtinctDay = CurrentDay;
            }
            return true;
        }

        private static ConstructionSurvivalSaveData CopySurvival(
            ConstructionSurvivalSaveData source)
        {
            if (source == null) return new ConstructionSurvivalSaveData();
            return new ConstructionSurvivalSaveData
            {
                squirrels = source.squirrels,
                pigeons = source.pigeons,
                predatedSquirrels = source.predatedSquirrels,
                predatedPigeons = source.predatedPigeons,
                trafficKilledSquirrels = source.trafficKilledSquirrels,
                trafficKilledPigeons = source.trafficKilledPigeons,
                trafficDeaths = source.trafficDeaths?.Where(item => item != null)
                    .Select(item =>
                    new ConstructionTrafficDeathSaveData
                    {
                        streetTileId = item.streetTileId,
                        count = item.count
                    }).ToList() ?? new List<ConstructionTrafficDeathSaveData>(),
                hedgehogNextArrivalShrubs = source.hedgehogNextArrivalShrubs,
                foxEstablished = source.foxEstablished,
                foxHungryDays = source.foxHungryDays,
                foxReturnDay = source.foxReturnDay,
                squirrelHungryDays = source.squirrelHungryDays,
                pigeonHungryDays = source.pigeonHungryDays,
                squirrelFedDays = source.squirrelFedDays,
                pigeonFedDays = source.pigeonFedDays,
                squirrelReturnDays = source.squirrelReturnDays?.ToList() ??
                    new List<int>(),
                pigeonReturnDays = source.pigeonReturnDays?.ToList() ??
                    new List<int>(),
                returnedSquirrels = source.returnedSquirrels,
                returnedPigeons = source.returnedPigeons,
                squirrelReturnCeiling = source.squirrelReturnCeiling,
                pigeonReturnCeiling = source.pigeonReturnCeiling,
                lastSquirrelReturnDay = source.lastSquirrelReturnDay,
                lastPigeonReturnDay = source.lastPigeonReturnDay,
                squirrelExtinctDay = source.squirrelExtinctDay,
                pigeonExtinctDay = source.pigeonExtinctDay,
                hedgehogAbsent = source.hedgehogAbsent,
                hedgehogReturnDay = source.hedgehogReturnDay,
                hedgehogPatches = source.hedgehogPatches?
                    .Where(state => state != null).Select(state =>
                        new ConstructionHedgehogPatchState
                        {
                            homeTileId = state.homeTileId,
                            hungryDays = state.hungryDays,
                            returnDay = state.returnDay
                        }).ToList() ?? new List<ConstructionHedgehogPatchState>(),
                animalDeathDays = source.animalDeathDays?.ToList() ??
                    new List<int>(),
                unemployedResidentsLastDay = source.unemployedResidentsLastDay,
                unemployedDaysInRow = source.unemployedDaysInRow,
                residents = source.residents?.Where(person => person != null)
                    .Select(person =>
                    new ConstructionResidentSurvivalData
                    {
                        homeTileId = person.homeTileId,
                        missedDays = person.missedDays,
                        departed = person.departed
                    }).ToList() ?? new List<ConstructionResidentSurvivalData>()
            };
        }

        private static bool ValidSavedWildlifeHabitat(
            ConstructionBoardModel board, string tileId,
            GreenPlanting planting, int day)
        {
            if (string.IsNullOrEmpty(tileId)) return true;
            return board.BuiltTiles.Any(tile => tile.id == tileId &&
                tile.category == ConstructionCategory.Green &&
                tile.planting == planting && tile.builtDay <= day);
        }

        private static bool ValidSavedDispatchTarget(
            ConstructionBoardModel board, int day, string homeTileId,
            string targetTileId, bool meal)
        {
            if (string.IsNullOrEmpty(targetTileId)) return true;
            return board.BuiltTiles.Any(tile => tile.id == targetTileId &&
                (meal ? ConstructionHumanModel.DailyMealCapacity(tile.category)
                    : ConstructionHumanModel.DailyJobCapacity(tile.category)) > 0 &&
                board.ResidenceServiceRouteTo(homeTileId, tile.id, day,
                    useLegacyEightNeighbours: true).Count > 0);
        }
    }
}
