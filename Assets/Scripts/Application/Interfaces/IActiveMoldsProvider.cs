using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Exposes the active gameplay molds without leaking the Presentation-layer
    /// <c>MoldPoolInitializer</c> into the Application layer.
    /// </summary>
    public interface IActiveMoldsProvider
    {
        IMoldView[] Molds { get; }

        /// <summary>
        /// Activates the optional mold slots declared by the level, appends them
        /// to <see cref="Molds"/>, and re-wires input/history/error indicators.
        /// No-op when the level has no optional targets.
        /// </summary>
        void ActivateOptionalMolds(LevelData level);
    }
}
