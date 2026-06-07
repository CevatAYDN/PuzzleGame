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
        private readonly Stack<List<OreLayer>[]> _snapshotPool = new Stack<List<OreLayer>[]>();
        private readonly List<OreLayer> _layerBuffer = new List<OreLayer>(16);
        private IMoldView[] _Molds;
        private int _moveCount;
        private int _maxMolds;

        public int CurrentMoveCount => _moveCount;
        public bool CanUndo => _history.Count > 0;

        public event Action<int> OnMoveCountChanged;

        public void Initialize(IMoldView[] Molds)
        {
            if (Molds == null) throw new ArgumentNullException(nameof(Molds));
            _Molds = Molds;
            _maxMolds = Molds.Length;
            ResetAll();
        }

        public void SetMolds(IMoldView[] Molds)
        {
            _Molds = Molds ?? throw new ArgumentNullException(nameof(Molds));
            _maxMolds = Molds.Length;
        }

        public void RecordUndoSnapshot()
        {
            if (_Molds == null)
            {
                MoldLogger.LogWarning("RecordUndoSnapshot called before Initialize.");
                return;
            }

            // Fix #10: Size the snapshot to the CURRENT number of active molds,
            // not the level-initial `_maxMolds`. After ActivateOptionalMolds the
            // pool grows, but a recycled snapshot from the pool is still sized to
            // the old `_maxMolds` — extra optional molds would never be recorded
            // and would silently revert on Undo. Use the larger of the two so
            // pooled snapshots never truncate a freshly-grown mold set.
            int neededSize = Math.Max(_maxMolds, _Molds.Length);
            var snapshot = _snapshotPool.Count > 0
                ? _snapshotPool.Pop()
                : new List<OreLayer>[neededSize];

            // If a recycled snapshot is too small for the current mold set, grow it.
            if (snapshot.Length < neededSize)
            {
                Array.Resize(ref snapshot, neededSize);
            }

            for (int i = 0; i < _Molds.Length; i++)
            {
                var state = _Molds[i]?.State;
                if (state == null || state.Layers == null)
                {
                    snapshot[i] = null;
                    continue;
                }

                _layerBuffer.Clear();
                for (int j = 0; j < state.Layers.Count; j++)
                {
                    var layer = state.Layers[j];
                    if (layer.Amount > 0f)
                        _layerBuffer.Add(layer);
                }

                if (snapshot[i] == null)
                    snapshot[i] = new List<OreLayer>(_layerBuffer.Count);
                else
                    snapshot[i].Clear();

                snapshot[i].AddRange(_layerBuffer);
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
                if (snapshot[i] == null) continue;

                try
                {
                    _Molds[i].State.ReplaceLayers(snapshot[i]);
                    _Molds[i].UpdateVisualsFromState();
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException(
                        $"Undo failed for Mold {i}: snapshot is invalid (layer count > MaxLayers).", ex);
                }
            }

            if (snapshot.Length == _maxMolds)
            {
                // Fix #10: Only return to the pool if the snapshot's capacity
                // matches the original pool's expected size. Snapshots grown to
                // accommodate optional molds (Array.Resize above) stay in history
                // to avoid contaminating the pool with oversized entries.
                for (int i = 0; i < snapshot.Length; i++)
                {
                    if (snapshot[i] != null)
                        snapshot[i].Clear();
                }
                _snapshotPool.Push(snapshot);
            }

            _moveCount = Math.Max(0, _moveCount - 1);
            OnMoveCountChanged?.Invoke(_moveCount);
            MoldLogger.LogInfo($"Undo performed. Current moves: {_moveCount}");
        }

        public void ResetAll()
        {
            while (_history.Count > 0)
            {
                var snap = _history.Pop();
                // Fix #10: see Undo — only re-pool snapshots sized for the
                // original pool capacity to avoid contaminating it.
                if (snap != null && snap.Length == _maxMolds)
                {
                    for (int i = 0; i < snap.Length; i++)
                    {
                        if (snap[i] != null)
                            snap[i].Clear();
                    }
                    _snapshotPool.Push(snap);
                }
            }
            _moveCount = 0;
            OnMoveCountChanged?.Invoke(_moveCount);
            MoldLogger.LogInfo("History and move count reset.");
        }
    }
}
