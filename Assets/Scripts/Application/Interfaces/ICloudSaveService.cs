using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Synchronises all player progress to a cloud-backed save slot.
    /// Collects data from leaderboard, progress, coin wallet, and cosmetic shop services,
    /// serializes to JSON, signs with HMAC, and stores to a single persistent file.
    /// On load it restores all services from the snapshot.
    /// </summary>
    public interface ICloudSaveService
    {
        /// <summary>Whether a cloud save file exists.</summary>
        bool HasCloudSave { get; }

        /// <summary>Timestamp of the last successful save (Unix seconds). 0 if never saved.</summary>
        long LastSavedAtUnix { get; }

        /// <summary>Serialize all progress to a JSON string (before HMAC signing).</summary>
        string SerializeSnapshot();

        /// <summary>Deserialize a JSON snapshot back to structured data.</summary>
        CloudSaveData DeserializeSnapshot(string json);

        /// <summary>Save all progress to the cloud slot.</summary>
        void SaveToCloud();

        /// <summary>Load all progress from the cloud slot and restore services.</summary>
        bool LoadFromCloud();

        /// <summary>Delete the cloud save file.</summary>
        void DeleteCloudSave();
    }
}
