namespace UrbanWildlifeRooms.Presentation
{
    /// <summary>
    /// Shared world-scale contract. One Unity unit represents approximately one metre.
    /// Camera zoom may change for readability, but model scale must not.
    /// </summary>
    public static class WorldScaleStandards
    {
        public const float CellSizeMeters = 3.1f;

        public const float CitizenSourceHeight = 3.315f;
        public const float CitizenModelScale = 0.52f;
        public const float CitizenVisualHeight = CitizenSourceHeight * CitizenModelScale;

        public const float PigeonSourceLength = 2.86f;
        public const float PigeonModelScale = 0.15f;
        public const float PigeonVisualLength = PigeonSourceLength * PigeonModelScale;

        public const float SquirrelSourceLength = 2.315599f;
        public const float SquirrelTargetLength = 0.38f;
        public const float SquirrelModelScale = SquirrelTargetLength / SquirrelSourceLength;
        public const float SquirrelVisualLength = SquirrelSourceLength * SquirrelModelScale;
        public const float HedgehogSourceLength = 1.487344f;
        public const float HedgehogTargetLength = 0.25f;
        public const float HedgehogModelScale = HedgehogTargetLength / HedgehogSourceLength;
        public const float HedgehogVisualLength = HedgehogSourceLength * HedgehogModelScale;
        public const float FoxSourceLength = 3.476015f;
        public const float FoxTargetLength = 0.95f;
        public const float FoxModelScale = FoxTargetLength / FoxSourceLength;

        public static float FractionOfCell(float worldMeters)
        {
            return worldMeters / CellSizeMeters;
        }
    }
}
