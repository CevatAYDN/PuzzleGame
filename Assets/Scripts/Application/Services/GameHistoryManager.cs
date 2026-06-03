using System;
using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Application.Services
{
    public class GameHistoryManager : IGameHistoryManager
    {
        private readonly Stack<List<LiquidLayer>[]> _history = new Stack<List<LiquidLayer>[]>();
        private IBottleView[] _bottles;
        private int _moveCount;

        public int CurrentMoveCount => _moveCount;
        public bool CanUndo => _history.Count > 0;

        public event Action<int> OnMoveCountChanged;

        public void Initialize(IBottleView[] bottles)
        {
            if (bottles == null) throw new ArgumentNullException(nameof(bottles));
            _bottles = bottles;
            ResetAll();
        }

        public void RecordUndoSnapshot()
        {
            if (_bottles == null)
            {
                BottleLogger.LogWarning("RecordUndoSnapshot called before Initialize.");
                return;
            }

            var snapshot = new List<LiquidLayer>[_bottles.Length];
            for (int i = 0; i < _bottles.Length; i++)
            {
                var state = _bottles[i]?.State;
                if (state == null) continue;
                snapshot[i] = new List<LiquidLayer>(state.Layers);
            }
            _history.Push(snapshot);
            BottleLogger.LogDebug($"Undo snapshot recorded. Stack size: {_history.Count}");
        }

        public void IncrementMoveCount()
        {
            _moveCount++;
            OnMoveCountChanged?.Invoke(_moveCount);
            BottleLogger.LogInfo($"Move incremented: {_moveCount}");
        }

        public void Undo()
        {
            if (!CanUndo)
            {
                BottleLogger.LogDebug("Undo requested but history is empty.");
                return;
            }
            if (_bottles == null) throw new InvalidOperationException(
                "GameHistoryManager.Undo called before Initialize.");

            var snapshot = _history.Pop();
            if (snapshot == null) return;

            for (int i = 0; i < snapshot.Length && i < _bottles.Length; i++)
            {
                if (_bottles[i] == null || _bottles[i].State == null) continue;
                try
                {
                    _bottles[i].State.ReplaceLayers(snapshot[i] ?? new List<LiquidLayer>());
                    _bottles[i].UpdateVisualsFromState();
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException(
                        $"Undo failed for bottle {i}: snapshot is invalid (layer count > MaxLayers).", ex);
                }
            }

            _moveCount = Mathf.Max(0, _moveCount - 1);
            OnMoveCountChanged?.Invoke(_moveCount);
            BottleLogger.LogInfo($"Undo performed. Current moves: {_moveCount}");
        }

        public void ResetAll()
        {
            _history.Clear();
            _moveCount = 0;
            OnMoveCountChanged?.Invoke(_moveCount);
            BottleLogger.LogInfo("History and move count reset.");
        }
    }
}
