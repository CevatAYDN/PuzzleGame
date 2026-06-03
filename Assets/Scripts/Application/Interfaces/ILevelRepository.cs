using System.Collections.Generic;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Level data provider. Implementations: ScriptableObject-levels, JSON, remote.
    /// </summary>
    public interface ILevelRepository
    {
        IReadOnlyList<LevelData> AllLevels { get; }
        LevelData GetByNumber(int levelNumber);
        int TotalCount { get; }
    }
}
