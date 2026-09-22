using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlifeRooms.Data;

namespace UrbanWildlifeRooms.Core
{
    public sealed class PhysicalBoardValidationResult
    {
        public bool IsCompleteAndLegal { get; set; }
        public int RecognisedModuleCount { get; set; }
        public IReadOnlyList<int> AffectedCells { get; set; } = Array.Empty<int>();
        public string Signature { get; set; } = string.Empty;
    }

    public static class PhysicalBoardRecognitionModel
    {
        public const float RequiredStableSeconds = 1.5f;

        public static PhysicalBoardValidationResult Validate(IEnumerable<RoomPlacementData> observation)
        {
            var placements = (observation ?? Array.Empty<RoomPlacementData>())
                .Where(item => item != null)
                .ToList();
            var model = new RoomLayoutModel(RoomLayoutData.All);
            var legal = model.TryRestore(placements);
            return new PhysicalBoardValidationResult
            {
                IsCompleteAndLegal = legal,
                RecognisedModuleCount = placements.Select(item => item.id).Distinct().Count(),
                AffectedCells = legal ? Array.Empty<int>() : FindAffectedCells(placements),
                Signature = Signature(placements)
            };
        }

        public static string Signature(IEnumerable<RoomPlacementData> placements)
        {
            return string.Join("|", (placements ?? Array.Empty<RoomPlacementData>())
                .Where(item => item != null)
                .OrderBy(item => item.id, StringComparer.Ordinal)
                .Select(item => $"{item.id}:{item.column},{item.row},r{NormalizeTurns(item.quarterTurns)}"));
        }

        private static IReadOnlyList<int> FindAffectedCells(IReadOnlyList<RoomPlacementData> placements)
        {
            var specs = RoomLayoutData.All.ToDictionary(item => item.Id);
            var occupants = new Dictionary<int, int>();
            var affected = new HashSet<int>();
            foreach (var placement in placements)
            {
                if (string.IsNullOrEmpty(placement.id) || !specs.TryGetValue(placement.id, out var spec))
                {
                    continue;
                }
                var rotated = NormalizeTurns(placement.quarterTurns) % 2 == 1;
                var width = rotated ? spec.Height : spec.Width;
                var height = rotated ? spec.Width : spec.Height;
                for (var row = placement.row; row < placement.row + height; row++)
                {
                    for (var column = placement.column; column < placement.column + width; column++)
                    {
                        if (column < 0 || row < 0 || column >= RoomLayoutData.GridSize || row >= RoomLayoutData.GridSize)
                        {
                            continue;
                        }
                        var cell = row * RoomLayoutData.GridSize + column;
                        occupants[cell] = occupants.TryGetValue(cell, out var count) ? count + 1 : 1;
                    }
                }
            }
            for (var cell = 0; cell < RoomLayoutData.GridSize * RoomLayoutData.GridSize; cell++)
            {
                if (!occupants.TryGetValue(cell, out var count) || count != 1)
                {
                    affected.Add(cell);
                }
            }
            return affected.OrderBy(item => item).ToArray();
        }

        private static int NormalizeTurns(int turns)
        {
            return (turns % 4 + 4) % 4;
        }
    }

    public sealed class PhysicalBoardStabilityTracker
    {
        private string signature = string.Empty;
        private float stableSeconds;

        public float Progress => Math.Clamp(
            stableSeconds / PhysicalBoardRecognitionModel.RequiredStableSeconds,
            0f,
            1f);

        public void Observe(string nextSignature, float deltaTime)
        {
            if (string.IsNullOrEmpty(nextSignature) || nextSignature != signature)
            {
                signature = nextSignature ?? string.Empty;
                stableSeconds = 0f;
                return;
            }
            stableSeconds += Math.Max(0f, deltaTime);
        }

        public void Reset()
        {
            signature = string.Empty;
            stableSeconds = 0f;
        }
    }
}
