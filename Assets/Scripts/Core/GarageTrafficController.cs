using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlifeRooms.Data;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    public sealed class GarageTrafficController : MonoBehaviour
    {
        private readonly Dictionary<string, RoomView> garages = new();
        private readonly HashSet<string> garageIds = new();
        private readonly System.Random random = new(4907);
        private GameRuntimeController runtime;
        private AnimalNavigationCoordinator navigation;
        private Material material;
        private HideFlags generatedHideFlags;
        private float nextVehicleTime = 1.5f;
        private int vehicleIndex;

        public event Action<string> VehicleSpawned;

        public void Initialize(
            GameRuntimeController runtimeController,
            AnimalNavigationCoordinator navigationCoordinator,
            IEnumerable<RoomView> rooms,
            Material sharedMaterial,
            HideFlags generatedHideFlags)
        {
            runtime = runtimeController;
            navigation = navigationCoordinator;
            material = sharedMaterial;
            this.generatedHideFlags = generatedHideFlags;
            foreach (var room in rooms ?? Array.Empty<RoomView>())
            {
                if (room.Spec.Type != RoomType.Garage)
                {
                    continue;
                }
                garages[room.Spec.Id] = room;
                garageIds.Add(room.Spec.Id);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || runtime == null || !runtime.HasActiveRun || runtime.IsPaused)
            {
                return;
            }
            nextVehicleTime -= Time.deltaTime;
            if (nextVehicleTime > 0f || garages.Count == 0)
            {
                return;
            }
            var garage = garages.Values.OrderBy(item => item.Spec.Id, StringComparer.Ordinal)
                .ElementAt(vehicleIndex % garages.Count);
            SpawnVehicle(garage);
            vehicleIndex++;
            nextVehicleTime = 4f + (float)random.NextDouble() * 4f;
        }

        public GroundRoutePlan PlanRoute(string startRoomId, string destinationRoomId)
        {
            if (!navigation.NavigationMap.TryFindRoute(startRoomId, destinationRoomId, out var route))
            {
                return new GroundRoutePlan { Decision = GarageCrossingDecision.Abandon };
            }
            var garageIndex = -1;
            for (var index = 1; index < route.Count; index++)
            {
                if (garageIds.Contains(route[index]))
                {
                    garageIndex = index;
                    break;
                }
            }
            if (garageIndex < 0)
            {
                return new GroundRoutePlan
                {
                    Rooms = route,
                    Decision = GarageCrossingDecision.NoGarage
                };
            }

            var hasDetour = navigation.NavigationMap.TryFindRoute(
                startRoomId,
                destinationRoomId,
                garageIds,
                out var detour);
            var decision = GarageTrafficModel.Decide(
                true,
                hasDetour,
                (float)random.NextDouble(),
                (float)random.NextDouble());
            return new GroundRoutePlan
            {
                Rooms = decision == GarageCrossingDecision.Detour ? detour : route,
                Decision = decision,
                HazardConnectionIndex = decision == GarageCrossingDecision.CrossSafely ||
                                        decision == GarageCrossingDecision.CrossFatally
                    ? garageIndex - 1
                    : -1
            };
        }

        private void SpawnVehicle(RoomView garage)
        {
            var root = new GameObject($"Passing Car {vehicleIndex + 1:000}") { hideFlags = generatedHideFlags };
            root.transform.SetParent(garage.VisualRoot, false);
            var forward = vehicleIndex % 2 == 0;
            var halfDepth = garage.Spec.Height * WorldScaleStandards.CellSizeMeters * 0.5f - 0.24f;
            var laneX = GarageVisualLayout.LaneCenterX(garage.Spec.Width);
            var start = new Vector3(laneX, 0.34f, forward ? halfDepth : -halfDepth);
            var end = new Vector3(laneX, 0.34f, forward ? -halfDepth : halfDepth);
            var paint = forward ? new Color(0.78f, 0.29f, 0.20f) : new Color(0.24f, 0.45f, 0.62f);
            CarPart("Car Body", new Vector3(0f, 0.07f, 0f), new Vector3(0.72f, 0.27f, 1.10f), paint);
            CarPart("Car Cabin", new Vector3(0f, 0.27f, -0.05f), new Vector3(0.57f, 0.21f, 0.55f), paint);
            CarPart("Front Windscreen", new Vector3(0f, 0.28f, 0.238f), new Vector3(0.48f, 0.12f, 0.045f),
                new Color(0.28f, 0.38f, 0.43f));
            CarPart("Rear Window", new Vector3(0f, 0.28f, -0.34f), new Vector3(0.46f, 0.11f, 0.045f),
                new Color(0.28f, 0.38f, 0.43f));
            for (var side = -1; side <= 1; side += 2)
            {
                for (var axle = -1; axle <= 1; axle += 2)
                {
                    CarPart($"Wheel {side} {axle}", new Vector3(side * 0.39f, -0.055f, axle * 0.34f),
                        new Vector3(0.10f, 0.16f, 0.19f), new Color(0.12f, 0.13f, 0.15f));
                }
                CarPart($"Headlight {side}", new Vector3(side * 0.25f, 0.09f, 0.563f),
                    new Vector3(0.11f, 0.07f, 0.025f), new Color(0.97f, 0.89f, 0.67f));
            }
            root.AddComponent<GarageVehicleVisual>().Initialize(start, end, 1.25f);
            VehicleSpawned?.Invoke(garage.Spec.Id);

            void CarPart(string name, Vector3 position, Vector3 scale, Color color)
            {
                UrbanVisualFactory.CreatePrimitive(PrimitiveType.Cube, name, root.transform,
                    position, scale, color, material, true, generatedHideFlags);
            }
        }
    }
}
