using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for performing Ore Cast operations.
    /// Separated from CastService.cs to respect ISP and SRP.
    /// </summary>
    public interface ICastService
    {
        bool TryCast(IMoldView source, IMoldView target, Configuration.LevelData levelData, IMoldView[] activeMolds);
        bool TryMultiCast(IMoldView[] sources, IMoldView target, Configuration.LevelData levelData, IMoldView[] activeMolds);
        int GetCastLayerCount(IMoldView source, IMoldView target, Configuration.LevelData levelData);
    }
}
