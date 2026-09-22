using System;

namespace UrbanWildlifeRooms.Core
{
    public enum WasteRoomLoadStage
    {
        Empty,
        Low,
        Medium,
        Full,
        Overflow
    }

    /// <summary>
    /// Unity-independent load model for one waste room. The room can hold eight
    /// units before excess waste becomes overflow feedback in the world.
    /// </summary>
    [Serializable]
    public sealed class WasteRoomLoadModel
    {
        public const int Capacity = 8;

        private int units;

        public event Action Changed;

        public int Units => units;
        public int StoredUnits => Math.Min(units, Capacity);
        public int OverflowUnits => Math.Max(0, units - Capacity);
        public bool IsOverflowing => units > Capacity;
        public WasteRoomLoadStage Stage => StageForUnits(units);

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Waste amount must be positive.");
            }

            var next = (long)units + amount;
            units = next > int.MaxValue ? int.MaxValue : (int)next;
            Changed?.Invoke();
        }

        public void Restore(int amount)
        {
            var next = Math.Max(0, amount);
            if (units == next)
            {
                return;
            }

            units = next;
            Changed?.Invoke();
        }

        public void CollectAll()
        {
            Restore(0);
        }

        public static WasteRoomLoadStage StageForUnits(int amount)
        {
            if (amount <= 0)
            {
                return WasteRoomLoadStage.Empty;
            }

            if (amount <= 2)
            {
                return WasteRoomLoadStage.Low;
            }

            if (amount <= 5)
            {
                return WasteRoomLoadStage.Medium;
            }

            return amount <= Capacity
                ? WasteRoomLoadStage.Full
                : WasteRoomLoadStage.Overflow;
        }
    }
}
