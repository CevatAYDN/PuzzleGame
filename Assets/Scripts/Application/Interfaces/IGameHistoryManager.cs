using System;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Interfaces
{
    public interface IGameHistoryManager
    {
        int CurrentMoveCount { get; }
        bool CanUndo { get; }
        event Action<int> OnMoveCountChanged;

        void Initialize(IBottleView[] bottles);
        void RecordUndoSnapshot();
        void IncrementMoveCount();
        void Undo();
        void ResetAll();
    }
}
