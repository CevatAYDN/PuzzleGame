using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    public interface IInputHandlerService
    {
        void SetMolds(IMoldView[] Molds);
        void SetLevelData(LevelData levelData);
        void ProcessInput();
    }
}
