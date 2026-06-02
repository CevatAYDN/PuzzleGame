using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Interfaces
{
    public interface ILevelValidationService
    {
        bool ValidateLevel(LevelData levelData, int totalBottlesAvailable);
    }
}
