using System;
using UnityEngine;

namespace UrbanWildlifeRooms.Presentation
{
    public enum StreetVehicleEntrySide
    {
        North, East, South, West, NorthEast, SouthEast, SouthWest, NorthWest
    }

    // This view follows the daily model's traffic result. Its ordinary passes
    // are decorative; QueueCollision only presents an already settled event.
    public sealed class ConstructionStreetVehicleVisual : MonoBehaviour
    {
        private const float TravelSeconds = 4f;
        private const float BetweenCarsSeconds = 1.4f;
        private const float LaneEnd = 1.75f;
        private const float EntranceLength = 0.35f;
        private const float CollisionCentrePhase = TravelSeconds * 0.5f;
        private const float CollisionHoldSeconds = 0.85f;
        private bool configured;
        private bool allowEastWest;
        private bool allowNorthSouth;
        private bool allowNorthEastSouthWest;
        private bool allowNorthWestSouthEast;
        private bool eastWest;
        private Vector3 laneDirection;
        private float phaseSeconds;
        private Vector3 fullSize;
        private bool roadsideWildlife;
        private GameObject brakeLamps;
        private bool collisionPending;
        private bool collisionWaitForWrap;
        private bool collisionHolding;
        private float collisionHoldElapsed;
        private readonly System.Random approachRandom =
            new System.Random(Guid.NewGuid().GetHashCode());

        public bool EastWest => eastWest;
        public StreetVehicleEntrySide EntrySide { get; private set; }
        public bool CollisionContact => collisionHolding;

        // Schedule the impact at the centre of this car's current or next
        // natural pass. Returning the time to contact lets the wildlife view
        // cross in sync without changing the car's random entry direction.
        public float QueueCollision()
        {
            if (!configured) return -1f;
            collisionPending = true;
            collisionWaitForWrap = phaseSeconds > CollisionCentrePhase - 0.55f;
            collisionHolding = false;
            collisionHoldElapsed = 0f;
            var cycle = TravelSeconds + BetweenCarsSeconds;
            return phaseSeconds > CollisionCentrePhase - 0.55f
                ? cycle - phaseSeconds + CollisionCentrePhase
                : CollisionCentrePhase - phaseSeconds;
        }

        public void CancelCollision()
        {
            collisionPending = false;
            collisionWaitForWrap = false;
            collisionHolding = false;
            collisionHoldElapsed = 0f;
            if (configured) UpdatePosition();
        }

        public void SetRoadsideWildlife(int count)
        {
            roadsideWildlife = count > 0;
            if (configured) UpdatePosition();
        }

        public void SetBrakeLamps(GameObject lamps)
        {
            brakeLamps = lamps;
            if (configured) UpdatePosition();
            else if (brakeLamps != null) brakeLamps.SetActive(false);
        }

        // The car covers less distance near the road centre, then resumes its
        // normal speed. The endpoints and total pass time stay unchanged.
        public static float PassageProgress(float normalizedTime,
            bool slowForWildlife)
        {
            var time = Mathf.Clamp01(normalizedTime);
            if (!slowForWildlife) return time;
            // This curve slows smoothly through the centre, stays monotonic,
            // and still enters and exits the tile at the same times.
            return Mathf.Clamp01(time +
                0.075f * Mathf.Sin(2f * Mathf.PI * time));
        }

        public void Configure(bool moveEastWest, float initialProgress)
            => Configure(moveEastWest, !moveEastWest, initialProgress);

        public void Configure(bool mayMoveEastWest, bool mayMoveNorthSouth,
            float initialProgress)
            => Configure(mayMoveEastWest, mayMoveNorthSouth, false, false,
                initialProgress);

        public void Configure(bool mayMoveEastWest, bool mayMoveNorthSouth,
            bool mayMoveNorthEastSouthWest, bool mayMoveNorthWestSouthEast,
            float initialProgress)
        {
            if (!mayMoveEastWest && !mayMoveNorthSouth &&
                !mayMoveNorthEastSouthWest && !mayMoveNorthWestSouthEast)
            {
                mayMoveEastWest = true;
                mayMoveNorthSouth = true;
            }
            if (configured && allowEastWest == mayMoveEastWest &&
                allowNorthSouth == mayMoveNorthSouth &&
                allowNorthEastSouthWest == mayMoveNorthEastSouthWest &&
                allowNorthWestSouthEast == mayMoveNorthWestSouthEast) return;
            if (!configured) fullSize = transform.localScale;
            // A new adjoining street can change the preferred direction. Start
            // the next pass at its entrance instead of snapping a visible car
            // sideways across the intersection.
            phaseSeconds = configured ? 0f : Mathf.Repeat(initialProgress, 1f) * TravelSeconds;
            configured = true;
            allowEastWest = mayMoveEastWest;
            allowNorthSouth = mayMoveNorthSouth;
            allowNorthEastSouthWest = mayMoveNorthEastSouthWest;
            allowNorthWestSouthEast = mayMoveNorthWestSouthEast;
            ChooseNextApproach();
            UpdatePosition();
        }

