using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    public interface ILevelValidationService
    {
        bool ValidateLevel(LevelData levelData, int totalMoldsAvailable);
    }
}
