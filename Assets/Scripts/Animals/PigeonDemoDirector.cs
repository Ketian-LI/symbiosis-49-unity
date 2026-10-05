using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UrbanWildlifeRooms.Core;
using UrbanWildlifeRooms.Presentation;
using UrbanWildlifeRooms.UI;

namespace UrbanWildlifeRooms.Animals
{
    /// <summary>
    /// Connects all four wildlife species to the same board-camera follow view.
    /// Camera input and return-to-overview handling live in the camera controller.
    /// </summary>
    public sealed class PigeonDemoDirector : MonoBehaviour
    {
        private BoardCameraController boardCamera;
        private GameRuntimeController runtime;
        private readonly List<PigeonDemoAgent> pigeons = new();
        private readonly List<SquirrelDemoAgent> squirrels = new();
        private readonly List<HedgehogDemoAgent> hedgehogs = new();
        private readonly List<FoxDemoAgent> foxes = new();
        private PigeonDemoAgent selectedPigeon;
        private AnimalFollowSelection selectedOther;
        private Transform selectedAnimal;

        public Transform SelectedAnimal => selectedAnimal;

        public void Initialize(
            BoardCameraController cameraController,
            GameRuntimeController gameRuntime,
            IEnumerable<PigeonDemoAgent> pigeonAgents,
            IEnumerable<SquirrelDemoAgent> squirrelAgents,
            IEnumerable<HedgehogDemoAgent> hedgehogAgents,
            IEnumerable<FoxDemoAgent> foxAgents)
        {
            boardCamera = cameraController;
            runtime = gameRuntime;
            pigeons.AddRange((pigeonAgents ?? Array.Empty<PigeonDemoAgent>()).Where(item => item != null));
            squirrels.AddRange((squirrelAgents ?? Array.Empty<SquirrelDemoAgent>()).Where(item => item != null));
            hedgehogs.AddRange((hedgehogAgents ?? Array.Empty<HedgehogDemoAgent>()).Where(item => item != null));
            foxes.AddRange((foxAgents ?? Array.Empty<FoxDemoAgent>()).Where(item => item != null));
            if (boardCamera != null)
            {
                boardCamera.OverviewRestored += StopFollowing;
                boardCamera.FollowingStopped += StopFollowing;
            }

            foreach (var pigeon in pigeons)
            {
                pigeon.Clicked += StartFollowing;
            }
            foreach (var squirrel in squirrels)
            {
                AddSelection(squirrel, 0.88f);
                squirrel.Clicked += StartFollowing;
            }
            foreach (var hedgehog in hedgehogs)
            {
                AddSelection(hedgehog, 0.76f);
                hedgehog.Clicked += StartFollowing;
            }
            foreach (var fox in foxes)
            {
                AddSelection(fox, 1.20f);
                fox.Clicked += StartFollowing;
            }
        }

        private void Update()
        {
            if (Application.isPlaying && selectedPigeon != null && Input.GetKeyDown(KeyCode.K))
            {
                selectedPigeon.Kill();
            }

            if (selectedAnimal != null &&
                (selectedPigeon != null && !selectedPigeon.IsAlive ||
                 selectedAnimal.TryGetComponent<WildlifeVitality>(out var vitality) && !vitality.IsAlive))
            {
                boardCamera?.ReturnToOverviewIfNeeded();
            }
        }

        private void OnDestroy()
        {
            foreach (var pigeon in pigeons)
            {
                pigeon.Clicked -= StartFollowing;
            }
            foreach (var squirrel in squirrels)
            {
                squirrel.Clicked -= StartFollowing;
            }
            foreach (var hedgehog in hedgehogs)
            {
                hedgehog.Clicked -= StartFollowing;
            }
            foreach (var fox in foxes)
            {
                fox.Clicked -= StartFollowing;
            }

            if (boardCamera != null)
            {
                boardCamera.OverviewRestored -= StopFollowing;
                boardCamera.FollowingStopped -= StopFollowing;
            }
        }

