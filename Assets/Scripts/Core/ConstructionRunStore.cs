using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace UrbanWildlifeRooms.Core
{
    // Kept separate from the legacy 49-room save. A new construction run must
    // never revive the old complete layout on the next launch.
    public sealed class ConstructionRunStore
    {
        private readonly string filePath;

        public static string DefaultPath => Path.Combine(
            Application.persistentDataPath, "construction-run-v1.json");

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
        }

        public bool TryLoad(out ConstructionRunModel run)
        {
            run = null;
            try
            {
                if (!File.Exists(filePath)) return false;
                var saved = JsonUtility.FromJson<ConstructionRunSaveData>(
                    File.ReadAllText(filePath, Encoding.UTF8));
                return ConstructionRunModel.TryRestore(saved, out run);
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (ArgumentException) { return false; }
        }

        public bool TryDelete()
        {
            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);
                return !File.Exists(filePath);
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }
}
