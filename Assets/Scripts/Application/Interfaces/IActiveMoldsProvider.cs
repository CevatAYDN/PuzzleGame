namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Exposes the active gameplay molds without leaking the Presentation-layer
    /// <c>MoldPoolInitializer</c> into the Application layer.
    /// </summary>
    public interface IActiveMoldsProvider
    {
        IMoldView[] Molds { get; }
    }
}
