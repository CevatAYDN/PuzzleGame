using System;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Application.Services
{
    public class GameHistoryManager : IGameHistoryManager
    {
        private readonly Stack<List<OreLayer>[]> _history = new Stack<List<OreLayer>[]>();
        private IMoldView[] _Molds;
        private int _moveCount;

        public int CurrentMoveCount => _moveCount;
        public bool CanUndo => _history.Count > 0;

        public event Action<int> OnMoveCountChanged;

        public void Initialize(IMoldView[] Molds)
        {
            if (Molds == null) throw new ArgumentNullException(nameof(Molds));
            _Molds = Molds;
            ResetAll();
        }

        public void RecordUndoSnapshot()
        {
            if (_Molds == null)
            {
                MoldLogger.LogWarning("RecordUndoSnapshot called before Initialize.");
                return;
            }

            var snapshot = new List<OreLayer>[_Molds.Length];
            for (int i = 0; i < _Molds.Length; i++)
            {
                var state = _Molds[i]?.State;
                if (state == null) continue;
                snapshot[i] = new List<OreLayer>(state.Layers);
            }
            _history.Push(snapshot);
            MoldLogger.LogDebug($"Undo snapshot recorded. Stack size: {_history.Count}");
        }

        public void IncrementMoveCount()
        {
            _moveCount++;
            OnMoveCountChanged?.Invoke(_moveCount);
            MoldLogger.LogInfo($"Move incremented: {_moveCount}");
        }

        public void Undo()
        {
            if (!CanUndo)
            {
                MoldLogger.LogDebug("Undo requested but history is empty.");
                return;
            }
            if (_Molds == null) throw new InvalidOperationException(
                "GameHistoryManager.Undo called before Initialize.");

            var snapshot = _history.Pop();
            if (snapshot == null) return;

            for (int i = 0; i < snapshot.Length && i < _Molds.Length; i++)
            {
                if (_Molds[i] == null || _Molds[i].State == null) continue;
                try
                {
                    _Molds[i].State.ReplaceLayers((IEnumerable<OreLayer>)snapshot[i] ?? System.Array.Empty<OreLayer>());
                    _Molds[i].UpdateVisualsFromState();
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException(
                        $"Undo failed for Mold {i}: snapshot is invalid (layer count > MaxLayers).", ex);
                }
            }

            _moveCount = Math.Max(0, _moveCount - 1);
            OnMoveCountChanged?.Invoke(_moveCount);
            MoldLogger.LogInfo($"Undo performed. Current moves: {_moveCount}");
        }

        public void ResetAll()
        {
            _history.Clear();
            _moveCount = 0;
            OnMoveCountChanged?.Invoke(_moveCount);
            MoldLogger.LogInfo("History and move count reset.");
        }
    }
}
