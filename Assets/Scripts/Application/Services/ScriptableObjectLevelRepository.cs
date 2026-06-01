using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// LevelData repository backed by an array of ScriptableObject assets.
    /// Sorted by levelNumber, null-safe, no Unity API dependency beyond constructor.
    /// </summary>
    public class ScriptableObjectLevelRepository : ILevelRepository
    {
        private readonly List<LevelData> _sorted;

        public IReadOnlyList<LevelData> AllLevels => _sorted;

        public int TotalCount => _sorted.Count;

        public ScriptableObjectLevelRepository(IEnumerable<LevelData> levels)
        {
            _sorted = levels?
                .Where(l => l != null)
                .OrderBy(l => l.levelNumber)
                .ToList()
                      ?? new List<LevelData>();
        }

        public LevelData GetByNumber(int levelNumber)
        {
            return _sorted.FirstOrDefault(l => l.levelNumber == levelNumber);
        }
    }
}
