using System.Collections.Generic;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Interfaces
{
    /// <summary>
    /// Level data provider. Implementations: ScriptableObject-levels, JSON, remote.
    /// Domain layer — pure POCO.
    /// </summary>
    public interface ILevelRepository
    {
        IReadOnlyList<LevelData> AllLevels { get; }
        LevelData GetByNumber(int levelNumber);
        int TotalCount { get; }
    }
}
