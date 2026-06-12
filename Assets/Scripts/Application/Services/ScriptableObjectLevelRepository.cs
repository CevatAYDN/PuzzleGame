using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// LevelData repository backed by an array of ScriptableObject assets.
    /// Sorted by levelNumber, null-safe, no Unity API dependency beyond constructor.
    /// </summary>
    public class ScriptableObjectLevelRepository : ILevelRepository
    {
        private readonly List<LevelData> _sorted;
        // Fix #M8: Dictionary cache for O(1) lookup instead of O(n) FirstOrDefault
        private readonly Dictionary<int, LevelData> _byNumber;

        public IReadOnlyList<LevelData> AllLevels => _sorted;

        public int TotalCount => _sorted.Count;

        public ScriptableObjectLevelRepository(IEnumerable<LevelData> levels)
        {
            _sorted = levels?
                .Where(l => l != null)
                .OrderBy(l => l.levelNumber)
                .ToList()
                      ?? new List<LevelData>();

            // Build dictionary cache for O(1) lookup
            _byNumber = new Dictionary<int, LevelData>(_sorted.Count);
            foreach (var level in _sorted)
            {
                _byNumber[level.levelNumber] = level;
            }
        }

        public LevelData GetByNumber(int levelNumber)
        {
            // Fix #M8: O(1) dictionary lookup instead of O(n) linear scan
            if (_byNumber.TryGetValue(levelNumber, out var level))
            {
                return level;
            }

            // Infinite Levels Fallback: dynamically generate a level if we run out of pre-authored ones.
            var proceduralLevel = UnityEngine.ScriptableObject.CreateInstance<LevelData>();
            proceduralLevel.name = $"Level_{levelNumber}_Procedural";
            proceduralLevel.levelNumber = levelNumber;
            proceduralLevel.autoGenerate = true;
            proceduralLevel.randomSeed = levelNumber * 1337;
            
            // Progressive curve for procedural levels
            proceduralLevel.MoldCount = System.Math.Clamp(5 + (levelNumber / 15), 5, 14);
            proceduralLevel.emptyMoldCount = 2;
            proceduralLevel.maxLayersPerMold = 4;
            
            if (levelNumber > 200) proceduralLevel.difficulty = PuzzleGame.Domain.Difficulty.Expert;
            else if (levelNumber > 80) proceduralLevel.difficulty = PuzzleGame.Domain.Difficulty.Hard;
            else proceduralLevel.difficulty = PuzzleGame.Domain.Difficulty.Medium;

            // Cache it so it remains consistent during the session
            _byNumber[levelNumber] = proceduralLevel;
            
            return proceduralLevel;
        }
    }
}
