namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>
    /// Shared world-scale contract. One Unity unit represents approximately one metre.
    /// Animal display lengths are deliberately exaggerated for legibility on
    /// the full 7x7 board. Navigation and click colliders use separate sizes.
    /// </summary>
    public static class WorldScaleStandards
    {
        public const float CellSizeMeters = 3.1f;

        public const float CitizenSourceHeight = 3.315f;
        public const float CitizenModelScale = 0.52f;
        public const float CitizenVisualHeight = CitizenSourceHeight * CitizenModelScale;

        public const float PigeonSourceLength = 2.86f;
        public const float PigeonTargetLength = 0.66f;
        public const float PigeonModelScale = PigeonTargetLength / PigeonSourceLength;
        public const float PigeonVisualLength = PigeonSourceLength * PigeonModelScale;

        public const float SquirrelSourceLength = 2.315599f;
        public const float SquirrelTargetLength = 0.60f;
        public const float SquirrelModelScale = SquirrelTargetLength / SquirrelSourceLength;
        public const float SquirrelVisualLength = SquirrelSourceLength * SquirrelModelScale;
        public const float HedgehogSourceLength = 1.487344f;
        public const float HedgehogTargetLength = 0.48f;
        public const float HedgehogModelScale = HedgehogTargetLength / HedgehogSourceLength;
        public const float HedgehogVisualLength = HedgehogSourceLength * HedgehogModelScale;
        public const float FoxSourceLength = 3.476015f;
        public const float FoxTargetLength = 1.20f;
        public const float FoxModelScale = FoxTargetLength / FoxSourceLength;
        public const float FoxVisualLength = FoxSourceLength * FoxModelScale;

        public static float FractionOfCell(float worldMeters)
        {
            return worldMeters / CellSizeMeters;
        }
    }
}
