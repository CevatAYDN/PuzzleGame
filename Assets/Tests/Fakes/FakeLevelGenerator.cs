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
        public List<List<OreLayer>> GenerateResult { get; set; }
            = new List<List<OreLayer>>();

        public bool GenerateSolvableFlag { get; set; } = true;

        public int GenerateCallCount { get; private set; }
        public int LastMoldCount { get; private set; }
        public int LastMaxLayers { get; private set; }
        public int LastEmptyMoldCount { get; private set; }
        public DomainColor[] LastPalette { get; private set; }
        public Difficulty LastDifficulty { get; private set; }
        public int LastSeed { get; private set; }

        public List<List<OreLayer>> Generate(int MoldCount, int maxLayers,
            int emptyMoldCount, DomainColor[] colorPalette,
            Difficulty difficulty, int seed)
        {
            GenerateCallCount++;
            LastMoldCount = MoldCount;
            LastMaxLayers = maxLayers;
            LastEmptyMoldCount = emptyMoldCount;
            LastPalette = colorPalette;
            LastDifficulty = difficulty;
            LastSeed = seed;
            return GenerateResult;
        }

        public (List<List<OreLayer>> Molds, bool IsSolvable) GenerateSolvable(
            int MoldCount, int maxLayers, int emptyMoldCount,
            DomainColor[] colorPalette, Difficulty difficulty, int seed, int maxAttempts,
            bool enableFrozenLayers = false, bool enableMultiPour = false)
        {
            GenerateCallCount++;
            LastMoldCount = MoldCount;
            LastMaxLayers = maxLayers;
            LastEmptyMoldCount = emptyMoldCount;
            LastPalette = colorPalette;
            LastDifficulty = difficulty;
            LastSeed = seed;
            return (GenerateResult, GenerateSolvableFlag);
        }

        /// <summary>
        /// Helper: creates a simple valid assignment for testing.
        /// </summary>
        public void SetSimpleAssignment(int MoldCount, int emptyMoldCount)
        {
            GenerateResult = new List<List<OreLayer>>();
            for (int i = 0; i < MoldCount - emptyMoldCount; i++)
            {
                var color = new DomainColor(0.2f + i * 0.1f, 0.5f, 0.8f);
                GenerateResult.Add(new List<OreLayer>
                {
                    new OreLayer(color, 1f)
                });
            }
            for (int i = 0; i < emptyMoldCount; i++)
            {
                GenerateResult.Add(new List<OreLayer>());
            }
        }
    }
}