        public static StreetVehicleEntrySide PickEntrySide(bool mayMoveEastWest,
            bool mayMoveNorthSouth, System.Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (!mayMoveEastWest && !mayMoveNorthSouth)
            {
                mayMoveEastWest = true;
                mayMoveNorthSouth = true;
            }
            var choice = random.Next((mayMoveEastWest ? 2 : 0) +
                (mayMoveNorthSouth ? 2 : 0));
            if (mayMoveEastWest)
            {
                if (choice == 0) return StreetVehicleEntrySide.West;
                if (choice == 1) return StreetVehicleEntrySide.East;
                choice -= 2;
            }
            return choice == 0 ? StreetVehicleEntrySide.South :
                StreetVehicleEntrySide.North;
        }

        public static StreetVehicleEntrySide PickEntrySide(bool mayMoveEastWest,
            bool mayMoveNorthSouth, bool mayMoveNorthEastSouthWest,
            bool mayMoveNorthWestSouthEast, System.Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            var cardinal = (mayMoveEastWest ? 2 : 0) +
                (mayMoveNorthSouth ? 2 : 0);
            var total = cardinal + (mayMoveNorthEastSouthWest ? 2 : 0) +
                (mayMoveNorthWestSouthEast ? 2 : 0);
            if (total == 0)
                return PickEntrySide(true, true, random);
            var choice = random.Next(total);
            if (choice < cardinal)
                return PickEntrySide(mayMoveEastWest, mayMoveNorthSouth,
                    random);
            choice -= cardinal;
            if (mayMoveNorthEastSouthWest)
            {
                if (choice == 0) return StreetVehicleEntrySide.NorthEast;
                if (choice == 1) return StreetVehicleEntrySide.SouthWest;
                choice -= 2;
            }
            return choice == 0 ? StreetVehicleEntrySide.NorthWest :
                StreetVehicleEntrySide.SouthEast;
        }

        private void ChooseNextApproach()
        {
            EntrySide = PickEntrySide(allowEastWest, allowNorthSouth,
                allowNorthEastSouthWest, allowNorthWestSouthEast,
                approachRandom);
            eastWest = EntrySide is StreetVehicleEntrySide.East or
                StreetVehicleEntrySide.West;
            laneDirection = EntrySide switch
            {
                StreetVehicleEntrySide.West => Vector3.right,
                StreetVehicleEntrySide.East => Vector3.left,
                StreetVehicleEntrySide.South => Vector3.forward,
                StreetVehicleEntrySide.North => Vector3.back,
                StreetVehicleEntrySide.SouthWest =>
                    new Vector3(1f, 0f, 1f),
                StreetVehicleEntrySide.NorthEast =>
                    new Vector3(-1f, 0f, -1f),
                StreetVehicleEntrySide.NorthWest =>
                    new Vector3(1f, 0f, -1f),
                _ => new Vector3(-1f, 0f, 1f)
            };
            transform.localRotation = Quaternion.Euler(0f,
                Mathf.Atan2(laneDirection.x, laneDirection.z) *
                    Mathf.Rad2Deg, 0f);
        }

        private void Update()
        {
            if (!configured) return;
            if (collisionHolding)
            {
                collisionHoldElapsed += Time.unscaledDeltaTime;
                if (collisionHoldElapsed >= CollisionHoldSeconds)
                {
                    collisionHolding = false;
                    collisionPending = false;
                    collisionHoldElapsed = 0f;
                }
                UpdatePosition();
                return;
            }
            var previousPhase = phaseSeconds;
            phaseSeconds = Mathf.Repeat(phaseSeconds + Time.unscaledDeltaTime,
                TravelSeconds + BetweenCarsSeconds);
            if (phaseSeconds < previousPhase)
            {
                ChooseNextApproach();
                collisionWaitForWrap = false;
            }
            if (collisionPending && !collisionWaitForWrap &&
                previousPhase < CollisionCentrePhase &&
                phaseSeconds >= CollisionCentrePhase &&
                phaseSeconds >= previousPhase)
            {
                phaseSeconds = CollisionCentrePhase;
                collisionHolding = true;
                collisionHoldElapsed = 0f;
            }
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            // Each miniature car makes one straight pass, disappears through
            // the far threshold, and leaves a short traffic gap. The loop
            // resets only while the car is invisible, never in the road centre.
            var passing = phaseSeconds < TravelSeconds;
            var normalizedTime = Mathf.Clamp01(phaseSeconds / TravelSeconds);
            var distance = passing
                ? Mathf.Lerp(-LaneEnd, LaneEnd,
                    PassageProgress(normalizedTime, roadsideWildlife))
                : LaneEnd;
            var entrance = passing
                ? Mathf.Clamp01((LaneEnd - Mathf.Abs(distance)) / EntranceLength)
                : 0f;
            transform.localScale = fullSize * Mathf.SmoothStep(0f, 1f, entrance);
            if (brakeLamps != null)
                brakeLamps.SetActive(collisionHolding
                    ? Mathf.FloorToInt(collisionHoldElapsed / 0.11f) % 2 == 0
                    : passing && roadsideWildlife &&
                      normalizedTime >= 0.4f && normalizedTime <= 0.6f);
            transform.localPosition = laneDirection * distance +
                new Vector3(0f, 0.38f, 0f);
        }
    }
}
