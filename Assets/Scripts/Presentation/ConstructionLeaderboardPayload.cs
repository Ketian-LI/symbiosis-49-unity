using System;
using UnityEngine;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    // A completed-run snapshot for the local leaderboard. The score version
    // prevents older scoring rules from being mixed into current rankings.
    [Serializable]
    public sealed class ConstructionLeaderboardPayload
    {
        public string runId;
        public string nickname;
        public string mode;
        public int scoringVersion;
        // Zero in records saved before the tabletop rules were versioned.
        // Random physical draws use version 2; older board-game runs use 1.
        public int boardGameRulesVersion;
        public int daysPlayed;
        public string endReason;
        public int animalPoints;
        public int residentPoints;
        public int totalPoints;
        // Zero denotes legacy shared-score records. One includes construction
        // and goal points; keeping both prevents unlike scores being ranked.
        public int scoreFormatVersion;
        public int buildPoints;
        public int goalPoints;
        public int animalMeals;
        public int humanWorkdays;
        public int linkagePoints;
        public int sharedSquareMeals;
        public int sharedSupermarketMeals;
        public int sharedGreenMeals;
        public int linkageTrackedDays;
        public int bothSidesScoredDays;

        public static bool TryCreate(ConstructionRunModel run, string name,
            out ConstructionLeaderboardPayload payload)
        {
            payload = null;
            var result = run?.FinalResult;
            if (result == null || !run.IsFinished || !run.HasLinkageScore ||
                result.LinkageTrackedDays != result.DaysPlayed ||
                !Guid.TryParse(run.RunId, out _) ||
                run.Mode == ConstructionGameMode.Endless &&
                    run.EndReason != ConstructionEndReason.AnimalDeathsLimit &&
                    run.EndReason != ConstructionEndReason.ResidentsUnemployed &&
                    run.EndReason != ConstructionEndReason.WildlifeGone &&
                    run.EndReason != ConstructionEndReason.ResidentsGone ||
                run.Mode != ConstructionGameMode.Endless &&
                    (result.DaysPlayed != 49 ||
                     run.EndReason != ConstructionEndReason.BoardComplete))
                return false;

            payload = new ConstructionLeaderboardPayload
            {
                runId = run.RunId,
                nickname = name,
                mode = run.Mode.ToString(),
                scoringVersion = run.ScoringVersion,
                boardGameRulesVersion = run.BoardGameRulesVersion,
                daysPlayed = result.DaysPlayed,
                endReason = run.EndReason.ToString(),
                animalPoints = result.AnimalPointsTotal,
                residentPoints = result.HumanPointsTotal,
                totalPoints = run.UsesOverallScore ? run.OverallScore :
                    result.BalancePointsTotal,
                scoreFormatVersion = run.UsesOverallScore ? 1 : 0,
                buildPoints = run.UsesPlacementScore ? run.PlacementScore : 0,
                goalPoints = run.UsesSpecialTasks ? run.SpecialTaskScore : 0,
                animalMeals = result.AnimalMealsTotal,
                humanWorkdays = result.HumanWorkdaysTotal,
                linkagePoints = result.LinkagePointsTotal,
                sharedSquareMeals = result.SharedSquareMealsTotal,
                sharedSupermarketMeals = result.SharedSupermarketMealsTotal,
                sharedGreenMeals = result.SharedGreenMealsTotal,
                linkageTrackedDays = result.LinkageTrackedDays,
                bothSidesScoredDays = result.BothSidesScoredDays
            };
            return true;
        }

        public string ToJson() => JsonUtility.ToJson(this);
    }

}
