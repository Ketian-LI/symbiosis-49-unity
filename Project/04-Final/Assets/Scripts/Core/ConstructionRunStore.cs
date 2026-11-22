using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace UrbanWildlifeRooms.Core
{
    public enum ConstructionSavedRunStatus
    {
        None, InProgress, Finished, Corrupt, Unavailable
    }

    public enum ConstructionSaveLoadStatus
    {
        Missing, Loaded, Invalid, Unavailable
    }

    // Kept separate from the legacy 49-room save. A new construction run must
    // never revive the old complete layout on the next launch.
    public sealed class ConstructionRunStore
    {
        private readonly string filePath;

        public static string DefaultPath => Path.Combine(
            Application.persistentDataPath, "construction-run-v1.json");

        public static string PathFor(ConstructionGameMode mode) => Path.Combine(
            Application.persistentDataPath, mode switch
            {
                ConstructionGameMode.Fill => "construction-run-v1.json",
                ConstructionGameMode.Endless => "construction-endless-v1.json",
                ConstructionGameMode.BoardGame => "construction-boardgame-v1.json",
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            });

        public ConstructionRunStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A save file path is required.", nameof(path));
            filePath = Path.GetFullPath(path);
        }

        public bool TrySave(ConstructionRunModel run)
        {
            if (run == null) return false;
            try
            {
                // Never replace a file that we could not restore. A damaged or
                // temporarily unreadable save belongs to the player, not to a
                // newly created default run.
                var loadStatus = TryLoadDetailed(out var existing);
                if (loadStatus != ConstructionSaveLoadStatus.Missing &&
                    (loadStatus != ConstructionSaveLoadStatus.Loaded ||
                     existing.Mode != run.Mode))
                    return false;
                var directory = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(directory)) return false;
                Directory.CreateDirectory(directory);
                var temporary = filePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(run.Export()),
                    new UTF8Encoding(false));
                if (File.Exists(filePath)) File.Replace(temporary, filePath, null);
                else File.Move(temporary, filePath);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (System.Security.SecurityException) { return false; }
        }

        // A confirmed new run may replace an unreadable old run, but only
        // after preserving the original file beside the normal save path.
        // Ordinary saves still refuse to replace it.
        public bool TrySaveConfirmedNewRun(ConstructionRunModel run,
            out bool archivedInvalidSave)
        {
            archivedInvalidSave = false;
            if (run == null) return false;
            if (TrySave(run)) return true;

            var status = TryLoadDetailed(out var existing);
            if (status != ConstructionSaveLoadStatus.Invalid &&
                !(status == ConstructionSaveLoadStatus.Loaded &&
                  existing.Mode != run.Mode))
                return false;
            if (!TryArchiveInvalid(run.Mode, out _)) return false;
            archivedInvalidSave = true;
            return TrySave(run);
        }

        public bool TryLoad(out ConstructionRunModel run)
        {
            return TryLoadDetailed(out run) == ConstructionSaveLoadStatus.Loaded;
        }

        public ConstructionSaveLoadStatus TryLoadDetailed(
            out ConstructionRunModel run)
        {
            run = null;
            string json;
            try
            {
                // File.Exists also returns false for some access failures. Read
                // directly so a missing file can be distinguished from one
                // that must be preserved for later recovery.
                json = File.ReadAllText(filePath, new UTF8Encoding(false, true));
            }
            catch (FileNotFoundException) { return ConstructionSaveLoadStatus.Missing; }
            catch (DirectoryNotFoundException) { return ConstructionSaveLoadStatus.Missing; }
            catch (UnauthorizedAccessException) { return ConstructionSaveLoadStatus.Unavailable; }
            catch (IOException) { return ConstructionSaveLoadStatus.Unavailable; }
            catch (System.Security.SecurityException) { return ConstructionSaveLoadStatus.Unavailable; }
            catch (DecoderFallbackException) { return ConstructionSaveLoadStatus.Invalid; }

            try
            {
                var saved = JsonUtility.FromJson<ConstructionRunSaveData>(
                    json);
                return ConstructionRunModel.TryRestore(saved, out run)
                    ? ConstructionSaveLoadStatus.Loaded
                    : ConstructionSaveLoadStatus.Invalid;
            }
            catch (ArgumentException) { return ConstructionSaveLoadStatus.Invalid; }
            catch (InvalidOperationException) { return ConstructionSaveLoadStatus.Invalid; }
            catch (OverflowException) { return ConstructionSaveLoadStatus.Invalid; }
            catch (NullReferenceException) { return ConstructionSaveLoadStatus.Invalid; }
            catch (IndexOutOfRangeException) { return ConstructionSaveLoadStatus.Invalid; }
        }

        public ConstructionSavedRunStatus StatusFor(ConstructionGameMode mode)
        {
            var loadStatus = TryLoadDetailed(out var run);
            if (loadStatus == ConstructionSaveLoadStatus.Missing)
                return ConstructionSavedRunStatus.None;
            if (loadStatus == ConstructionSaveLoadStatus.Unavailable)
                return ConstructionSavedRunStatus.Unavailable;
            if (loadStatus == ConstructionSaveLoadStatus.Invalid || run.Mode != mode)
                return ConstructionSavedRunStatus.Corrupt;
            return run.IsFinished
                ? ConstructionSavedRunStatus.Finished
                : ConstructionSavedRunStatus.InProgress;
        }

        // Called only after the player explicitly chooses to archive an invalid
        // save. The original bytes are moved beside the normal save file;
        // neither an automatic launch nor an ordinary TrySave may do this.
        public bool TryArchiveInvalid(ConstructionGameMode expectedMode,
            out string archivedPath)
        {
            archivedPath = null;
            var status = TryLoadDetailed(out var existing);
            if (status != ConstructionSaveLoadStatus.Invalid &&
                !(status == ConstructionSaveLoadStatus.Loaded &&
                  existing.Mode != expectedMode))
                return false;
            try
            {
                var candidate = filePath + ".invalid-" +
                    DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" +
                    Guid.NewGuid().ToString("N") + ".json";
                File.Move(filePath, candidate);
                archivedPath = candidate;
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (System.Security.SecurityException) { return false; }
        }

        public bool TryDelete()
        {
            try
            {
                var loadStatus = TryLoadDetailed(out _);
                if (loadStatus == ConstructionSaveLoadStatus.Invalid ||
                    loadStatus == ConstructionSaveLoadStatus.Unavailable)
                    return false;
                if (File.Exists(filePath)) File.Delete(filePath);
                return !File.Exists(filePath);
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (System.Security.SecurityException) { return false; }
        }
    }
}
