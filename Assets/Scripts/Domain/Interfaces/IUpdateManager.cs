namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Central manager interface to register and unregister updateable objects.
    /// Lives in Application layer — Unity lifecycle concern, not domain logic.
    /// </summary>
    public interface IUpdateManager
    {
        void Register(IUpdateable updateable);
        void Unregister(IUpdateable updateable);
    }
}
