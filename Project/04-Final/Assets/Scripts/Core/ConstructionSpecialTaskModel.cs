using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public enum ConstructionSpecialTaskKind
    {
        CommuteCycles,
        RaiseAnimals,
        BuildSquares,
        AvoidStreets,
        ConnectGreens,
        CompleteCommunities
    }

    public enum ConstructionSpecialTaskDifficulty { Easy, Medium, Hard }

    [Serializable]
    public sealed class ConstructionSpecialTaskSaveData
    {
        public ConstructionSpecialTaskKind kind;
        public ConstructionSpecialTaskDifficulty difficulty;
        public int target;
        public int progress;
        public int points;
        public int effectPoints;
        // A manual replacement is only available during the opening choice.
        // Completed cards stay in their original slots.
        public bool replacementUsed;
        public bool completed;
    }

    [Serializable]
    public sealed class ConstructionSpecialTaskStateSaveData
    {
        public int version;
        public int randomState;
        public int bonusScore;
        public int effectScore;
        public int endgameScore;
        public bool endgameAwarded;
        public bool selectionConfirmed;
        public List<ConstructionSpecialTaskSaveData> activeTasks = new();
        public List<string> seenTaskKeys = new();
        public List<string> completedTaskKeys = new();
    }

    public sealed class ConstructionSpecialTaskStatus
    {
        public int Slot { get; internal set; }
        public string Key { get; internal set; }
        public ConstructionSpecialTaskKind Kind { get; internal set; }
        public ConstructionSpecialTaskDifficulty Difficulty { get; internal set; }
        public int Target { get; internal set; }
        public int Progress { get; internal set; }
        public int Points { get; internal set; }
        public int EffectPoints { get; internal set; }
        public int EffectLimit { get; internal set; }
        public bool ReplacementUsed { get; internal set; }
        public bool Completed { get; internal set; }
        public bool CanReplace { get; internal set; }
    }

    public sealed partial class ConstructionRunModel
    {
        private readonly struct SpecialTaskDefinition
        {
            public SpecialTaskDefinition(ConstructionSpecialTaskKind kind,
                ConstructionSpecialTaskDifficulty difficulty, int target,
                int points)
            {
                Kind = kind;
                Difficulty = difficulty;
                Target = target;
                Points = points;
            }

            public ConstructionSpecialTaskKind Kind { get; }
            public ConstructionSpecialTaskDifficulty Difficulty { get; }
            public int Target { get; }
            public int Points { get; }
            public string Key => TaskKey(Kind, Difficulty);
        }

        private const int SpecialTaskRulesVersion = 6;
        private static readonly SpecialTaskDefinition[] LegacySpecialTaskDefinitions =
        {
            new(ConstructionSpecialTaskKind.CommuteCycles,
                ConstructionSpecialTaskDifficulty.Easy, 3, 5),
            new(ConstructionSpecialTaskKind.CommuteCycles,
                ConstructionSpecialTaskDifficulty.Medium, 10, 12),
            new(ConstructionSpecialTaskKind.CommuteCycles,
                ConstructionSpecialTaskDifficulty.Hard, 20, 22),
            new(ConstructionSpecialTaskKind.RaiseAnimals,
                ConstructionSpecialTaskDifficulty.Easy, 2, 6),
            new(ConstructionSpecialTaskKind.RaiseAnimals,
                ConstructionSpecialTaskDifficulty.Medium, 4, 14),
            new(ConstructionSpecialTaskKind.RaiseAnimals,
                ConstructionSpecialTaskDifficulty.Hard, 6, 24),
            new(ConstructionSpecialTaskKind.BuildSquares,
                ConstructionSpecialTaskDifficulty.Easy, 1, 5),
            new(ConstructionSpecialTaskKind.BuildSquares,
                ConstructionSpecialTaskDifficulty.Medium, 2, 13),
            new(ConstructionSpecialTaskKind.BuildSquares,
                ConstructionSpecialTaskDifficulty.Hard, 4, 23),
            new(ConstructionSpecialTaskKind.AvoidStreets,
                ConstructionSpecialTaskDifficulty.Easy, 3, 6),
            new(ConstructionSpecialTaskKind.AvoidStreets,
                ConstructionSpecialTaskDifficulty.Medium, 5, 15),
            new(ConstructionSpecialTaskKind.AvoidStreets,
                ConstructionSpecialTaskDifficulty.Hard, 8, 26),
            new(ConstructionSpecialTaskKind.ConnectGreens,
                ConstructionSpecialTaskDifficulty.Easy, 3, 6),
            new(ConstructionSpecialTaskKind.ConnectGreens,
                ConstructionSpecialTaskDifficulty.Medium, 5, 14),
            new(ConstructionSpecialTaskKind.ConnectGreens,
                ConstructionSpecialTaskDifficulty.Hard, 8, 24),
            new(ConstructionSpecialTaskKind.CompleteCommunities,
                ConstructionSpecialTaskDifficulty.Easy, 1, 8),
            new(ConstructionSpecialTaskKind.CompleteCommunities,
                ConstructionSpecialTaskDifficulty.Medium, 2, 18),
            new(ConstructionSpecialTaskKind.CompleteCommunities,
                ConstructionSpecialTaskDifficulty.Hard, 3, 30)
        };
        // New cards count distinct living species. Earlier cards counted
        // individual animals and must retain their saved targets and points.
        private static readonly SpecialTaskDefinition[] SpecialTaskDefinitions =
            LegacySpecialTaskDefinitions.Where(definition =>
                definition.Kind != ConstructionSpecialTaskKind.RaiseAnimals &&
                !(definition.Kind == ConstructionSpecialTaskKind.CommuteCycles &&
                  definition.Difficulty == ConstructionSpecialTaskDifficulty.Easy))
            .Concat(new[]
            {
                new SpecialTaskDefinition(ConstructionSpecialTaskKind.CommuteCycles,
                    ConstructionSpecialTaskDifficulty.Easy, 6, 8),
                new SpecialTaskDefinition(ConstructionSpecialTaskKind.RaiseAnimals,
                    ConstructionSpecialTaskDifficulty.Medium, 3, 14),
                new SpecialTaskDefinition(ConstructionSpecialTaskKind.RaiseAnimals,
                    ConstructionSpecialTaskDifficulty.Hard, 4, 24)
            }).OrderBy(definition => definition.Kind)
              .ThenBy(definition => definition.Difficulty).ToArray();
        private static readonly SpecialTaskDefinition[] BalancedTaskDefinitions =
            SpecialTaskDefinitions.Select(definition =>
                new SpecialTaskDefinition(definition.Kind,
                    definition.Difficulty, definition.Target,
                    BalancedTaskPoints(definition.Kind,
                        definition.Difficulty))).ToArray();

        private static int BalancedTaskPoints(
            ConstructionSpecialTaskKind kind,
            ConstructionSpecialTaskDifficulty difficulty)
        {
            var index = (int)difficulty;
            return kind switch
            {
                ConstructionSpecialTaskKind.CommuteCycles =>
                    new[] { 25, 45, 70 }[index],
                ConstructionSpecialTaskKind.RaiseAnimals =>
                    new[] { 0, 50, 80 }[index],
                ConstructionSpecialTaskKind.BuildSquares =>
                    new[] { 20, 35, 55 }[index],
                ConstructionSpecialTaskKind.AvoidStreets =>
                    new[] { 15, 25, 40 }[index],
                ConstructionSpecialTaskKind.ConnectGreens =>
                    new[] { 25, 45, 70 }[index],
                ConstructionSpecialTaskKind.CompleteCommunities =>
                    new[] { 50, 80, 110 }[index],
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        public static int BalancedTaskEffectLimit(
            ConstructionSpecialTaskDifficulty difficulty) =>
            difficulty switch
            {
                ConstructionSpecialTaskDifficulty.Easy => 20,
                ConstructionSpecialTaskDifficulty.Medium => 30,
                ConstructionSpecialTaskDifficulty.Hard => 40,
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
            };

        private SpecialTaskDefinition[] ActiveSpecialTaskDefinitions =>
            specialTasks?.version >= 5 ? BalancedTaskDefinitions :
                specialTasks?.version >= 4 ? SpecialTaskDefinitions :
                    LegacySpecialTaskDefinitions;

        private ConstructionSpecialTaskStateSaveData specialTasks;

        private int TutorialTaskBuildLimit => FillTutorialSkipped
            ? FillTutorialSkipBuiltCount : ConstructionTutorialGuide.RoomCount;

        public bool UsesSpecialTasks => UsesFillHand &&
            specialTasks?.version is >= 3 and <= SpecialTaskRulesVersion;
        public bool UsesSpeciesTaskRules => UsesSpecialTasks &&
            specialTasks.version >= 4;
        public bool UsesPostTutorialTaskRules => UsesSpecialTasks &&
            specialTasks.version >= 6 && introRulesVersion == 1;
        // The eighth fixed card is still a lesson even though IsIntroPhase
        // turns false as soon as it is placed, before its settlement.
        private bool IsTutorialTaskEvent => UsesPostTutorialTaskRules &&
            board.BuiltCount <= TutorialTaskBuildLimit;
        private int PostTutorialCompletedWorkCycles => score.Days
            .Where(day => day.day > TutorialTaskBuildLimit)
            .Sum(day => day.completedWorkCycles);
        public int SpecialTaskScore => UsesSpecialTasks
            ? specialTasks.bonusScore + specialTasks.effectScore +
              specialTasks.endgameScore : 0;
        public int SpecialTaskEffectScore => UsesSpecialTasks
            ? specialTasks.effectScore : 0;
        public int SpecialTaskEndgameScore => UsesSpecialTasks
            ? specialTasks.endgameScore : 0;
        public int OverallScore => (int)Math.Max(int.MinValue,
            Math.Min(int.MaxValue,
                (long)BalanceScore + PlacementScore + SpecialTaskScore));
        public int CompletedSpecialTaskCount => UsesSpecialTasks
            ? specialTasks.completedTaskKeys.Count : 0;
        public bool IsSpecialTaskSelectionConfirmed => UsesSpecialTasks &&
            specialTasks.selectionConfirmed;
        public static int SpecialTaskDifficultyDrawWeight(
            ConstructionSpecialTaskDifficulty difficulty) => difficulty switch
        {
            ConstructionSpecialTaskDifficulty.Easy => 10,
            ConstructionSpecialTaskDifficulty.Medium => 7,
            ConstructionSpecialTaskDifficulty.Hard => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };
        public IReadOnlyList<ConstructionSpecialTaskStatus> SpecialTasks =>
            !UsesSpecialTasks
                ? Array.Empty<ConstructionSpecialTaskStatus>()
                : specialTasks.activeTasks.Select((task, slot) =>
                    new ConstructionSpecialTaskStatus
                    {
                        Slot = slot,
                        Key = TaskKey(task.kind, task.difficulty),
                        Kind = task.kind,
                        Difficulty = task.difficulty,
                        Target = task.target,
                        Progress = task.progress,
                        Points = task.points,
                        EffectPoints = task.effectPoints,
                        EffectLimit = specialTasks.version >= 5
                            ? BalancedTaskEffectLimit(task.difficulty) : 0,
                        ReplacementUsed = task.replacementUsed,
                        Completed = task.completed,
                        CanReplace = !specialTasks.selectionConfirmed &&
                            !task.completed && !task.replacementUsed &&
                            HasReplacementCandidate()
                    }).ToArray();

        public bool TryReplaceSpecialTask(int slot)
        {
            if (!UsesSpecialTasks || specialTasks.selectionConfirmed ||
                IsFinished || slot < 0 ||
                slot >= specialTasks.activeTasks.Count)
                return false;
            var current = specialTasks.activeTasks[slot];
            if (current.completed || current.replacementUsed) return false;
            var replacement = DrawSpecialTask(replacementUsed: true);
            if (replacement == null) return false;
            specialTasks.activeTasks[slot] = replacement;
            return true;
        }

        public bool TryConfirmSpecialTasks()
        {
            if (!UsesSpecialTasks || specialTasks.selectionConfirmed ||
                specialTasks.activeTasks.Count != 3 || IsFinished)
                return false;
            specialTasks.selectionConfirmed = true;
            return true;
        }

        private void InitializeSpecialTasks(bool selectionConfirmed = false)
        {
            if (!UsesFillHand)
            {
                specialTasks = null;
                return;
            }
            specialTasks = new ConstructionSpecialTaskStateSaveData
            {
                version = score?.ScoringVersion >= 12
                    ? SpecialTaskRulesVersion : 4,
                randomState = SpecialTaskSeed(),
                selectionConfirmed = selectionConfirmed
            };
            while (specialTasks.activeTasks.Count < 3)
            {
                var task = DrawSpecialTask(replacementUsed: false);
                if (task == null) break;
                specialTasks.activeTasks.Add(task);
            }
        }

        private ConstructionSpecialTaskStateSaveData ExportSpecialTasks()
        {
            if (!UsesSpecialTasks) return null;
            return new ConstructionSpecialTaskStateSaveData
            {
                version = specialTasks.version,
                randomState = specialTasks.randomState,
                bonusScore = specialTasks.bonusScore,
                effectScore = specialTasks.effectScore,
                endgameScore = specialTasks.endgameScore,
                endgameAwarded = specialTasks.endgameAwarded,
                selectionConfirmed = specialTasks.selectionConfirmed,
                activeTasks = specialTasks.activeTasks.Select(CopyTask).ToList(),
                seenTaskKeys = specialTasks.seenTaskKeys.ToList(),
                completedTaskKeys = specialTasks.completedTaskKeys.ToList()
            };
        }

        private bool TryRestoreSpecialTasks(
            ConstructionSpecialTaskStateSaveData saved)
        {
            if (!UsesFillHand)
            {
                specialTasks = null;
                return saved == null || saved.version == 0;
            }
            if (saved == null || saved.version == 0)
            {
                // Old hand-mode saves did not have tasks. Do not interrupt an
                // already-started run with a new opening modal; a genuinely
                // new run still requires the player to confirm its three cards.
                InitializeSpecialTasks(board.BuiltCount > 0 || IsFinished);
                return specialTasks?.activeTasks.Count == 3;
            }
            if (saved.version is < 1 or > SpecialTaskRulesVersion ||
                (score.ScoringVersion >= 12) != (saved.version >= 5) ||
                saved.randomState == 0 || saved.bonusScore < 0 ||
                saved.effectScore is < 0 or > 1000 ||
                saved.endgameScore is < 0 or > 240 ||
                (saved.version < 3 && (saved.effectScore != 0 ||
                    saved.endgameScore != 0 || saved.endgameAwarded)) ||
                (saved.version >= 3 &&
                    (saved.endgameAwarded != IsFinished ||
                     !saved.endgameAwarded && saved.endgameScore != 0)) ||
                saved.activeTasks == null || saved.activeTasks.Count != 3 ||
                saved.seenTaskKeys == null || saved.completedTaskKeys == null)
                return false;

            var definitions = (saved.version >= 5 ? BalancedTaskDefinitions :
                saved.version >= 4 ? SpecialTaskDefinitions :
                LegacySpecialTaskDefinitions).ToDictionary(item =>
                item.Key, StringComparer.Ordinal);
            var seen = new HashSet<string>(saved.seenTaskKeys,
                StringComparer.Ordinal);
            var completed = new HashSet<string>(saved.completedTaskKeys,
                StringComparer.Ordinal);
            if (seen.Count != saved.seenTaskKeys.Count ||
                completed.Count != saved.completedTaskKeys.Count ||
                seen.Any(key => !definitions.ContainsKey(key)) ||
                completed.Any(key => !seen.Contains(key)))
                return false;
            long expectedBonus = 0;
            foreach (var key in completed)
                expectedBonus += definitions[key].Points;
            if (expectedBonus != saved.bonusScore) return false;

            var activeKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var task in saved.activeTasks)
            {
                if (task == null ||
                    !Enum.IsDefined(typeof(ConstructionSpecialTaskKind), task.kind) ||
                    !Enum.IsDefined(typeof(ConstructionSpecialTaskDifficulty),
                        task.difficulty)) return false;
                var key = TaskKey(task.kind, task.difficulty);
                if (!activeKeys.Add(key) || !seen.Contains(key) ||
                    !definitions.TryGetValue(key, out var definition) ||
                    task.target != definition.Target ||
                    task.points != definition.Points ||
                    task.effectPoints < 0 ||
                    (saved.version < 5 && task.effectPoints != 0) ||
                    (saved.version >= 5 &&
                     (task.effectPoints >
                          BalancedTaskEffectLimit(task.difficulty) ||
                      !task.completed && task.effectPoints != 0)) ||
                    task.progress < 0 || task.progress > task.target ||
                    task.completed != (task.progress >= task.target) ||
                    task.completed != completed.Contains(key))
                    return false;
            }
            if (saved.version >= 5)
            {
                var endgame = saved.activeTasks.Where(task =>
                    task.kind is ConstructionSpecialTaskKind.CommuteCycles or
                        ConstructionSpecialTaskKind.CompleteCommunities)
                    .Sum(task => task.effectPoints);
                var ongoing = saved.activeTasks.Sum(task =>
                    task.effectPoints) - endgame;
                if (saved.effectScore != ongoing ||
                    saved.endgameScore != endgame ||
                    saved.activeTasks.Select(task => task.kind).Distinct()
                        .Count() != saved.activeTasks.Count)
                    return false;
            }

            var selectionConfirmed = saved.version == 1 ||
                saved.selectionConfirmed;
            if (!selectionConfirmed &&
                (saved.bonusScore != 0 || completed.Count != 0 ||
                 saved.activeTasks.Any(task => task.progress != 0)))
                return false;

            specialTasks = new ConstructionSpecialTaskStateSaveData
            {
                version = saved.version < 3 ? 3 : saved.version,
                randomState = saved.randomState,
                bonusScore = saved.bonusScore,
                effectScore = saved.effectScore,
                endgameScore = saved.endgameScore,
                endgameAwarded = saved.version < 3
                    ? IsFinished : saved.endgameAwarded,
                selectionConfirmed = selectionConfirmed,
                activeTasks = saved.activeTasks.Select(CopyTask).ToList(),
                seenTaskKeys = saved.seenTaskKeys.ToList(),
                completedTaskKeys = saved.completedTaskKeys.ToList()
            };
            // V5 saves made during the fixed tutorial may already contain
            // completed cards and points from those lessons. Rebase them
            // before the player begins free placement.
            if (saved.version == 5 && introRulesVersion == 1 &&
                board.BuiltCount <= ConstructionTutorialGuide.RoomCount &&
                !IsFinished)
            {
                specialTasks.version = 6;
                specialTasks.bonusScore = 0;
                specialTasks.effectScore = 0;
                specialTasks.endgameScore = 0;
                specialTasks.endgameAwarded = false;
                specialTasks.completedTaskKeys.Clear();
                foreach (var task in specialTasks.activeTasks)
                {
                    task.progress = 0;
                    task.effectPoints = 0;
                    task.completed = false;
                }
            }
            return true;
        }

        private void UpdateSpecialTasksAfterBuild(ConstructionCategory category)
        {
            if (!IsSpecialTaskSelectionConfirmed || IsTutorialTaskEvent) return;
            foreach (var task in specialTasks.activeTasks.Where(task =>
                         !task.completed))
            {
                if (task.kind == ConstructionSpecialTaskKind.AvoidStreets)
                    task.progress = category == ConstructionCategory.Street
                        ? 0 : Math.Min(task.target, task.progress + 1);
                else if (task.kind is ConstructionSpecialTaskKind.BuildSquares or
                         ConstructionSpecialTaskKind.ConnectGreens or
                         ConstructionSpecialTaskKind.RaiseAnimals)
                    task.progress = Math.Min(task.target,
                        CurrentSpecialTaskMetric(task.kind, LastHumanDay));
            }
            ResolveSpecialTaskCompletions();
        }

        private void UpdateSpecialTasksAfterSettlement(
            ConstructionHumanDayResult observedHuman)
        {
            if (!IsSpecialTaskSelectionConfirmed || IsTutorialTaskEvent) return;
            foreach (var task in specialTasks.activeTasks.Where(task =>
                         !task.completed && task.kind !=
                         ConstructionSpecialTaskKind.AvoidStreets))
                task.progress = Math.Min(task.target,
                    CurrentSpecialTaskMetric(task.kind, observedHuman));
            ResolveSpecialTaskCompletions();
        }

        private void ResolveSpecialTaskCompletions()
        {
            var completedTasks = specialTasks.activeTasks
                .Where(task => !task.completed &&
                    task.progress >= task.target)
                .ToArray();
            foreach (var task in completedTasks)
            {
                task.completed = true;
                var key = TaskKey(task.kind, task.difficulty);
                if (!specialTasks.completedTaskKeys.Contains(key))
                {
                    specialTasks.completedTaskKeys.Add(key);
                    specialTasks.bonusScore += task.points;
                }
            }
        }

        // Completed opening cards stay in their slots. Their effects begin
        // with the next matching event, so the event that unlocked a card is
        // never rewarded twice.
        private void AwardSpecialTaskBuildEffect(ConstructionTileData placed)
        {
            if (!IsSpecialTaskSelectionConfirmed || IsTutorialTaskEvent ||
                placed == null) return;
            var roadless = CompletedSpecialTaskStrength(
                ConstructionSpecialTaskKind.AvoidStreets);
            if (roadless > 0 && placed.category != ConstructionCategory.Street)
                GrantSpecialTaskEffect(
                    ConstructionSpecialTaskKind.AvoidStreets, 1 + roadless);

            var green = CompletedSpecialTaskStrength(
                ConstructionSpecialTaskKind.ConnectGreens);
            if (green > 0 && placed.category == ConstructionCategory.Green &&
                board.BuiltTiles.Any(tile => tile.id != placed.id &&
                    tile.category == ConstructionCategory.Green &&
                    ConstructionBoardModel.AreCardinalNeighbours(
                        tile.column, tile.row, placed.column, placed.row)))
                GrantSpecialTaskEffect(
                    ConstructionSpecialTaskKind.ConnectGreens, 1 + green);
        }

        private void AwardSpecialTaskDayEffect(int animalMeals,
            ConstructionHumanDayResult human,
            ConstructionLinkageDayResult linkage)
        {
            if (!IsSpecialTaskSelectionConfirmed || IsTutorialTaskEvent ||
                human == null) return;
            GrantSpecialTaskEffect(ConstructionSpecialTaskKind.RaiseAnimals,
                Math.Min(2, animalMeals) * CompletedSpecialTaskStrength(
                    ConstructionSpecialTaskKind.RaiseAnimals));
            GrantSpecialTaskEffect(ConstructionSpecialTaskKind.BuildSquares,
                Math.Min(2, linkage?.sharedSquareMeals ?? 0) *
                CompletedSpecialTaskStrength(
                    ConstructionSpecialTaskKind.BuildSquares));
        }

        private void AwardSpecialTaskEndgameEffect()
        {
            if (!IsSpecialTaskSelectionConfirmed || !IsFinished ||
                specialTasks.endgameAwarded) return;
            var commute = CompletedSpecialTaskStrength(
                ConstructionSpecialTaskKind.CommuteCycles);
            if (commute > 0)
                GrantSpecialTaskEffect(
                    ConstructionSpecialTaskKind.CommuteCycles,
                    Math.Min(30, UsesPostTutorialTaskRules
                        ? PostTutorialCompletedWorkCycles
                        : CompletedWorkCyclesTotal) * (1 + commute),
                    endgame: true);
            var communities = CompletedSpecialTaskStrength(
                ConstructionSpecialTaskKind.CompleteCommunities);
            if (communities > 0)
                GrantSpecialTaskEffect(
                    ConstructionSpecialTaskKind.CompleteCommunities,
                    40 * communities, endgame: true);
            specialTasks.endgameAwarded = true;
        }

        private void GrantSpecialTaskEffect(
            ConstructionSpecialTaskKind kind, int requested,
            bool endgame = false)
        {
            if (requested <= 0) return;
            if (specialTasks.version < 5)
            {
                if (endgame) specialTasks.endgameScore += requested;
                else specialTasks.effectScore += requested;
                return;
            }
            var task = specialTasks.activeTasks.FirstOrDefault(item =>
                item.kind == kind && item.completed);
            if (task == null) return;
            var remaining = BalancedTaskEffectLimit(task.difficulty) -
                task.effectPoints;
            var granted = Math.Min(requested, Math.Max(0, remaining));
            task.effectPoints += granted;
            if (endgame) specialTasks.endgameScore += granted;
            else specialTasks.effectScore += granted;
        }

        private int CompletedSpecialTaskStrength(
            ConstructionSpecialTaskKind kind)
        {
            if (!IsSpecialTaskSelectionConfirmed) return 0;
            for (var strength = 3; strength >= 1; strength--)
                if (specialTasks.completedTaskKeys.Contains(TaskKey(kind,
                        (ConstructionSpecialTaskDifficulty)(strength - 1))))
                    return strength;
            return 0;
        }

        private ConstructionSpecialTaskSaveData DrawSpecialTask(bool replacementUsed)
        {
            var seen = new HashSet<string>(specialTasks.seenTaskKeys,
                StringComparer.Ordinal);
            // The art and title are shared by all difficulties of a kind.
            // Replacing a card must change the kind the player sees, not only
            // its target number. Three opening cards and three swaps fit the
            // six kinds exactly, so previously seen kinds can stay retired.
            var seenKinds = ActiveSpecialTaskDefinitions
                .Where(definition => seen.Contains(definition.Key))
                .Select(definition => definition.Kind).ToHashSet();
            var candidates = ActiveSpecialTaskDefinitions.Where(definition =>
                    !seenKinds.Contains(definition.Kind) &&
                    !IsSpecialTaskAlreadySatisfied(definition))
                .ToArray();
            if (candidates.Length == 0) return null;
            var totalWeight = candidates.Sum(candidate =>
                SpecialTaskDifficultyDrawWeight(candidate.Difficulty));
            var weightedRoll = (int)(NextSpecialTaskRandom() %
                (uint)totalWeight);
            var definition = candidates[0];
            foreach (var candidate in candidates)
            {
                var weight = SpecialTaskDifficultyDrawWeight(
                    candidate.Difficulty);
                if (weightedRoll < weight)
                {
                    definition = candidate;
                    break;
                }
                weightedRoll -= weight;
            }
            specialTasks.seenTaskKeys.Add(definition.Key);
            return new ConstructionSpecialTaskSaveData
            {
                kind = definition.Kind,
                difficulty = definition.Difficulty,
                target = definition.Target,
                progress = definition.Kind == ConstructionSpecialTaskKind.AvoidStreets
                    ? 0 : Math.Min(definition.Target - 1,
                        CurrentSpecialTaskMetric(definition.Kind, LastHumanDay)),
                points = definition.Points,
                replacementUsed = replacementUsed
            };
        }

        private bool HasReplacementCandidate()
        {
            var seen = new HashSet<string>(specialTasks.seenTaskKeys,
                StringComparer.Ordinal);
            var seenKinds = ActiveSpecialTaskDefinitions
                .Where(definition => seen.Contains(definition.Key))
                .Select(definition => definition.Kind).ToHashSet();
            return ActiveSpecialTaskDefinitions.Any(definition =>
                !seenKinds.Contains(definition.Kind) &&
                !IsSpecialTaskAlreadySatisfied(definition));
        }

        private bool IsSpecialTaskAlreadySatisfied(
            SpecialTaskDefinition definition) =>
            definition.Kind != ConstructionSpecialTaskKind.AvoidStreets &&
            CurrentSpecialTaskMetric(definition.Kind, LastHumanDay) >=
            definition.Target;

        private int CurrentSpecialTaskMetric(ConstructionSpecialTaskKind kind,
            ConstructionHumanDayResult human)
        {
            if (IsTutorialTaskEvent) return 0;
            return kind switch
            {
                ConstructionSpecialTaskKind.CommuteCycles =>
                    UsesPostTutorialTaskRules
                        ? PostTutorialCompletedWorkCycles
                        : CompletedWorkCyclesTotal,
                // Existing species can count once observed in free play;
                // the tutorial itself cannot advance this card.
                ConstructionSpecialTaskKind.RaiseAnimals =>
                    UsesPostTutorialTaskRules && score.Days.Count <=
                        TutorialTaskBuildLimit ? 0 :
                    specialTasks?.version >= 4
                    ? (SquirrelPopulation > 0 ? 1 : 0) +
                      (PigeonPopulation > 0 ? 1 : 0) +
                      (HedgehogHabitat != null ? 1 : 0) +
                      (FoxHabitat != null ? 1 : 0)
                    : SquirrelPopulation + PigeonPopulation +
                    (HedgehogHabitat != null ? 1 : 0) +
                    (FoxHabitat != null ? 1 : 0),
                ConstructionSpecialTaskKind.BuildSquares =>
                    board.BuiltTiles.Count(tile => tile.category ==
                        ConstructionCategory.Square &&
                        (!UsesPostTutorialTaskRules || tile.buildIndex >
                            TutorialTaskBuildLimit)),
                ConstructionSpecialTaskKind.AvoidStreets => 0,
                ConstructionSpecialTaskKind.ConnectGreens =>
                    UsesPostTutorialTaskRules
                        ? LargestPostTutorialConnectedGreenCount()
                        : LargestConnectedGreenCount(),
                ConstructionSpecialTaskKind.CompleteCommunities =>
                    UsesPostTutorialTaskRules && score.Days.Count <=
                        TutorialTaskBuildLimit ? 0 :
                    CountCompleteCommunities(human),
                _ => 0
            };
        }

        private int LargestConnectedGreenCount() => board.BuiltTiles
            .Where(tile => tile.category == ConstructionCategory.Green)
            .Select(tile => board.ConnectedGreenCount(tile.id))
            .DefaultIfEmpty(0).Max();

        // Tutorial greens may connect later plots, but only greens placed
        // during free play contribute to the card's required number.
        private int LargestPostTutorialConnectedGreenCount() => board.BuiltTiles
            .Where(tile => tile.category == ConstructionCategory.Green &&
                tile.buildIndex > TutorialTaskBuildLimit)
            .Select(tile => board.ConnectedGreenTiles(tile.id).Count(connected =>
                connected.buildIndex > TutorialTaskBuildLimit))
            .DefaultIfEmpty(0).Max();

        // A community is anchored on one home. Its 3x3 neighbourhood may
        // contain repeated Green plots, but every other category is unique.
        // The resident's observed home-work-meal-home route must stay inside
        // the same nine cells, so planned access alone never completes it.
        private int CountCompleteCommunities(ConstructionHumanDayResult human)
        {
            if (human?.residents == null) return 0;
            var tiles = board.BuiltTiles.ToDictionary(tile => tile.id,
                StringComparer.Ordinal);
            var completed = 0;
            foreach (var resident in human.residents.Where(person =>
                         person.completedCycle))
            {
                if (!tiles.TryGetValue(resident.homeTileId, out var home) ||
                    !tiles.TryGetValue(resident.workTileId, out var work) ||
                    !tiles.TryGetValue(resident.mealTileId, out var meal) ||
                    !InsideCommunity(home, work) ||
                    !InsideCommunity(home, meal)) continue;
                var routeInside = resident.routeTileIds != null &&
                    resident.routeTileIds.All(id => tiles.TryGetValue(id,
                        out var routeTile) && InsideCommunity(home, routeTile));
                if (!routeInside) continue;
                var neighbourhood = board.BuiltTiles.Where(tile =>
                    InsideCommunity(home, tile)).ToArray();
                if (neighbourhood.Where(tile => tile.category !=
                        ConstructionCategory.Green)
                    .GroupBy(tile => tile.category)
                    .Any(group => group.Count() > 1)) continue;
                completed++;
            }
            return completed;
        }

        private static bool InsideCommunity(ConstructionTileData home,
            ConstructionTileData tile) =>
            Math.Abs(home.column - tile.column) <= 1 &&
            Math.Abs(home.row - tile.row) <= 1;

        private int SpecialTaskSeed()
        {
            uint seed = 2166136261;
            foreach (var character in RunId + ":special-tasks")
                seed = unchecked((seed ^ character) * 16777619);
            return unchecked((int)(seed == 0 ? 1 : seed));
        }

        private uint NextSpecialTaskRandom()
        {
            var value = unchecked((uint)specialTasks.randomState);
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            specialTasks.randomState = unchecked((int)value);
            return value;
        }

        private static string TaskKey(ConstructionSpecialTaskKind kind,
            ConstructionSpecialTaskDifficulty difficulty) =>
            $"{(int)kind}:{(int)difficulty}";

        private static ConstructionSpecialTaskSaveData CopyTask(
            ConstructionSpecialTaskSaveData source) => new()
        {
            kind = source.kind,
            difficulty = source.difficulty,
            target = source.target,
            progress = source.progress,
            points = source.points,
            effectPoints = source.effectPoints,
            replacementUsed = source.replacementUsed,
            completed = source.completed
        };
    }
}
