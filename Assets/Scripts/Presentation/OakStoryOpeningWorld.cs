using System.Collections.Generic;
using UnityEngine;
using UrbanWildlifeRooms.Animals;
using UrbanWildlifeRooms.People;

namespace UrbanWildlifeRooms.Presentation
{
    // The opening is a separate, frozen story stage. The existing 49-room
    // simulation is restored unchanged when the player leaves this stage.
    public sealed class OakStoryOpeningWorld
    {
        private readonly Transform worldRoot;
        private readonly Transform oakRoot;
        private readonly SquirrelDemoAgent squirrel;
        private readonly RoomView homeRoom;
        private readonly RoomView workRoom;
        private readonly RoomView mealRoom;
        private readonly CitizenDemoAgent resident;
        private readonly List<(GameObject target, bool wasActive)> objects = new();
        private readonly List<(Renderer target, bool wasEnabled)> renderers = new();
        private readonly List<(Collider target, bool wasEnabled)> colliders = new();
        private readonly List<(Transform target, Vector3 originalPosition)> stagedRooms = new();
        private Vector3 residentHome;
        private Transform stageCenter;

        public bool IsActive { get; private set; }
        public Transform StageCenter => stageCenter;

        public OakStoryOpeningWorld(Transform worldRoot, RoomView oakRoom, SquirrelDemoAgent squirrelAgent,
            RoomView home, RoomView work, RoomView meal, CitizenDemoAgent openingResident)
        {
            this.worldRoot = worldRoot;
            oakRoot = oakRoom != null ? oakRoom.VisualRoot : null;
            squirrel = squirrelAgent;
            homeRoom = home;
            workRoom = work;
            mealRoom = meal;
            resident = openingResident;
        }

        public bool Enter()
        {
            if (IsActive || worldRoot == null || oakRoot == null || squirrel == null || !squirrel.IsAlive ||
                homeRoom == null || workRoom == null || mealRoom == null || resident == null)
                return false;

            residentHome = resident.SpawnPosition;

            // Capture the original state first. World-space food, road markings,
            // traffic and population visuals may live outside their room roots.
            foreach (var item in worldRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (BelongsToOpening(item.transform)) continue;
                renderers.Add((item, item.enabled));
                item.enabled = false;
            }
            foreach (var item in worldRoot.GetComponentsInChildren<Collider>(true))
            {
                if (BelongsToOpening(item.transform)) continue;
                colliders.Add((item, item.enabled));
                item.enabled = false;
            }

            foreach (var room in worldRoot.GetComponentsInChildren<RoomView>(true))
            {
                if (room.VisualRoot != oakRoot) Hide(room.VisualRoot.gameObject);
            }
            foreach (var actor in worldRoot.GetComponentsInChildren<SquirrelDemoAgent>(true))
            {
                if (actor != squirrel) Hide(actor.gameObject);
            }
            foreach (var actor in worldRoot.GetComponentsInChildren<PigeonDemoAgent>(true))
                Hide(actor.gameObject);
            foreach (var actor in worldRoot.GetComponentsInChildren<HedgehogDemoAgent>(true))
                Hide(actor.gameObject);
            foreach (var actor in worldRoot.GetComponentsInChildren<FoxDemoAgent>(true))
                Hide(actor.gameObject);
            foreach (var actor in worldRoot.GetComponentsInChildren<CitizenDemoAgent>(true))
                Hide(actor.gameObject);

            stageCenter = new GameObject("Story room group centre").transform;
            stageCenter.SetParent(worldRoot, false);
            stageCenter.position = oakRoot.position + new Vector3(-2.15f, 0f, 2.15f);

            IsActive = true;
            return true;
        }

        public void RevealHome() => RevealRoom(homeRoom, new Vector3(-4.3f, 0f, 0f));

        public void RevealResident()
        {
            if (!IsActive || resident == null || homeRoom == null) return;
            RevealHome();
            resident.MoveHome(homeRoom.VisualRoot.position + new Vector3(0.48f, 0.30f, -0.40f),
                new Vector2(0.52f, 0.52f));
            RestoreVisibility(resident.gameObject);
        }

        public void RevealWork() => RevealRoom(workRoom, new Vector3(-4.3f, 0f, 4.3f));

        public void RevealMeal() => RevealRoom(mealRoom, new Vector3(0f, 0f, 4.3f));

        public void Exit()
        {
            if (!IsActive) return;
            foreach (var item in stagedRooms)
                if (item.target != null) item.target.position = item.originalPosition;
            if (resident != null) resident.MoveHome(residentHome, new Vector2(0.52f, 0.52f));
            foreach (var item in objects)
                if (item.target != null) item.target.SetActive(item.wasActive);
            foreach (var item in renderers)
                if (item.target != null) item.target.enabled = item.wasEnabled;
            foreach (var item in colliders)
                if (item.target != null) item.target.enabled = item.wasEnabled;
            objects.Clear();
            renderers.Clear();
            colliders.Clear();
            stagedRooms.Clear();
            DestroyStageCenter();
            IsActive = false;
        }

        public void Abandon()
        {
            // Scene teardown destroys these objects together. Reactivating
            // children during OnDestroy produces Unity errors and serves no purpose.
            objects.Clear();
            renderers.Clear();
            colliders.Clear();
            stagedRooms.Clear();
            DestroyStageCenter();
            IsActive = false;
        }

        private bool BelongsToOpening(Transform item)
        {
            return item.IsChildOf(oakRoot) || item.IsChildOf(squirrel.transform) ||
                   item.GetComponentInParent<TabletopBackdropAlignment>() != null;
        }

        private void Hide(GameObject target)
        {
            objects.Add((target, target.activeSelf));
            target.SetActive(false);
        }

        private void RevealRoom(RoomView room, Vector3 offset)
        {
            if (!IsActive || room?.VisualRoot == null) return;
            var root = room.VisualRoot;
            if (stagedRooms.Exists(item => item.target == root)) return;
            stagedRooms.Add((root, root.position));
            root.position = oakRoot.position + offset;
            RestoreVisibility(root.gameObject);
        }

        private void RestoreVisibility(GameObject target)
        {
            foreach (var item in objects)
                if (item.target == target) target.SetActive(item.wasActive);
            foreach (var item in renderers)
                if (item.target != null && item.target.transform.IsChildOf(target.transform))
                    item.target.enabled = item.wasEnabled;
            foreach (var item in colliders)
                if (item.target != null && item.target.transform.IsChildOf(target.transform))
                    item.target.enabled = item.wasEnabled;
            Physics.SyncTransforms();
        }

        private void DestroyStageCenter()
        {
            if (stageCenter == null) return;
            if (Application.isPlaying) Object.Destroy(stageCenter.gameObject);
            else Object.DestroyImmediate(stageCenter.gameObject);
            stageCenter = null;
        }
    }
}
