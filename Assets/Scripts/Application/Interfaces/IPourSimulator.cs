using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Gameplay-facing pour operations. Preview without mutation + execute
    /// with mutation. No dev-tool concerns (no direct state setters, no
    /// config overrides, no debug queries).
    /// </summary>
    public interface IPourSimulator
    {
        /// <summary>
        /// Computes what would happen if a pour were executed between two molds.
        /// Does NOT mutate state. Safe to call from editor preview or AI solvers.
        /// </summary>
        PourPreviewResult PreviewPour(int sourceIndex, int targetIndex);

        /// <summary>
        /// Executes a pour instantly — no tween, no VFX, no audio.
        /// State is mutated directly. Fires standard events
        /// (<c>CastCompletedEvent</c> or <c>CastRejectedEvent</c>).
        /// </summary>
        /// <returns>True if the pour was valid and executed.</returns>
        bool ExecuteInstantPour(int sourceIndex, int targetIndex);
    }
}
