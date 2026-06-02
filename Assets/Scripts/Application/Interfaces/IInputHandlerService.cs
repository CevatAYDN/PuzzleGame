using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Interfaces
{
    public interface IInputHandlerService
    {
        void SetBottles(IBottleView[] bottles);
        void SetLevelData(LevelData levelData);
        void ProcessInput();
    }
}
