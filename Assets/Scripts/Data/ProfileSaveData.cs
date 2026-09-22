using System;

namespace UrbanWildlifeRooms.Data
{
    [Serializable]
    public sealed class ProfileSaveData
    {
        public int schemaVersion = 1;
        public string savedAtUtc;
        public int bestSurvivalDays;

        public bool TryRecordSurvivalDays(int days)
        {
            if (days <= bestSurvivalDays)
            {
                return false;
            }

            bestSurvivalDays = days;
            return true;
        }
    }
}
