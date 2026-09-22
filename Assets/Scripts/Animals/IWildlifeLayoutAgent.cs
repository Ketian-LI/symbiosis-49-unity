using UnityEngine;

namespace UrbanWildlifeRooms.Animals
{
    public enum WildlifeSpecies
    {
        Pigeon,
        Squirrel,
        Hedgehog,
        Fox
    }

    public interface IWildlifeLayoutAgent
    {
        WildlifeSpecies Species { get; }
        Transform AgentTransform { get; }
        void RelocateTo(Vector3 worldPosition, float durationSeconds);
    }
}
