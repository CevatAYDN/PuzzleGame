using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    public interface IHintService
    {
        int Cost { get; }
        int RemainingHintsForCurrentLevel { get; }

        /// <summary>
        /// Resolves a suggested move using the in-process OreSortSolver.
        /// Returns false if no hint is available (already solved, no solution found, or daily limit hit).
        /// </summary>
        bool TryGetHint(LevelData currentLevel, out int sourceMoldIndex, out int targetMoldIndex);
    }
}
