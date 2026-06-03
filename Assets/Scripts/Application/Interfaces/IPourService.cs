using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for performing liquid pour operations.
    /// Separated from PourService.cs to respect ISP and SRP.
    /// </summary>
    public interface IPourService
    {
        bool TryPour(IBottleView source, IBottleView target, Configuration.LevelData levelData, IBottleView[] activeBottles);
        int GetPourLayerCount(IBottleView source, IBottleView target, Configuration.LevelData levelData);
    }
}
