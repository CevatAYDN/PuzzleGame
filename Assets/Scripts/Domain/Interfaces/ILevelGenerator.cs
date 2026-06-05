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

        /// <summary>
        /// Generates a layout and verifies it is solvable via OreSortSolver.
        /// Retries with seed+1, seed+2, ... up to <paramref name="maxAttempts"/> times
        /// if the initial attempt is unsolvable. Returns the first solvable layout,
        /// or the last attempt with <c>IsSolvable=false</c> if no attempt succeeds.
        /// </summary>
        (List<List<OreLayer>> Molds, bool IsSolvable) GenerateSolvable(
            int MoldCount,
            int maxLayers,
            int emptyMoldCount,
            DomainColor[] colorPalette,
            Difficulty difficulty,
            int seed = 0,
            int maxAttempts = 8);
    }
}