        private void StartFollowing(PigeonDemoAgent selectedPigeon)
        {
            if (selectedPigeon != null && selectedPigeon.IsAlive)
            {
                FollowAnimal(selectedPigeon.transform, 1.45f, selectedPigeon);
            }
        }

        private void StartFollowing(SquirrelDemoAgent squirrel)
        {
            if (squirrel != null && squirrel.IsAlive)
            {
                FollowAnimal(squirrel.transform, 1.45f);
            }
        }

        private void StartFollowing(HedgehogDemoAgent hedgehog)
        {
            if (hedgehog != null && hedgehog.IsAlive)
            {
                FollowAnimal(hedgehog.transform, 1.35f);
            }
        }

        private void StartFollowing(FoxDemoAgent fox)
        {
            if (fox != null && fox.IsAlive)
            {
                FollowAnimal(fox.transform, 1.85f);
            }
        }

        private void FollowAnimal(Transform target, float viewSize, PigeonDemoAgent pigeon = null)
        {
            if (target == null || boardCamera == null || runtime == null ||
                !runtime.HasActiveRun || runtime.AtDesktop || runtime.PauseMenuOpen ||
                runtime.SettingsOpen || runtime.LayoutEditing || runtime.ResultsOpen ||
                runtime.CameraCalibrationOpen || boardCamera.IsMenuView ||
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (selectedPigeon != null && selectedPigeon != pigeon)
            {
                selectedPigeon.SetSelected(false);
            }
            var other = target.GetComponent<AnimalFollowSelection>();
            if (selectedOther != null && selectedOther != other)
            {
                selectedOther.SetSelected(false);
            }
            selectedPigeon = pigeon;
            selectedOther = other;
            selectedAnimal = target;
            selectedPigeon?.SetSelected(true);
            selectedOther?.SetSelected(true);
            boardCamera.Follow(target, viewSize);
        }

        private void StopFollowing()
        {
            if (selectedPigeon != null)
            {
                selectedPigeon.SetSelected(false);
                selectedPigeon = null;
            }
            if (selectedOther != null)
            {
                selectedOther.SetSelected(false);
                selectedOther = null;
            }
            selectedAnimal = null;
        }

        private static void AddSelection(Component animal, float worldSize)
        {
            if (animal == null)
            {
                return;
            }
            var selection = animal.GetComponent<AnimalFollowSelection>() ??
                            animal.gameObject.AddComponent<AnimalFollowSelection>();
            selection.Initialize(worldSize, animal.gameObject.hideFlags);
        }
    }

    // Pigeons already own their follow visuals. The other species use this
    // shared observer feedback so clicking them is equally legible without
    // granting direct control over their autonomous movement.
    public sealed class AnimalFollowSelection : MonoBehaviour
    {
        private GameObject ring;
        private AnimalNeedIndicator needs;

        public bool IsSelected { get; private set; }

        public void Initialize(float worldSize, HideFlags hideFlags)
        {
            if (ring != null)
            {
                return;
            }
            ring = new GameObject("Follow Selection Ring") { hideFlags = hideFlags };
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var renderer = ring.AddComponent<SpriteRenderer>();
            renderer.sprite = AnimalSelectionVisualCatalog.GetSprite(AnimalSelectionVisual.SelectionRing);
            renderer.sortingOrder = 30;
            if (renderer.sprite != null)
            {
                var longestSide = Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);
                ring.transform.localScale = Vector3.one * (worldSize / Mathf.Max(0.001f, longestSide));
            }
            ring.SetActive(false);
            needs = GetComponent<AnimalNeedIndicator>() ?? gameObject.AddComponent<AnimalNeedIndicator>();
            needs.Initialize(hideFlags);
        }

        public void SetSelected(bool value)
        {
            IsSelected = value;
            ring?.SetActive(value);
            needs?.SetSelected(value);
        }

        public void SetNeedWarnings(bool hungry, bool habitatWarning, bool danger, bool alive)
        {
            needs?.SetNeeds(hungry, habitatWarning, danger);
            needs?.SetAlive(alive);
        }
    }
}
