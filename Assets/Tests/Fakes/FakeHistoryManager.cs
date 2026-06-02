using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IGameHistoryManager.
    /// </summary>
    public class FakeHistoryManager : IGameHistoryManager
    {
        public int CurrentMoveCount { get; set; }
        public bool CanUndo { get; set; } = true;

        public event Action<int> OnMoveCountChanged;

        public int RecordUndoSnapshotCallCount { get; private set; }
        public int IncrementMoveCountCallCount { get; private set; }
        public int UndoCallCount { get; private set; }
        public int ResetAllCallCount { get; private set; }
        public int InitializeCallCount { get; private set; }
        public IBottleView[] LastInitializedBottles { get; private set; }

        public void Initialize(IBottleView[] bottles)
        {
            InitializeCallCount++;
            LastInitializedBottles = bottles;
        }

        public void RecordUndoSnapshot()
        {
            RecordUndoSnapshotCallCount++;
        }

        public void IncrementMoveCount()
        {
            IncrementMoveCountCallCount++;
            CurrentMoveCount++;
            OnMoveCountChanged?.Invoke(CurrentMoveCount);
        }

        public void Undo()
        {
            UndoCallCount++;
            if (CurrentMoveCount > 0)
                CurrentMoveCount--;
        }

        public void ResetAll()
        {
            ResetAllCallCount++;
            CurrentMoveCount = 0;
        }

        public void RaiseMoveCountChanged(int count)
        {
            CurrentMoveCount = count;
            OnMoveCountChanged?.Invoke(count);
        }
    }
}
