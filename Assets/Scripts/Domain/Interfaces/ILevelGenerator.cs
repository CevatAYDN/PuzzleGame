using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Interfaces
{
    public interface ILevelGenerator
    {
        List<List<OreLayer>> Generate(
            int MoldCount,
            int maxLayers,
            int emptyMoldCount,
            DomainColor[] colorPalette,
            Difficulty difficulty,
            int seed = 0);
    }
}
