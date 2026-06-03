namespace PuzzleGame.Domain.Interfaces
{
    /// <summary>
    /// Central manager interface to register and unregister updateable classes.
    /// </summary>
    public interface IUpdateManager
    {
        void Register(IUpdateable updateable);
        void Unregister(IUpdateable updateable);
    }
}
