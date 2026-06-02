namespace PuzzleGame.Application.Interfaces
{
    public interface IInputHandlerService
    {
        void SetBottles(IBottleView[] bottles);
        void ProcessInput();
    }
}
