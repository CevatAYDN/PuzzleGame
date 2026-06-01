namespace PuzzleGame.Domain.Interfaces
{
    /// <summary>
    /// Player progress per level. Unlock/stars/best moves.
    /// Domain layer — pure interface, swappable persistence (PlayerPrefs/cloud).
    /// </summary>
    public interface ILevelProgressService
    {
        bool IsUnlocked(int levelNumber);
        int GetStars(int levelNumber);
        int GetBestMoves(int levelNumber);
        bool IsCompleted(int levelNumber);

        /// <summary>Records completion. Only overwrites if better than previous.</summary>
        /// <param name="levelNumber">Level finished</param>
        /// <param name="moveCount">Moves used to complete</param>
        /// <param name="stars">Star rating (1-3)</param>
        void RecordCompletion(int levelNumber, int moveCount, int stars);

        /// <summary>Wipes all progress. Used by "Reset Save" debug action.</summary>
        void ResetAll();
    }
}
