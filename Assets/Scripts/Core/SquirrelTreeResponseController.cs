using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Animals;

namespace UrbanWildlifeRooms.Core
{
    public sealed class SquirrelTreeResponseController : MonoBehaviour
    {
        public const float PanicDurationSeconds = 5f;

        private readonly Dictionary<string, SquirrelDemoAgent> ownerByTreeRoom = new();
        private readonly List<SquirrelDemoAgent> squirrels = new();
        private OakTreeLifecycleController oakTrees;
        private PlayerFeedingController feeding;
        private Transform centralPark;

        public event Action<string, int> CacheExposed;

        public void Initialize(
            OakTreeLifecycleController treeController,
            PlayerFeedingController feedingController,
            IReadOnlyList<SquirrelDemoAgent> squirrelAgents,
            Transform centralParkTransform)
        {
            oakTrees = treeController;
            feeding = feedingController;
            centralPark = centralParkTransform;
            squirrels.AddRange(squirrelAgents ?? Array.Empty<SquirrelDemoAgent>());
            var ownedTrees = new[] { "oak-a", "oak-b", "oak-d" };
            for (var index = 0; index < ownedTrees.Length && index < squirrels.Count; index++)
            {
                ownerByTreeRoom[ownedTrees[index]] = squirrels[index];
            }
            oakTrees.TreeFelled += HandleTreeFelled;
        }

        private void OnDestroy()
        {
            if (oakTrees != null)
            {
                oakTrees.TreeFelled -= HandleTreeFelled;
            }
        }

        private void HandleTreeFelled(string roomId)
        {
            if (ownerByTreeRoom.TryGetValue(roomId, out var owner))
            {
                var cachePosition = owner.CachePosition;
                var portions = owner.ExposeCache();
                if (portions > 0)
                {
                    feeding.AddExposedCacheFood(cachePosition, portions);
                    CacheExposed?.Invoke(roomId, portions);
                }
                ownerByTreeRoom.Remove(roomId);
            }

            var safeCenter = centralPark != null ? centralPark.position : Vector3.zero;
            for (var index = 0; index < squirrels.Count; index++)
            {
                var offset = new Vector3((index - 1.5f) * 0.28f, 0.30f, index % 2 == 0 ? 0.35f : -0.35f);
                squirrels[index].BeginPanic(PanicDurationSeconds, safeCenter + offset);
            }
        }
    }
}
