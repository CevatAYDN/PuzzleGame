using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Contract for secure level progress persistence.
    /// Fix #9: Extracted from static GameSaveManager so implementations
    /// can be injected via DI and mocked in tests.
    /// </summary>
    public interface ISaveManager
    {
        bool Save(int levelIndex, int moveCount, IMoldView[] Molds, bool isCompleted, int stars);
        GameSaveData? LoadLevel(int levelIndex);
        int LoadLastPlayedLevel();
        void DeleteAll();
        bool HasSaveData { get; }
        bool VerifyIntegrity();
    }

    /// <summary>
    /// Minimal POCO carrying loaded level save data — replaces internal GameSaveManager.LevelStateData
    /// for interface consumers (no static dependency).
    /// </summary>
    public struct GameSaveData
    {
        public int LevelIndex;
        public int MoveCount;
        public bool IsCompleted;
        public int Stars;
        public long SavedAtUnix;
    }
}
