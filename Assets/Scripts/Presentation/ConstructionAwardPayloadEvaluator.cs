using System;
using UrbanWildlifeRooms.Core;

namespace UrbanWildlifeRooms.Presentation
{
    // Adapts saved local leaderboard payloads to the pure award rules without
    // putting a Presentation dependency into the Core score model.
    public static class ConstructionAwardPayloadEvaluator
    {
        public static bool TryEvaluate(ConstructionLeaderboardPayload payload,
            out ConstructionAwardTier tier)
        {
            tier = default;
            if (payload == null ||
                !Enum.TryParse(payload.mode, false,
                    out ConstructionGameMode mode) ||
                !Enum.IsDefined(typeof(ConstructionGameMode), mode) ||
                !Enum.TryParse(payload.endReason, false,
                    out ConstructionEndReason endReason) ||
                !Enum.IsDefined(typeof(ConstructionEndReason), endReason))
                return false;

            return ConstructionAwardModel.TryEvaluate(mode,
                payload.scoringVersion, payload.daysPlayed, endReason,
                payload.totalPoints, payload.awardRulesVersion, out tier);
        }
    }
}
