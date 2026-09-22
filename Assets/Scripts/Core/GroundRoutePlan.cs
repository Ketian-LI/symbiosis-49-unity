using System.Collections.Generic;

namespace UrbanWildlifeRooms.Core
{
    public sealed class GroundRoutePlan
    {
        public IReadOnlyList<string> Rooms { get; set; } = System.Array.Empty<string>();
        public GarageCrossingDecision Decision { get; set; }
        public int HazardConnectionIndex { get; set; } = -1;
        public bool Abandoned => Decision == GarageCrossingDecision.Abandon;
        public bool Fatal => Decision == GarageCrossingDecision.CrossFatally;
    }
}
