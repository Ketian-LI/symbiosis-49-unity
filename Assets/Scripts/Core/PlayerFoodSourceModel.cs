using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    [Serializable]
    public sealed class PlayerFoodSourceState
    {
        public string id;
        public string roomId;
        public Vector3 worldPosition;
        public int portions = PlayerFoodSourceModel.PortionsPerSource;
        public float remainingLifetime = PlayerFoodSourceModel.LifetimeSeconds;
        public bool playerPlaced = true;
    }

    public sealed class PlayerFoodSourceModel
    {
        public const int PortionsPerSource = 5;
        public const int MaximumSources = 5;
        public const float LifetimeSeconds = 30f;

        private readonly Dictionary<string, PlayerFoodSourceState> sources = new();
        private int nextId = 1;

        public IReadOnlyDictionary<string, PlayerFoodSourceState> Sources => sources;
        public int PlayerPlacedCount => sources.Values.Count(source => source.playerPlaced);

        public bool TryCreate(string roomId, Vector3 worldPosition, out PlayerFoodSourceState source)
        {
            source = null;
            if (string.IsNullOrEmpty(roomId) || PlayerPlacedCount >= MaximumSources)
            {
                return false;
            }

            source = new PlayerFoodSourceState
            {
                id = $"food-{nextId++:000}",
                roomId = roomId,
                worldPosition = worldPosition,
                portions = PortionsPerSource,
                remainingLifetime = LifetimeSeconds,
                playerPlaced = true
            };
            sources[source.id] = source;
            return true;
        }

        public bool TryCreateExposed(
            string roomId,
            Vector3 worldPosition,
            int portions,
            out PlayerFoodSourceState source)
        {
            source = null;
            if (string.IsNullOrEmpty(roomId) || portions <= 0)
            {
                return false;
            }
            source = new PlayerFoodSourceState
            {
                id = $"cache-{nextId++:000}",
                roomId = roomId,
                worldPosition = worldPosition,
                portions = Mathf.Clamp(portions, 1, PortionsPerSource),
                remainingLifetime = SimulationClockModel.CycleSeconds > float.MaxValue
                    ? float.MaxValue
                    : (float)SimulationClockModel.CycleSeconds,
                playerPlaced = false
            };
            sources[source.id] = source;
            return true;
        }

        public bool TryClaimPortion(string sourceId)
        {
            if (!sources.TryGetValue(sourceId, out var source) || source.portions <= 0)
            {
                return false;
            }

            source.portions--;
            if (source.portions == 0)
            {
                sources.Remove(sourceId);
            }
            return true;
        }

        public IReadOnlyList<string> Advance(float simulationDeltaSeconds)
        {
            if (simulationDeltaSeconds <= 0f)
            {
                return Array.Empty<string>();
            }

            var expired = new List<string>();
            foreach (var source in sources.Values)
            {
                source.remainingLifetime -= simulationDeltaSeconds;
                if (source.remainingLifetime <= 0f)
                {
                    expired.Add(source.id);
                }
            }

            foreach (var id in expired)
            {
                sources.Remove(id);
            }
            return expired;
        }

        public List<PlayerFoodSourceSaveData> Export()
        {
            return sources.Values
                .OrderBy(source => source.id, StringComparer.Ordinal)
                .Select(source => new PlayerFoodSourceSaveData
                {
                    id = source.id,
                    roomId = source.roomId,
                    x = source.worldPosition.x,
                    y = source.worldPosition.y,
                    z = source.worldPosition.z,
                    portions = source.portions,
                    remainingLifetime = source.remainingLifetime,
                    playerPlaced = source.playerPlaced
                })
                .ToList();
        }

        public void Restore(IEnumerable<PlayerFoodSourceSaveData> savedSources)
        {
            sources.Clear();
            nextId = 1;
            foreach (var item in (savedSources ?? Array.Empty<PlayerFoodSourceSaveData>())
                         .Where(item => item != null && item.portions > 0 && item.remainingLifetime > 0f)
                         .Take(MaximumSources))
            {
                sources[item.id] = new PlayerFoodSourceState
                {
                    id = item.id,
                    roomId = item.roomId,
                    worldPosition = new Vector3(item.x, item.y, item.z),
                    portions = Mathf.Clamp(item.portions, 1, PortionsPerSource),
                    remainingLifetime = item.playerPlaced
                        ? Mathf.Min(LifetimeSeconds, item.remainingLifetime)
                        : Mathf.Min((float)SimulationClockModel.CycleSeconds, item.remainingLifetime),
                    playerPlaced = item.playerPlaced
                };
                if (item.id != null && item.id.StartsWith("food-") &&
                    int.TryParse(item.id.Substring(5), out var parsed))
                {
                    nextId = Math.Max(nextId, parsed + 1);
                }
            }
        }

        public void Reset()
        {
            sources.Clear();
            nextId = 1;
        }
    }
}
