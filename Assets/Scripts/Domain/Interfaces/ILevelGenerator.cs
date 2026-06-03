using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Interfaces
{
    public interface ILevelGenerator
    {
        List<List<LiquidLayer>> Generate(
            int bottleCount,
            int maxLayers,
            int emptyBottleCount,
            DomainColor[] colorPalette,
            Difficulty difficulty,
            int seed = 0);
    }
}
