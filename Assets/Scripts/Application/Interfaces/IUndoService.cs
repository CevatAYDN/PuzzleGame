using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Interfaces
{
    public interface IUndoService
    {
        int Cost { get; }
        int RemainingUndosForCurrentLevel { get; }

        /// <summary>
        /// Performs an undo if affordable and the history allows it.
        /// Deducts the configured coin cost via <see cref="ICoinWallet"/>.
        /// </summary>
        bool TryUndo();
    }
}
