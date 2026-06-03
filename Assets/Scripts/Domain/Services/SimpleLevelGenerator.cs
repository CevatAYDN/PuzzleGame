using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Simple, non-difficulty-aware level generator.
    /// Produces random valid assignments for testing purposes — does not apply
    /// difficulty curves or constraint solving. For production use, prefer
    /// <see cref="DifficultyBasedLevelGenerator"/>.
    /// </summary>
    public class SimpleLevelGenerator : ILevelGenerator
    {
        public List<List<LiquidLayer>> Generate(int bottleCount, int maxLayers,
            int emptyBottleCount, DomainColor[] colorPalette,
            Difficulty difficulty, int seed)
        {
            var rng = new System.Random(seed);
            var result = new List<List<LiquidLayer>>(bottleCount);
            int colorIdx = 0;

            for (int b = 0; b < bottleCount - emptyBottleCount; b++)
            {
                var bottle = new List<LiquidLayer>(maxLayers);
                var color = colorPalette[colorIdx % colorPalette.Length];
                colorIdx++;
                int layers = rng.Next(2, maxLayers + 1);
                for (int l = 0; l < layers; l++)
                    bottle.Add(new LiquidLayer(color, 1f));
                result.Add(bottle);
            }

            for (int e = 0; e < emptyBottleCount; e++)
                result.Add(new List<LiquidLayer>());

            return result;
        }
    }
}
