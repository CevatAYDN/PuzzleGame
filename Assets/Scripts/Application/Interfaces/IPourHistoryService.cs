namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Undo support — deep snapshot stack of all active mold states.
    /// Max 32 snapshots (older entries discarded). Snapshots are taken
    /// before each pour and restored on undo.
    /// </summary>
    public interface IPourHistoryService
    {
        /// <summary>Takes a deep snapshot of all active molds and pushes it onto the stack.</summary>
        void SnapshotAllMolds();

        /// <summary>Pops the most recent snapshot and restores all molds to that state.
        /// No-op if the stack is empty.</summary>
        void RestoreSnapshot();
    }
}
