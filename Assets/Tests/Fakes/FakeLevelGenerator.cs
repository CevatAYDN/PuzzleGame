using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for ILevelGenerator.
    /// </summary>
    public class FakeLevelGenerator : ILevelGenerator
    {
        public List<List<LiquidLayer>> GenerateResult { get; set; }
            = new List<List<LiquidLayer>>();

        public int GenerateCallCount { get; private set; }
        public int LastBottleCount { get; private set; }
        public int LastMaxLayers { get; private set; }
        public int LastEmptyBottleCount { get; private set; }
        public DomainColor[] LastPalette { get; private set; }
        public Difficulty LastDifficulty { get; private set; }
        public int LastSeed { get; private set; }

        public List<List<LiquidLayer>> Generate(int bottleCount, int maxLayers,
            int emptyBottleCount, DomainColor[] colorPalette,
            Difficulty difficulty, int seed)
        {
            GenerateCallCount++;
            LastBottleCount = bottleCount;
            LastMaxLayers = maxLayers;
            LastEmptyBottleCount = emptyBottleCount;
            LastPalette = colorPalette;
            LastDifficulty = difficulty;
            LastSeed = seed;
            return GenerateResult;
        }

        /// <summary>
        /// Helper: creates a simple valid assignment for testing.
        /// </summary>
        public void SetSimpleAssignment(int bottleCount, int emptyBottleCount)
        {
            GenerateResult = new List<List<LiquidLayer>>();
            for (int i = 0; i < bottleCount - emptyBottleCount; i++)
            {
                var color = new DomainColor(0.2f + i * 0.1f, 0.5f, 0.8f);
                GenerateResult.Add(new List<LiquidLayer>
                {
                    new LiquidLayer(color, 1f)
                });
            }
            for (int i = 0; i < emptyBottleCount; i++)
            {
                GenerateResult.Add(new List<LiquidLayer>());
            }
        }
    }
}
