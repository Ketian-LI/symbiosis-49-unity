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
        public const float BufferedFatalJudgementProbability = 0.25f;

        public static GarageCrossingDecision Decide(
            bool routeContainsGarage,
            bool detourExists,
            float routeChoiceRoll,
            float safetyRoll,
            bool garageBuffered = false)
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
            return safetyRoll < (garageBuffered
                    ? BufferedFatalJudgementProbability
                    : FatalJudgementProbability)
                ? GarageCrossingDecision.CrossFatally
                : GarageCrossingDecision.CrossSafely;
        }
    }
}
