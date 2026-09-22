using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Core
{
    public static class AnimalActivitySchedule
    {
        public static bool IsNormallyActive(WildlifeSpecies species, DayPhase phase)
        {
            return species switch
            {
                WildlifeSpecies.Pigeon => phase == DayPhase.Dawn || phase == DayPhase.Day || phase == DayPhase.Dusk,
                WildlifeSpecies.Squirrel => phase == DayPhase.Dawn || phase == DayPhase.Day || phase == DayPhase.Dusk,
                WildlifeSpecies.Hedgehog => phase == DayPhase.Dusk || phase == DayPhase.Night,
                WildlifeSpecies.Fox => phase == DayPhase.Dusk || phase == DayPhase.Night,
                _ => false
            };
        }
    }
}
