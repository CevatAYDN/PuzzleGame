namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Exposes the active gameplay molds without leaking the Presentation-layer
    /// <c>MoldPoolInitializer</c> into the Application layer.
    /// 
    /// This interface is intentionally minimal - only provides read access to the
    /// active mold list. Operations like ActivateOptionalMolds belong in
    /// MoldPoolInitializer to avoid circular dependencies.
    /// </summary>
    public interface IActiveMoldsProvider
    {
        IMoldView[] Molds { get; set; }
    }
}
