namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Contract for any class that needs to receive centralized frame-rate updates.
    /// Lives in Application layer — Unity lifecycle concern, not domain logic.
    /// </summary>
    public interface IUpdateable
    {
        void OnUpdate(float deltaTime);
    }
}
