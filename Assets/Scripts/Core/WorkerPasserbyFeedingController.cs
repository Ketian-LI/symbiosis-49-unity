using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.People;
using UrbanWildlifeRooms.Presentation;

namespace UrbanWildlifeRooms.Core
{
    // One free meal per worker per day keeps passing encounters meaningful
    // without turning every frame of a commute into unlimited food.
    public sealed class WorkerFeedingQuota
    {
        private readonly HashSet<string> fedResidents = new();

        public int DayNumber { get; private set; } = -1;
        public IReadOnlyCollection<string> FedResidents => fedResidents;

        public bool CanFeed(int dayNumber, string residentId)
        {
            AdvanceTo(dayNumber);
            return !string.IsNullOrEmpty(residentId) && !fedResidents.Contains(residentId);
        }

        public bool TryRecord(int dayNumber, string residentId)
        {
            return CanFeed(dayNumber, residentId) && fedResidents.Add(residentId);
        }

        public void Restore(int dayNumber, IEnumerable<string> residentIds)
        {
            DayNumber = dayNumber;
            fedResidents.Clear();
            foreach (var id in residentIds ?? Array.Empty<string>())
            {
                if (!string.IsNullOrEmpty(id))
                {
                    fedResidents.Add(id);
                }
            }
        }

        public void Reset()
        {
            DayNumber = -1;
            fedResidents.Clear();
        }

        public static float DistanceSquaredToSegmentXZ(
            Vector3 point, Vector3 from, Vector3 to, out Vector3 nearest)
        {
            var direction = new Vector2(to.x - from.x, to.z - from.z);
            var offset = new Vector2(point.x - from.x, point.z - from.z);
            var t = direction.sqrMagnitude <= 0.000001f
                ? 0f
                : Mathf.Clamp01(Vector2.Dot(offset, direction) / direction.sqrMagnitude);
            nearest = Vector3.Lerp(from, to, t);
            var dx = point.x - nearest.x;
            var dz = point.z - nearest.z;
            return dx * dx + dz * dz;
        }

        private void AdvanceTo(int dayNumber)
        {
            if (DayNumber == dayNumber)
            {
                return;
            }
            DayNumber = dayNumber;
            fedResidents.Clear();
        }
    }

    public sealed class WorkerPasserbyFeedingController : MonoBehaviour
    {
        // A close pass keeps Sandbox feeding useful without making its untouched
        // board self-sustaining. Preserve the assessed Research-mode radius.
        private const float SandboxFeedRadius = 0.9f;
        private const float ResearchFeedRadius = 1.15f;
        private readonly List<IWildlifeLayoutAgent> animals = new();
        private readonly WorkerFeedingQuota quota = new();
        private CitizenPopulationPresenter citizens;
        private ResidentPopulationController residents;
        private AnimalNeedsController needs;
        private GameRuntimeController runtime;
        private Transform mapRoot;
        private HideFlags generatedHideFlags;

        public WorkerFeedingQuota Quota => quota;

        public void Initialize(
            GameRuntimeController runtimeController,
            ResidentPopulationController residentController,
            CitizenPopulationPresenter citizenPresenter,
            AnimalNeedsController needsController,
            Transform boardRoot,
            HideFlags hideFlags,
            IEnumerable<IWildlifeLayoutAgent> wildlife)
        {
            runtime = runtimeController;
            residents = residentController;
            citizens = citizenPresenter;
            needs = needsController;
            mapRoot = boardRoot;
            generatedHideFlags = hideFlags;
            animals.AddRange(wildlife ?? Array.Empty<IWildlifeLayoutAgent>());
            citizens.WorkerCommuteMoved += HandleWorkerCommuteMoved;
            runtime.RestartRequested += HandleRestartRequested;
        }

        private void OnDestroy()
        {
            if (citizens != null)
            {
                citizens.WorkerCommuteMoved -= HandleWorkerCommuteMoved;
            }
            if (runtime != null)
            {
                runtime.RestartRequested -= HandleRestartRequested;
            }
        }

        private void HandleRestartRequested()
        {
            quota.Reset();
        }

        private void HandleWorkerCommuteMoved(string residentId, Vector3 from, Vector3 to)
        {
            if (runtime == null || !runtime.HasActiveRun || runtime.IsPaused ||
                residents?.Model?.NavigationMap == null || mapRoot == null ||
                !quota.CanFeed(runtime.Clock.DayNumber, residentId))
            {
                return;
            }

            var navigation = residents.Model.NavigationMap;
            IWildlifeLayoutAgent closest = null;
            var feedRadius = runtime.Mode == GameMode.Sandbox
                ? SandboxFeedRadius
                : ResearchFeedRadius;
            var closestDistance = feedRadius * feedRadius;
            foreach (var animal in animals)
            {
                if (animal?.AgentTransform == null || !needs.CanReceiveWorkerMeal(animal))
                {
                    continue;
                }

                var animalPosition = animal.AgentTransform.position;
                var distance = WorkerFeedingQuota.DistanceSquaredToSegmentXZ(
                    animalPosition, from, to, out var nearest);
                if (distance >= closestDistance)
                {
                    continue;
                }

                var localAnimal = mapRoot.InverseTransformPoint(animalPosition);
                var localNearest = mapRoot.InverseTransformPoint(nearest);
                if (!navigation.TryFindRoomContaining(
                        new Vector2(localAnimal.x, localAnimal.z), out var animalRoom) ||
                    !navigation.TryFindRoomContaining(
                        new Vector2(localNearest.x, localNearest.z), out var workerRoom) ||
                    animalRoom != workerRoom)
                {
                    continue;
                }

                closest = animal;
                closestDistance = distance;
            }

            if (closest == null || !needs.TryFeedFromWorker(closest))
            {
                return;
            }

            quota.TryRecord(runtime.Clock.DayNumber, residentId);
            ShowMealCue(closest.AgentTransform.position);
        }

        private void ShowMealCue(Vector3 animalPosition)
        {
            var marker = new GameObject("Worker Fed Animal", typeof(TextMesh))
            {
                hideFlags = generatedHideFlags
            };
            marker.transform.SetParent(transform, false);
            marker.transform.position = animalPosition + Vector3.up * 0.72f;
            marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var label = marker.GetComponent<TextMesh>();
            label.font = UrbanFontResolver.GetFont();
            label.fontSize = 72;
            label.characterSize = 0.028f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.96f, 0.83f, 0.48f, 1f);
            label.text = "+1";
            var renderer = marker.GetComponent<MeshRenderer>();
            if (renderer != null && label.font != null)
            {
                renderer.sharedMaterial = label.font.material;
            }
            StartCoroutine(AnimateMealCue(marker.transform));
        }

        private static IEnumerator AnimateMealCue(Transform marker)
        {
            var remaining = 1.1f;
            while (remaining > 0f && marker != null)
            {
                var deltaTime = Time.deltaTime;
                marker.position += Vector3.up * (deltaTime * 0.3f);
                remaining -= deltaTime;
                yield return null;
            }
            if (marker != null)
            {
                Destroy(marker.gameObject);
            }
        }
    }
}
