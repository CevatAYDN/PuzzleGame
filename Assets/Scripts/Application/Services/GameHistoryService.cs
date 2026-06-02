using System.Collections.Generic;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Records all moves (state snapshots) and allows Undo operation.
    /// Stores BottleState layers as immutable snapshots.
    /// </summary>
    public class GameHistoryService : IGameHistoryService
    {
        private readonly Stack<List<LiquidLayer>[]> _history = new Stack<List<LiquidLayer>[]>();

        public bool CanUndo => _history.Count > 0;

        public void RecordSnapshot(BottleState[] bottles)
        {
            if (bottles == null) return;

            var snapshot = new List<LiquidLayer>[bottles.Length];
            for (int i = 0; i < bottles.Length; i++)
            {
                var state = bottles[i];
                if (state == null) continue; // null bottle = empty layer
                snapshot[i] = new List<LiquidLayer>(state.Layers);
            }
            _history.Push(snapshot);
        }

        public void Undo()
        {
            if (_history.Count == 0) return;
            var snapshot = _history.Pop();
            LastSnapshot = snapshot;
        }

        /// <summary>
        /// Filled when Undo() is called.
        /// Consumer (GameManager) loads these lists into BottleStates.
        /// </summary>
        public List<LiquidLayer>[] LastSnapshot { get; private set; }
        
        /// <summary>
        /// Clears all history, resetting to initial state.
        /// </summary>
        public void Clear()
        {
            _history.Clear();
            LastSnapshot = null;
        }
    }
}
