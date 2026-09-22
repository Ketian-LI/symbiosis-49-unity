using System;

namespace UrbanWildlifeRooms.Core
{
    public enum GarageCrossingDecision
    {
        NoGarage,
        Detour,
        Abandon,
        CrossSafely,
        CrossFatally
    }

    public static class GarageTrafficModel
    {
        public const float DetourProbability = 0.5f;
        public const float FatalJudgementProbability = 0.5f;

        public static GarageCrossingDecision Decide(
            bool routeContainsGarage,
            bool detourExists,
            float routeChoiceRoll,
            float safetyRoll)
        {
            if (!routeContainsGarage)
            {
                return GarageCrossingDecision.NoGarage;
            }
            if (routeChoiceRoll < DetourProbability)
            {
                return detourExists
                    ? GarageCrossingDecision.Detour
                    : GarageCrossingDecision.Abandon;
            }
            return safetyRoll < FatalJudgementProbability
                ? GarageCrossingDecision.CrossFatally
                : GarageCrossingDecision.CrossSafely;
        }
    }
}
