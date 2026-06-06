namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Pure C# contract for file-backed save storage with atomic write.
    /// Implementation lives in Infrastructure (Unity persistentDataPath).
    /// Split out of GameSaveManager (Sprint #18) so the orchestrator stays
    /// decoupled from any specific storage backend.
    /// </summary>
    public interface ISaveStorage
    {
        /// <summary>Absolute path of the final save file.</summary>
        string FilePath { get; }

        /// <summary>Absolute path of the temp file used for atomic rename.</summary>
        string TempPath { get; }

        bool Exists();
        string ReadAll();
        void WriteAtomic(string content);
        void Delete();
        long GetSize();
    }
}
